using System.Net;
using System.Text.Json;
using Boltzenberg.Functions.Comms;
using Boltzenberg.Functions.DataModels.Auth;
using Boltzenberg.Functions.Domain;
using Boltzenberg.Functions.Domain.Algorithms;
using Boltzenberg.Functions.Dtos.SecretSanta;
using Boltzenberg.Functions.Logging;
using Boltzenberg.Functions.Storage;
using Boltzenberg.Functions.Storage.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Boltzenberg.Functions;

public class SecretSanta
{
    private readonly IJsonStore<SecretSantaConfigDocument> _configStore;
    private readonly IJsonStore<SecretSantaEventDocument> _eventStore;

    public SecretSanta(
        IJsonStore<SecretSantaConfigDocument> configStore,
        IJsonStore<SecretSantaEventDocument> eventStore)
    {
        _configStore = configStore;
        _eventStore = eventStore;
    }

    // --- Mapping helpers ---

    private static Domain.SecretSantaConfig DomainConfigFromDocument(SecretSantaConfigDocument doc)
        => new Domain.SecretSantaConfig
        {
            People = doc.People.Select(p => new Domain.SecretSantaConfig.Person(p.Name, p.Email)).ToList(),
            Restrictions = doc.Restrictions.Select(r => new Domain.SecretSantaConfig.Restriction(r.Person1Email, r.Person2Email)).ToList()
        };

    private static Domain.SecretSantaEvent DomainEventFromDocument(SecretSantaEventDocument doc)
        => new Domain.SecretSantaEvent
        {
            EventId = doc.id,
            IsRunning = doc.IsRunning,
            GroupName = doc.GroupName,
            Year = doc.Year,
            Participants = doc.Participants.Select(p =>
                new Domain.SecretSantaEvent.Participant(p.Name, p.Email, p.SantaForName, p.SantaForEmail)).ToList()
        };

    private static SecretSantaConfigResponse ResponseFromConfigDocument(SecretSantaConfigDocument doc)
        => new SecretSantaConfigResponse(
            VersionToken: doc._etag ?? string.Empty,
            People: doc.People.Select(p => new PersonDto(p.Name, p.Email)).ToList(),
            Restrictions: doc.Restrictions.Select(r => new RestrictionDto(r.Person1Email, r.Person2Email)).ToList()
        );

    private static SecretSantaEventResponse ResponseFromEventDocument(SecretSantaEventDocument doc)
        => new SecretSantaEventResponse(
            EventId: doc.id,
            VersionToken: doc._etag ?? string.Empty,
            GroupName: doc.GroupName,
            Year: doc.Year,
            IsRunning: doc.IsRunning,
            Participants: doc.Participants.Select(p =>
                new ParticipantDto(p.Name, p.Email, p.SantaForName, p.SantaForEmail)).ToList()
        );

    [Function("SecretSantaAdminGetConfig")]
    public async Task<IActionResult> SecretSantaAdminGetConfigUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminGetConfig", req, SecretSantaAdminGetConfig);
    private async Task<IActionResult> SecretSantaAdminGetConfig(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        var config = await _configStore.ReadAsync(SecretSantaConfigDocument.PartitionKey, SecretSantaConfigDocument.DocId);
        if (config.Code == ResultCode.Success && config.Entity != null)
        {
            return new OkObjectResult(JsonSerializer.Serialize(ResponseFromConfigDocument(config.Entity)));
        }
        else
        {
            log.OperationResult("Failed to get the secret santa config", config);
            return new BadRequestResult();
        }
    }

