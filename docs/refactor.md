# Backend Refactor Plan

## Goals

1. Establish three clearly separated data model layers: **frontend (API contracts)**, **middle tier (domain + commands)**, and **backend (Cosmos storage)**.
2. Standardize Telegram logging via `LogBuffer.Wrap()` across every Azure Function, webhook, and API.
3. Replace the monolithic webhook switch-statement with a **command dispatch pattern** where each command lives in its own class.

---

## Layer 1 — Frontend Data Models (API DTOs)

These types represent the shapes that cross the HTTP boundary — request bodies, query parameters, and response payloads. They are **never** exposed to Cosmos internals (`AppId`, `_etag`, document `id`). They live in a new `api/dtos/` folder.

### General conventions

- Response types are suffixed `Response`; request types are suffixed `Request`.
- All response types include an `Id` field so callers can reference the resource in subsequent calls.
- The Cosmos `_etag` is never exposed by name. Instead, every response for a mutable resource includes a `VersionToken` field (an opaque string that happens to hold the etag value). Callers must echo this token back in any update or delete request. The function maps it back to the etag and passes it to `JsonStore` for OCC. If the entry has changed since the read, the function returns `409 Conflict`.
- Create requests do not include a `VersionToken` (the document doesn't exist yet).
- Validation errors return a `400` with a `ProblemDetails`-style body.

### Grocery List

The grocery list does **not** use client-driven OCC. Concurrent updates are handled server-side via an internal retry loop (read → apply → write, retry on `PreconditionFailed`). The frontend just sends the desired mutation and gets back the resulting list — no `VersionToken` involved.

```csharp
// api/dtos/grocerylist/GroceryListResponse.cs
public record GroceryListResponse(
    string ListId,
    List<string> Items
);

// api/dtos/grocerylist/UpdateGroceryListRequest.cs
public record UpdateGroceryListRequest(
    List<string> ToAdd,
    List<string> ToRemove
);
```

### Address Book

```csharp
// api/dtos/addressbook/AddressBookEntryRequest.cs
// Used for creates (no version token needed).
public record AddressBookEntryRequest(
    string FirstName,
    string LastName,
    string Street,
    string? Apartment,
    string City,
    string State,
    string ZipCode,
    string? PhoneNumber,
    string? MailingName,
    string? OtherPeople,
    bool HolidayCard
);

// api/dtos/addressbook/AddressBookEntryUpdateRequest.cs
// Used for updates and deletes. The caller must echo back the VersionToken
// it received from the read response. The function maps this back to the
// Cosmos etag and uses it for OCC — the update/delete will fail with 409
// Conflict if the entry has been modified since it was read.
public record AddressBookEntryUpdateRequest(
    string Id,
    string VersionToken,         // opaque; set to the etag value from the read response
    string FirstName,
    string LastName,
    string Street,
    string? Apartment,
    string City,
    string State,
    string ZipCode,
    string? PhoneNumber,
    string? MailingName,
    string? OtherPeople,
    bool HolidayCard
);

// api/dtos/addressbook/AddressBookEntryResponse.cs
public record AddressBookEntryResponse(
    string Id,
    string VersionToken,         // opaque version token; pass back on update/delete to enforce OCC
    string FirstName,
    string LastName,
    string Street,
    string? Apartment,
    string City,
    string State,
    string ZipCode,
    string? PhoneNumber,
    string? MailingName,
    string? OtherPeople,
    bool HolidayCard,
    string MailingLabel          // formatted by domain layer, not caller
);
```

### Secret Santa

Secret Santa config and events both use client-driven OCC via `VersionToken`, the same pattern as Address Book. The admin UI reads a config or event, the user edits it, and the update only succeeds if nothing else changed it in the meantime.

```csharp
// api/dtos/secretsanta/SecretSantaConfigResponse.cs
public record SecretSantaConfigResponse(
    string VersionToken,         // echo back on update to enforce OCC
    List<PersonDto> People,
    List<RestrictionDto> Restrictions
);

// api/dtos/secretsanta/SecretSantaConfigUpdateRequest.cs
public record SecretSantaConfigUpdateRequest(
    string VersionToken,
    List<PersonDto> People,
    List<RestrictionDto> Restrictions
);

public record PersonDto(string Name, string Email);
public record RestrictionDto(string Person1Email, string Person2Email);

// api/dtos/secretsanta/SecretSantaEventResponse.cs
public record SecretSantaEventResponse(
    string EventId,
    string VersionToken,         // echo back on update to enforce OCC
    string GroupName,
    int Year,
    bool IsRunning,
    List<ParticipantResponse> Participants
);

// api/dtos/secretsanta/SecretSantaEventUpdateRequest.cs
// Used for updates (e.g. SecretSantaAdminUpdateEvent). Not used for
// SecretSantaAdminStartEvent, which takes only EventId + VersionToken.
public record SecretSantaEventUpdateRequest(
    string EventId,
    string VersionToken,
    string GroupName,
    int Year,
    List<ParticipantResponse> Participants
);

// api/dtos/secretsanta/SecretSantaStartEventRequest.cs
public record SecretSantaStartEventRequest(
    string EventId,
    string VersionToken
);

public record ParticipantResponse(
    string Name,
    string Email,
    string? SantaForName,     // null until event is started
    string? SantaForEmail
);
```

### Auth

```csharp
// api/dtos/auth/TokenResponse.cs
public record TokenResponse(string Token, DateTime Expiration);
```

---

## Layer 2 — Middle Tier Data Models

These are the **domain objects** and **command types** used by business logic, algorithms, and command handlers. They are independent of both HTTP contracts and Cosmos storage. They live in `api/domain/`.

### Domain Objects

```csharp
// api/domain/GroceryList.cs
public class GroceryList
{
    public string ListId { get; init; }
    public IReadOnlyList<string> Items { get; init; }

    public GroceryList Apply(IEnumerable<string> toAdd, IEnumerable<string> toRemove)
        => new GroceryList
        {
            ListId = ListId,
            Items = Items
                .Except(toRemove, StringComparer.OrdinalIgnoreCase)
                .Concat(toAdd)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList()
        };
}

// api/domain/AddressBookEntry.cs
public class AddressBookEntry
{
    public string Id { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Street { get; init; }
    public string? Apartment { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string ZipCode { get; init; }
    public string? PhoneNumber { get; init; }
    public string? MailingName { get; init; }
    public string? OtherPeople { get; init; }
    public bool HolidayCard { get; init; }

    public string MailingLabel => string.Join("\n",
        new[] { MailingName ?? $"{FirstName} {LastName}", Street,
                Apartment, $"{City}, {State} {ZipCode}" }
            .Where(l => !string.IsNullOrWhiteSpace(l)));
}

// api/domain/SecretSantaAssignment.cs
// (produced by the Assign algorithm)
public class SecretSantaAssignment
{
    public string GiverEmail { get; init; }
    public string GiverName { get; init; }
    public string ReceiverEmail { get; init; }
    public string ReceiverName { get; init; }
}
```

### Telegram Command Context

A shared context object passed to every command handler, giving it everything it needs without coupling to `HttpRequest` or raw Cosmos objects.

```csharp
// api/domain/telegram/CommandContext.cs
public class CommandContext
{
    public long ChatId { get; init; }
    public long FromUserId { get; init; }
    public string? Username { get; init; }
    public string RawText { get; init; }
    public string CommandName { get; init; }    // "/add", "/list", etc.
    public string[] Args { get; init; }         // tokens after the command name
    public LogBuffer Log { get; init; }
    public IServiceProvider Services { get; init; }
}

// api/domain/telegram/CommandResult.cs
public class CommandResult
{
    public bool Success { get; init; }
    public string Message { get; init; }        // text to send back to Telegram chat

    public static CommandResult Ok(string message) => new() { Success = true, Message = message };
    public static CommandResult Fail(string message) => new() { Success = false, Message = message };
}
```

### Command Interface

```csharp
// api/domain/telegram/ICommand.cs
public interface ICommand
{
    string Name { get; }                        // "/ping", "/add", etc.
    bool RequiresAuthorization { get; }
    Task<CommandResult> ExecuteAsync(CommandContext context);
}
```

---

## Layer 3 — Backend Data Models (Cosmos Storage)

These extend `CosmosDocument` and map 1:1 to Cosmos documents. They live in `api/storage/documents/`. **No business logic lives here** — they are pure storage representations.

The existing `JsonStore<T>` remains unchanged. All new document types follow the same `AppId`/`id`/`_etag` convention.

```csharp
// api/storage/documents/GroceryListDocument.cs
public class GroceryListDocument : CosmosDocument
{
    public const string PartitionKey = "GroceryList";
    public List<string> Items { get; set; } = new();
}

// api/storage/documents/AddressBookDocument.cs
public class AddressBookDocument : CosmosDocument
{
    public const string PartitionKey = "AddressBookEntry";
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Street { get; set; }
    public string? Apartment { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? MailingName { get; set; }
    public string? OtherPeople { get; set; }
    public bool HolidayCard { get; set; }
}

// api/storage/documents/SecretSantaConfigDocument.cs
public class SecretSantaConfigDocument : CosmosDocument
{
    public const string PartitionKey = "SecretSanta";
    public const string DocId = "SecretSantaConfig";
    public List<PersonDto> People { get; set; } = new();
    public List<RestrictionDto> Restrictions { get; set; } = new();
}

// api/storage/documents/SecretSantaEventDocument.cs
public class SecretSantaEventDocument : CosmosDocument
{
    public const string PartitionKey = "SecretSantaEvent";
    public bool IsRunning { get; set; }
    public string GroupName { get; set; }
    public int Year { get; set; }
    public List<ParticipantDocument> Participants { get; set; } = new();
}

public class ParticipantDocument
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string? SantaForName { get; set; }
    public string? SantaForEmail { get; set; }
}

// api/storage/documents/RefreshableTokenDocument.cs
public class RefreshableTokenDocument : CosmosDocument
{
    public const string PartitionKey = "RefreshableTokenApp";
    public string Token { get; set; }
    public DateTime Expiration { get; set; }
}

// api/storage/documents/AuthLogDocument.cs
public class AuthLogDocument : CosmosDocument
{
    public const string PartitionKey = "AuthLog";
    public string Line { get; set; }
}
```

### Layer mapping summary

| Concern | Layer | Folder |
|---|---|---|
| HTTP in/out shapes | Frontend DTOs | `api/dtos/` |
| Business logic, algorithms, command handling | Domain | `api/domain/` |
| Cosmos read/write | Storage documents | `api/storage/documents/` |

Each Azure Function maps DTOs ↔ Domain objects. Each storage call maps Domain objects ↔ Storage documents. The `JsonStore<T>` wrapper is the only code that ever touches a storage document type.

---

## Logging Pattern

### Rule: every entry point uses `LogBuffer.Wrap()`

Every Azure Function and every Telegram command handler must use the `LogBuffer.Wrap()` pattern. This means:

1. The `[Function]`-attributed method is a thin wrapper that calls `LogBuffer.Wrap()`.
2. The real implementation is in a private method that receives `(HttpRequest req, LogBuffer log)`.
3. All `log.Info()`, `log.Error()`, and `log.OperationResult()` calls are made on that `LogBuffer`.
4. On method exit (success or exception), `LogBuffer.Close()` fires automatically, flushing to Telegram.

### Standard wrapper pattern

```csharp
[Function("CreateGroceryList")]
public async Task<IActionResult> CreateGroceryListEntry(HttpRequest req)
    => await LogBuffer.Wrap("CreateGroceryList", req, CreateGroceryListImpl);

private async Task<IActionResult> CreateGroceryListImpl(HttpRequest req, LogBuffer log)
{
    log.Info("Reading request body");
    // ... implementation using log throughout
}
```

### Telegram command handlers

Command handlers receive a `CommandContext` that already holds a `LogBuffer`. They do **not** create their own — they write to `context.Log`.

```csharp
public class AddItemCommand : ICommand
{
    public string Name => "/add";
    public bool RequiresAuthorization => true;

    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        context.Log.Info("Adding item: {0}", string.Join(", ", context.Args));
        // ... implementation
    }
}
```

### What gets logged

Every log entry automatically includes:
- UTC timestamp
- Operation name (set when `LogBuffer` is created)
- Log level: `*INFO*`, `*ERROR*`, `*EXCEPTION*`
- `OperationResult<T>` helper logs only on non-success

The Telegram channel and `LogBuffer` implementation are unchanged; this plan simply mandates their use everywhere.

---

## Webhook Command Dispatch Pattern

Replace the current monolithic switch statement in `TelegramWebhook.cs` with a proper dispatch loop.

### ICommand implementations

Each command is a separate class in `api/commands/telegram/`:

```
api/commands/telegram/
├── PingCommand.cs
├── AddItemCommand.cs
├── RemoveItemCommand.cs
├── ListCommand.cs
└── (future commands here)
```

### CommandDispatcher

```csharp
// api/commands/telegram/CommandDispatcher.cs
public class CommandDispatcher
{
    private readonly IReadOnlyList<ICommand> _commands;
    private static readonly HashSet<long> _authorizedUserIds = new() { 5241310949, 5411752675 };

    public CommandDispatcher(IEnumerable<ICommand> commands)
    {
        _commands = commands.ToList();
    }

    public async Task<string> DispatchAsync(TelegramUpdate update, LogBuffer log)
    {
        var msg = update.Message;
        var chatId = msg.Chat.Id;
        var fromId = msg.From.Id;
        var text = msg.Text?.Trim() ?? string.Empty;

        var commandName = text.Split(' ')[0].ToLower();
        var args = text.Split(' ').Skip(1).ToArray();

        log.Info("Received command '{0}' from user {1}", commandName, fromId);

        var command = _commands.FirstOrDefault(c => c.Name == commandName);
        if (command == null)
        {
            log.Info("Unknown command: {0}", commandName);
            await Telegram.SendAsync(chatId, "🤖 Unknown command");
            return "unknown";
        }

        if (command.RequiresAuthorization && !_authorizedUserIds.Contains(fromId))
        {
            log.Error("Unauthorized user {0} attempted '{1}'", fromId, commandName);
            await Telegram.SendAsync(chatId, "❌ Unauthorized");
            return "unauthorized";
        }

        var context = new CommandContext
        {
            ChatId = chatId,
            FromUserId = fromId,
            Username = msg.From.Username,
            RawText = text,
            CommandName = commandName,
            Args = args,
            Log = log
        };

        var result = await command.ExecuteAsync(context);
        await Telegram.SendAsync(chatId, result.Message);
        return result.Success ? "ok" : "error";
    }
}
```

### Refactored TelegramWebhook function

```csharp
// api/TelegramWebhook.cs
[Function("TelegramWebhook")]
public async Task<IActionResult> InvokeWebhook(HttpRequest req)
    => await LogBuffer.Wrap("TelegramWebhook", req, InvokeWebhookImpl);

private async Task<IActionResult> InvokeWebhookImpl(HttpRequest req, LogBuffer log)
{
    var body = await new StreamReader(req.Body).ReadToEndAsync();
    var update = JsonSerializer.Deserialize<TelegramUpdate>(body);
    if (update?.Message == null)
        return new OkResult();

    await _dispatcher.DispatchAsync(update, log);
    return new OkResult();
}
```

The webhook itself does **only** three things: deserialize the payload, call the dispatcher, return 200. All command logic lives in the command classes.

### Adding a new command

1. Create `api/commands/telegram/MyNewCommand.cs` implementing `ICommand`.
2. Register it in `Program.cs` (or via DI container).
3. No changes to `TelegramWebhook.cs` or the dispatcher.

---

## Migration Plan

The existing code works and is in production. Refactor in phases so nothing breaks.

### Phase 1 — Storage document layer

- Create `api/storage/documents/` folder.
- Move `GroceryListDB`, `AddressBookEntry`, `SecretSantaConfig`, `SecretSantaEvent`, `RefreshableToken`, `AuthLog` into the new folder, renamed to `*Document`.
- Update `JsonStore<T>` references throughout. No behavior changes.

### Phase 2 — DTO layer

- Create `api/dtos/` folder with all request/response records.
- Update Azure Function methods to accept DTOs instead of raw `JsonDocument`/`string` parsing.
- Add mapping helpers (static methods or a `Mapper` class) to convert: DTO → Domain → Document and back.

### Phase 3 — Domain layer

- Create `api/domain/` folder.
- Extract business logic from Functions into domain classes (e.g., `GroceryList.Apply()`).
- Algorithms (e.g., Secret Santa `Assign.cs`) move to `api/domain/algorithms/`.

### Phase 4 — Logging standardization

- Audit every `[Function]`-attributed method.
- Wrap any that don't already use `LogBuffer.Wrap()`.
- Remove any ad-hoc `ILogger` or `Console.Write` calls.

### Phase 5 — Command dispatch

- Create `api/commands/telegram/` folder with `ICommand`, `CommandContext`, `CommandResult`, `CommandDispatcher`.
- Extract each command from `TelegramWebhook.cs` into its own class.
- Wire up `CommandDispatcher` in `Program.cs` via DI.
- Slim down `TelegramWebhook.cs` to the pattern shown above.

### Phase 6 — Unit tests

- Create `api.tests/` project (xUnit) alongside `api/`.
- Write tests in parallel with each phase above (see Unit Tests section).

---

## Unit Tests

Unit tests live in a separate `api.tests/` xUnit project. The three-layer architecture makes this straightforward: the domain layer has no external dependencies and is the primary target. External dependencies (Cosmos, Telegram, MailJet) are never hit in unit tests.

### What to test

#### Domain logic — highest priority

These are pure functions with no I/O. Test them exhaustively.

```csharp
// GroceryListTests.cs
[Fact] void Apply_AddsItems();
[Fact] void Apply_RemovesItems();
[Fact] void Apply_AddAndRemoveInOneCall();
[Fact] void Apply_IsCaseInsensitiveOnRemove();
[Fact] void Apply_DeduplicatesOnAdd();
[Fact] void Apply_SortsResult();

// AddressBookEntryTests.cs
[Fact] void MailingLabel_OmitsNullApartment();
[Fact] void MailingLabel_UsesMailingNameOverFullName();
[Fact] void MailingLabel_FormatsCorrectly();

// SecretSantaConfigTests.cs
[Fact] void Validate_PassesForValidConfig();
[Fact] void Validate_FailsWhenRestrictionReferencesUnknownPerson();

// SecretSantaEventTests.cs
[Fact] void Validate_PassesForCompleteValidAssignment();
[Fact] void Validate_FailsWhenSomeoneIsTheirOwnSanta();
[Fact] void Validate_FailsWhenRestrictionViolated();
[Fact] void Validate_FailsWhenParticipantNotInConfig();
[Fact] void Validate_FailsWhenAssignmentIsNotACompleteCycle();
```

#### Secret Santa assignment algorithm

The assignment algorithm is the most complex logic in the codebase and the most valuable to cover.

```csharp
// AssignTests.cs
[Fact] void Assign_ProducesValidAssignment();
[Fact] void Assign_RespectsRestrictions();
[Fact] void Assign_NobodyIsTheirOwnSanta();
[Fact] void Assign_AllParticipantsReceiveASanta();
[Fact] void Assign_ProducesACompleteCycle();
[Theory] void Assign_WorksForVariousGroupSizes(int participantCount);
[Fact] void Assign_ThrowsOrFailsWhenNoValidAssignmentExists();  // e.g. two people, restriction between them
```

#### Command dispatch

Test the dispatcher's routing and authorization logic in isolation. Use a fake `ICommand` — no real Telegram calls.

```csharp
// CommandDispatcherTests.cs
[Fact] void Dispatch_RoutesToCorrectCommand();
[Fact] void Dispatch_ReturnsUnknownForUnrecognizedCommand();
[Fact] void Dispatch_RejectsUnauthorizedUserForRestrictedCommand();
[Fact] void Dispatch_AllowsUnauthorizedUserForOpenCommand();  // e.g. /ping
[Fact] void Dispatch_ParsesArgsCorrectly();
```

#### Individual command handlers

Each command is tested with a fake `CommandContext` (real `CommandContext` built with a no-op `LogBuffer` and a mock storage layer).

```csharp
// PingCommandTests.cs
[Fact] void Execute_ReturnsPong();

// AddItemCommandTests.cs
[Fact] void Execute_AddsItemToList();
[Fact] void Execute_ReturnsFormattedList();
[Fact] void Execute_ReturnsErrorWhenStorageFails();

// RemoveItemCommandTests.cs
[Fact] void Execute_RemovesItemFromList();
[Fact] void Execute_ReturnsErrorWhenStorageFails();

// ListCommandTests.cs
[Fact] void Execute_ReturnsCurrentList();
[Fact] void Execute_ReturnsErrorWhenStorageFails();
```

#### DTO mapping

Verify that the VersionToken round-trip is correct — a value set on a response can be echoed back and recovered on the next request.

```csharp
// VersionTokenTests.cs
[Fact] void VersionToken_RoundTripsEtagThroughResponse();
[Fact] void VersionToken_UpdateRequestMapsBackToEtag();
```

### What NOT to test

| Concern | Reason |
|---|---|
| `JsonStore<T>` read/write | Requires live Cosmos or emulator — integration territory |
| `Telegram.SendAsync()` | External service; mock would test nothing useful |
| `Email.SendAsync()` | External service |
| Azure Function entry points | Thin wrappers; covered by integration tests if needed |
| `LogBuffer` I/O | Only the flush-to-Telegram path touches the network; internal accumulation logic is trivial |

### Project structure

```
api.tests/
├── domain/
│   ├── GroceryListTests.cs
│   ├── AddressBookEntryTests.cs
│   ├── SecretSantaConfigTests.cs
│   └── SecretSantaEventTests.cs
├── algorithms/
│   └── AssignTests.cs
├── commands/
│   ├── CommandDispatcherTests.cs
│   ├── PingCommandTests.cs
│   ├── AddItemCommandTests.cs
│   ├── RemoveItemCommandTests.cs
│   └── ListCommandTests.cs
├── dtos/
│   └── VersionTokenTests.cs
└── api.tests.csproj
```

### Fake helpers

A small set of shared test helpers keeps test setup concise:

```csharp
// Fakes/FakeCommand.cs — controllable ICommand for dispatcher tests
// Fakes/NoOpLogBuffer.cs — LogBuffer that discards output (no Telegram calls)
// Fakes/FakeGroceryListStore.cs — in-memory IJsonStore<GroceryListDocument>
```

The storage fake implements the same `JsonStore` interface (which should be extracted to `IJsonStore<T>` as part of Phase 3), making command handler tests independent of Cosmos entirely.