    [Function("SecretSantaAdminUpdateConfig")]
    public async Task<IActionResult> SecretSantaAdminUpdateConfigUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminUpdateConfig", req, SecretSantaAdminUpdateConfig);
    private async Task<IActionResult> SecretSantaAdminUpdateConfig(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
        {
            log.Error("No request body!");
            return new BadRequestResult();
        }

        SecretSantaConfigUpdateRequest? request = JsonSerializer.Deserialize<SecretSantaConfigUpdateRequest>(body);
        if (request == null)
        {
            log.Error("Failed to deserialize request body '{0}'", body);
            return new BadRequestResult();
        }

        // Build domain object for validation
        var domainConfig = new Domain.SecretSantaConfig
        {
            People = request.People.Select(p => new Domain.SecretSantaConfig.Person(p.Name, p.Email)).ToList(),
            Restrictions = request.Restrictions.Select(r => new Domain.SecretSantaConfig.Restriction(r.Person1Email, r.Person2Email)).ToList()
        };

        // Throws if the entry is invalid
        domainConfig.Validate();

        var doc = new SecretSantaConfigDocument
        {
            _etag = request.VersionToken,
            People = request.People.Select(p => new SecretSantaConfigDocument.PersonRecord { Name = p.Name, Email = p.Email }).ToList(),
            Restrictions = request.Restrictions.Select(r => new SecretSantaConfigDocument.RestrictionRecord { Person1Email = r.Person1Email, Person2Email = r.Person2Email }).ToList()
        };

        var result = await _configStore.UpdateAsync(doc);
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            return new OkObjectResult(JsonSerializer.Serialize(ResponseFromConfigDocument(result.Entity)));
        }
        else
        {
            log.OperationResult("Failed to update the config", result);
            if (result.Code == ResultCode.PreconditionFailed)
            {
                return new StatusCodeResult((int)HttpStatusCode.Conflict);
            }
        }

        return new BadRequestResult();
    }

    [Function("SecretSantaAdminCreateEvent")]
    public async Task<IActionResult> SecretSantaAdminCreateEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminCreateEvent", req, SecretSantaAdminCreateEvent);
    private async Task<IActionResult> SecretSantaAdminCreateEvent(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
        {
            log.Error("No request body!");
            return new BadRequestResult();
        }

        SecretSantaEventUpdateRequest? request = JsonSerializer.Deserialize<SecretSantaEventUpdateRequest>(body);
        if (request == null)
        {
            log.Error("Failed to deserialize the request body '{0}'", body);
            return new BadRequestObjectResult("Failed to deserialize the event");
        }

        var config = await _configStore.ReadAsync(SecretSantaConfigDocument.PartitionKey, SecretSantaConfigDocument.DocId);
        if (config.Code != ResultCode.Success || config.Entity == null)
        {
            log.OperationResult("Failed to read the config", config);
            return new BadRequestResult();
        }

        var domainConfig = DomainConfigFromDocument(config.Entity);

        // Build domain event for validation; created events don't start running
        var domainEvent = new Domain.SecretSantaEvent
        {
            EventId = request.EventId,
            IsRunning = false,
            GroupName = request.GroupName,
            Year = request.Year,
            Participants = request.Participants.Select(p =>
                new Domain.SecretSantaEvent.Participant(p.Name, p.Email, p.SantaForName, p.SantaForEmail)).ToList()
        };

        // Throws if the entry is invalid
        domainEvent.Validate(domainConfig);

        var doc = new SecretSantaEventDocument
        {
            id = request.EventId,
            IsRunning = false,
            GroupName = request.GroupName,
            Year = request.Year,
            Participants = request.Participants.Select(p => new ParticipantDocument
            {
                Name = p.Name,
                Email = p.Email,
                SantaForName = p.SantaForName,
                SantaForEmail = p.SantaForEmail
            }).ToList()
        };

        var result = await _eventStore.CreateAsync(doc);
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            log.Info("New Secret Santa Event created: {0} {1}", result.Entity.GroupName, result.Entity.Year);
            return new OkObjectResult(JsonSerializer.Serialize(ResponseFromEventDocument(result.Entity)));
        }
        else
        {
            log.OperationResult("Failed to create the event", result);
            if (result.Code == ResultCode.PreconditionFailed)
            {
                return new StatusCodeResult((int)HttpStatusCode.Conflict);
            }
        }

        return new BadRequestResult();
    }

    [Function("SecretSantaAdminUpdateEvent")]
    public async Task<IActionResult> SecretSantaAdminUpdateEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminUpdateEvent", req, SecretSantaAdminUpdateEvent);
    private async Task<IActionResult> SecretSantaAdminUpdateEvent(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
        {
            log.Error("No request body!");
            return new BadRequestResult();
        }

        SecretSantaEventUpdateRequest? request = JsonSerializer.Deserialize<SecretSantaEventUpdateRequest>(body);
        if (request == null)
        {
            log.Error("Failed to deserialize the request body '{0}'", body);
            return new BadRequestResult();
        }

        var config = await _configStore.ReadAsync(SecretSantaConfigDocument.PartitionKey, SecretSantaConfigDocument.DocId);
        if (config.Code != ResultCode.Success || config.Entity == null)
        {
            log.OperationResult("Failed to read the config", config);
            return new BadRequestResult();
        }

        var domainConfig = DomainConfigFromDocument(config.Entity);

        var domainEvent = new Domain.SecretSantaEvent
        {
            EventId = request.EventId,
            IsRunning = false,
            GroupName = request.GroupName,
            Year = request.Year,
            Participants = request.Participants.Select(p =>
                new Domain.SecretSantaEvent.Participant(p.Name, p.Email, p.SantaForName, p.SantaForEmail)).ToList()
        };

        if (domainEvent.IsRunning)
        {
            log.Error("Tried to update an event that is already running.  {0} {1}", domainEvent.GroupName, domainEvent.Year);
            return new BadRequestObjectResult("Can't update an event that is running!");
        }

        // Throws if the entry is invalid
        domainEvent.Validate(domainConfig);

        var doc = new SecretSantaEventDocument
        {
            id = request.EventId,
            _etag = request.VersionToken,
            IsRunning = false,
            GroupName = request.GroupName,
            Year = request.Year,
            Participants = request.Participants.Select(p => new ParticipantDocument
            {
                Name = p.Name,
                Email = p.Email,
                SantaForName = p.SantaForName,
                SantaForEmail = p.SantaForEmail
            }).ToList()
        };

        var result = await _eventStore.UpdateAsync(doc);
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            return new OkObjectResult(JsonSerializer.Serialize(ResponseFromEventDocument(result.Entity)));
        }
        else if (result.Code == ResultCode.PreconditionFailed)
        {
            log.Error("OCC failure");
            return new StatusCodeResult((int)HttpStatusCode.Conflict);
        }
        else
        {
            log.OperationResult("Failed to update the event", result);
        }

        return new BadRequestResult();
    }

    [Function("SecretSantaAdminStartEvent")]
    public async Task<IActionResult> SecretSantaAdminStartEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminStartEvent", req, SecretSantaAdminStartEvent);
    private async Task<IActionResult> SecretSantaAdminStartEvent(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
        {
            log.Error("No request body!");
            return new BadRequestResult();
        }

        SecretSantaStartEventRequest? request = JsonSerializer.Deserialize<SecretSantaStartEventRequest>(body);
        if (request == null)
        {
            log.Error("Failed to deserialize the request body '{0}'", body);
            return new BadRequestResult();
        }

        // Read the event from storage
        var evtResult = await _eventStore.ReadAsync(SecretSantaEventDocument.PartitionKey, request.EventId);
        if (evtResult.Code != ResultCode.Success || evtResult.Entity == null)
        {
            log.OperationResult("Failed to read the event", evtResult);
            return new BadRequestResult();
        }

        var evtDoc = evtResult.Entity;
        evtDoc._etag = request.VersionToken;

        if (evtDoc.IsRunning)
        {
            log.Error("Tried to start a running event!");
            return new BadRequestObjectResult("Can't start an event that is running!");
        }

        var config = await _configStore.ReadAsync(SecretSantaConfigDocument.PartitionKey, SecretSantaConfigDocument.DocId);
        if (config.Code != ResultCode.Success || config.Entity == null)
        {
            log.OperationResult("Failed to read the config", config);
            return new BadRequestResult();
        }

        var domainConfig = DomainConfigFromDocument(config.Entity);

        var allEventsResult = await _eventStore.ReadAllAsync(SecretSantaEventDocument.PartitionKey);
        if (allEventsResult.Code != ResultCode.Success || allEventsResult.Entity == null)
        {
            log.OperationResult("Failed to read the event list", allEventsResult);
            return new BadRequestResult();
        }

        var domainCurrentEvent = DomainEventFromDocument(evtDoc);
        var domainAllEvents = allEventsResult.Entity.Select(DomainEventFromDocument).ToList();

        // Set Assignments
        SecretSantaAssign.AssignSantas(domainCurrentEvent, domainAllEvents, domainConfig);

        // Map assignments back to the document
        evtDoc.IsRunning = true;
        evtDoc.Participants = domainCurrentEvent.Participants.Select(p => new ParticipantDocument
        {
            Name = p.Name,
            Email = p.Email,
            SantaForName = p.SantaForName,
            SantaForEmail = p.SantaForEmail
        }).ToList();

        // Validate via domain object
        var domainValidate = DomainEventFromDocument(evtDoc);
        domainValidate.Validate(domainConfig);

        var result = await _eventStore.UpdateAsync(evtDoc);
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            return new OkObjectResult(JsonSerializer.Serialize(ResponseFromEventDocument(result.Entity)));
        }
        else if (result.Code == ResultCode.PreconditionFailed)
        {
            log.Error("OCC failure");
            return new StatusCodeResult((int)HttpStatusCode.Conflict);
        }
        else
        {
            log.OperationResult("Failed to update the event", result);
        }

        return new BadRequestResult();
    }

    [Function("SecretSantaEmailAssignment")]
    public async Task<IActionResult> SecretSantaEmailAssignmentUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaEmailAssignment", req, SecretSantaEmailAssignment);
    private async Task<IActionResult> SecretSantaEmailAssignment(HttpRequest req, LogBuffer log)
    {
        string? participantEmail = req.Query["participant"];
        if (string.IsNullOrEmpty(participantEmail))
        {
            log.Error("No participant query parameter");
            return new BadRequestObjectResult("Missing the participant query parameter");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
        {
            log.Error("No request body!");
            return new BadRequestObjectResult("No request body!");
        }

        SecretSantaEventUpdateRequest? request = JsonSerializer.Deserialize<SecretSantaEventUpdateRequest>(body);
        if (request == null)
        {
            log.Error("Failed to deserialize the request body '{0}'", body);
            return new BadRequestObjectResult("Failed to deserialize the event");
        }

        if (!request.Participants.Any(p => p.Email == participantEmail))
        {
            log.Error("Failed to find {0} in the participant list for {1} {2}", participantEmail, request.GroupName, request.Year);
            return new BadRequestObjectResult("Participant is not part of the event!");
        }

        var participant = request.Participants.First(p => p.Email == participantEmail);

        bool result = await Email.SendSantaMailAsync(
            participant.Name,
            participant.Email,
            "Secret Santa Assignment",
            string.Format("Hi {0}!  Your secret santa assignment for {1} is {2} ({3}).", participant.Name, request.EventId, participant.SantaForName, participant.SantaForEmail));

        if (result)
        {
            return new OkResult();
        }
        else
        {
            log.Error("Failed to send the email");
            return new BadRequestObjectResult("Failed to send the email");
        }
    }

    [Function("SecretSantaSendSantaMail")]
    public async Task<IActionResult> SecretSantaSendSantaMailUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaSendSantaMail", req, SecretSantaSendSantaMail);
    private async Task<IActionResult> SecretSantaSendSantaMail(HttpRequest req, LogBuffer log)
    {
        string? evtId = req.Query["id"];
        if (string.IsNullOrEmpty(evtId))
        {
            log.Error("Missing the id query parameter");
            return new BadRequestObjectResult("Missing the id query parameter");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
        {
            log.Error("Missing the email message in the post body");
            return new BadRequestObjectResult("Missing the email message in the POST body");
        }

        var evtResult = await _eventStore.ReadAsync(SecretSantaEventDocument.PartitionKey, evtId);
        if (evtResult.Code != ResultCode.Success || evtResult.Entity == null)
        {
            log.OperationResult("Failed to get the event with id " + evtId, evtResult);
            return new BadRequestObjectResult("Failed to find the event with id " + evtId);
        }

        List<Tuple<string, string>> toAddresses = new List<Tuple<string, string>>();
        foreach (var participant in evtResult.Entity.Participants)
        {
            toAddresses.Add(new Tuple<string, string>(participant.Name, participant.Email));
        }

        bool result = await Email.SendSantaMailAsync(
            toAddresses,
            string.Format("SantaMail for event '{0}'", evtResult.Entity.id),
            body);

        if (result)
        {
            return new OkResult();
        }
        else
        {
            log.Error("Failed to send the santamail");
            return new BadRequestObjectResult("Failed to send the mail");
        }
    }

    [Function("SecretSantaGetEvent")]
    public async Task<IActionResult> SecretSantaGetEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaGetEvent", req, SecretSantaGetEvent);
    private async Task<IActionResult> SecretSantaGetEvent(HttpRequest req, LogBuffer log)
    {
        string? evtId = req.Query["id"];
        if (string.IsNullOrEmpty(evtId))
        {
            log.Error("Missing the id query parameter");
            return new BadRequestObjectResult("Missing the id query parameter");
        }

        var evt = await _eventStore.ReadAsync(SecretSantaEventDocument.PartitionKey, evtId);
        if (evt.Code == ResultCode.Success && evt.Entity != null)
        {
            return new OkObjectResult(JsonSerializer.Serialize(ResponseFromEventDocument(evt.Entity)));
        }
        else
        {
            log.OperationResult("Failed to read the event", evt);
            return new BadRequestResult();
        }
    }

    [Function("SecretSantaGetAllEvents")]
    public async Task<IActionResult> SecretSantaGetAllEventsUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaGetAllEvents", req, SecretSantaGetAllEvents);
    private async Task<IActionResult> SecretSantaGetAllEvents(HttpRequest req, LogBuffer log)
    {
        var evts = await _eventStore.ReadAllAsync(SecretSantaEventDocument.PartitionKey);
        if (evts.Code == ResultCode.Success && evts.Entity != null)
        {
            var responses = evts.Entity.Select(ResponseFromEventDocument).ToList();
            return new OkObjectResult(JsonSerializer.Serialize(responses));
        }
        else
        {
            log.OperationResult("Failed to get the events", evts);
            return new BadRequestResult();
        }
    }

    [Function("SecretSantaAdminGetCannedConfig")]
    public async Task<IActionResult> SecretSantaAdminGetCannedConfigUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminGetCannedConfig", req, SecretSantaAdminGetCannedConfig);
    private async Task<IActionResult> SecretSantaAdminGetCannedConfig(HttpRequest req, LogBuffer log)
    {
        await Task.Yield();
        var response = new SecretSantaConfigResponse(
            VersionToken: string.Empty,
            People: new List<PersonDto>
            {
                new PersonDto("Test1", "test.person.1@gmail.com"),
                new PersonDto("Test2", "test.person.2@gmail.com"),
                new PersonDto("Test3", "test.person.3@gmail.com"),
                new PersonDto("Test4", "test.person.4@gmail.com")
            },
            Restrictions: new List<RestrictionDto>
            {
                new RestrictionDto("test.person.1@gmail.com", "test.person.2@gmail.com"),
                new RestrictionDto("test.person.3@gmail.com", "test.person.4@gmail.com")
            }
        );
        return new OkObjectResult(JsonSerializer.Serialize(response));
    }

    [Function("SecretSantaAdminGetCannedEvent")]
    public async Task<IActionResult> SecretSantaAdminGetCannedEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminGetCannedEvent", req, SecretSantaAdminGetCannedEvent);
    private async Task<IActionResult> SecretSantaAdminGetCannedEvent(HttpRequest req, LogBuffer log)
    {
        await Task.Yield();
        var response = new SecretSantaEventResponse(
            EventId: "Secret Santa Canned Event",
            VersionToken: string.Empty,
            GroupName: string.Empty,
            Year: 0,
            IsRunning: false,
            Participants: new List<ParticipantDto>
            {
                new ParticipantDto("Test1", "test.person.1@gmail.com", "Test3", "test.person.3@gmail.com"),
                new ParticipantDto("Test2", "test.person.2@gmail.com", "Test4", "test.person.4@gmail.com"),
                new ParticipantDto("Test3", "test.person.3@gmail.com", "Test2", "test.person.2@gmail.com"),
                new ParticipantDto("Test4", "test.person.4@gmail.com", "Test1", "test.person.1@gmail.com")
            }
        );
        return new OkObjectResult(JsonSerializer.Serialize(response));
    }
}
