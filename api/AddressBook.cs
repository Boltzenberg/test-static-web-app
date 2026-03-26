using System.Net;
using System.Text.Json;
using Boltzenberg.Functions.DataModels.Auth;
using Boltzenberg.Functions.Domain;
using Boltzenberg.Functions.Dtos.AddressBook;
using Boltzenberg.Functions.Logging;
using Boltzenberg.Functions.Storage;
using Boltzenberg.Functions.Storage.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Boltzenberg.Functions;

public class AddressBook
{
    private readonly IJsonStore<AddressBookDocument> _store;

    public AddressBook(IJsonStore<AddressBookDocument> store)
    {
        _store = store;
    }

    // --- Mapping helpers ---

    // Cosmos stores HolidayCard as a string (legacy values include "Holiday", "Yes", "").
    // Treat any non-empty string as true.
    private static bool HolidayCardFromDoc(string? value) => !string.IsNullOrEmpty(value);
    private static string HolidayCardToDoc(bool value) => value ? "Yes" : "";

    private static AddressBookDocument DocumentFromRequest(AddressBookEntryRequest req)
        => new AddressBookDocument
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Street = req.Street,
            Apartment = req.Apartment,
            City = req.City,
            State = req.State,
            ZipCode = req.ZipCode,
            PhoneNumber = req.PhoneNumber,
            MailingName = req.MailingName,
            OtherPeople = req.OtherPeople,
            HolidayCard = HolidayCardToDoc(req.HolidayCard)
        };

    private static AddressBookDocument DocumentFromUpdateRequest(AddressBookEntryUpdateRequest req)
        => new AddressBookDocument
        {
            id = req.Id,
            _etag = req.VersionToken,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Street = req.Street,
            Apartment = req.Apartment,
            City = req.City,
            State = req.State,
            ZipCode = req.ZipCode,
            PhoneNumber = req.PhoneNumber,
            MailingName = req.MailingName,
            OtherPeople = req.OtherPeople,
            HolidayCard = HolidayCardToDoc(req.HolidayCard)
        };

    private static AddressBookEntryResponse ResponseFromDocument(AddressBookDocument doc)
    {
        var holidayCard = HolidayCardFromDoc(doc.HolidayCard);
        var domain = new AddressBookEntry
        {
            Id = doc.id,
            FirstName = doc.FirstName,
            LastName = doc.LastName,
            Street = doc.Street,
            Apartment = doc.Apartment,
            City = doc.City,
            State = doc.State,
            ZipCode = doc.ZipCode,
            PhoneNumber = doc.PhoneNumber,
            MailingName = doc.MailingName,
            OtherPeople = doc.OtherPeople,
            HolidayCard = holidayCard
        };

        return new AddressBookEntryResponse(
            Id: doc.id,
            VersionToken: doc._etag ?? string.Empty,
            FirstName: doc.FirstName,
            LastName: doc.LastName,
            Street: doc.Street,
            Apartment: doc.Apartment,
            City: doc.City,
            State: doc.State,
            ZipCode: doc.ZipCode,
            PhoneNumber: doc.PhoneNumber,
            MailingName: doc.MailingName,
            OtherPeople: doc.OtherPeople,
            HolidayCard: holidayCard,
            MailingLabel: domain.MailingLabel
        );
    }

    [Function("AddressBookAddEntry")]
    public async Task<IActionResult> AddressBookAddEntryUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("AddressBookAddEntry", req, AddressBookAddEntry);
    private async Task<IActionResult> AddressBookAddEntry(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForAddressBook(req))
        {
            log.Error("Attempted unauthorized access");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
        {
            log.Error("No request body");
            return new BadRequestResult();
        }

        AddressBookEntryRequest? request = JsonSerializer.Deserialize<AddressBookEntryRequest>(body);
        if (request == null)
        {
            log.Error("Failed to deserialize request body '{0}'", body);
            return new BadRequestObjectResult("Failed to deserialize the entry to add");
        }

        var doc = DocumentFromRequest(request);
        doc.id = Guid.NewGuid().ToString();

        var result = await _store.CreateAsync(doc);
        if (result.Entity != null && result.Code == ResultCode.Success)
        {
            log.Info("Created address book entry for '{0} {1}'", request.FirstName, request.LastName);
            return new OkObjectResult(JsonSerializer.Serialize(ResponseFromDocument(result.Entity)));
        }
        else
        {
            log.Error("Failed to create the address book entry: {0}", result.Code);
        }

        return new BadRequestResult();
    }

    [Function("AddressBookUpdateEntry")]
    public async Task<IActionResult> AddressBookUpdateEntryUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("AddressBookUpdateEntry", req, AddressBookUpdateEntry);
    private async Task<IActionResult> AddressBookUpdateEntry(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForAddressBook(req))
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

        AddressBookEntryUpdateRequest? request = JsonSerializer.Deserialize<AddressBookEntryUpdateRequest>(body);
        if (request == null)
        {
            log.Error("Failed to deserialize request body {0}", body);
            return new BadRequestResult();
        }

        var doc = DocumentFromUpdateRequest(request);
        var result = await _store.UpdateAsync(doc);
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            return new OkObjectResult(JsonSerializer.Serialize(ResponseFromDocument(result.Entity)));
        }
        else if (result.Code == ResultCode.PreconditionFailed)
        {
            log.Error("OCC failure");
            return new StatusCodeResult((int)HttpStatusCode.Conflict);
        }
        else
        {
            log.OperationResult("Failed to update the entry", result);
        }

        return new BadRequestResult();
    }

    [Function("AddressBookDeleteEntry")]
    public async Task<IActionResult> AddressBookDeleteEntryUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("AddressBookDeleteEntry", req, AddressBookDeleteEntry);
    private async Task<IActionResult> AddressBookDeleteEntry(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForAddressBook(req))
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

        AddressBookEntryUpdateRequest? request = JsonSerializer.Deserialize<AddressBookEntryUpdateRequest>(body);
        if (request == null)
        {
            log.Error("Failed to deserialize request body '{0}'", body);
            return new BadRequestObjectResult("Failed to deserialize the entry to delete");
        }

        var doc = DocumentFromUpdateRequest(request);
        var result = await _store.DeleteAsync(doc);
        if (result.Code == ResultCode.Success)
        {
            return new NoContentResult();
        }
        else if (result.Code == ResultCode.PreconditionFailed)
        {
            log.Error("OCC failure");
            return new StatusCodeResult((int)HttpStatusCode.Conflict);
        }
        else
        {
            log.OperationResult("Failed to delete the entry", result);
        }

        return new BadRequestResult();
    }

    [Function("AddressBookReadAll")]
    public async Task<IActionResult> AddressBookReadAllUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("AddressBookReadAll", req, AddressBookReadAll);
    private async Task<IActionResult> AddressBookReadAll(HttpRequest req, LogBuffer log)
    {
        log.Error("Testing logging");
        if (!await AuthZChecker.IsAuthorizedForAddressBook(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        log.Info("Reading from the store");
        var entries = await _store.ReadAllAsync(AddressBookDocument.PartitionKey);
        if (entries.Code == ResultCode.Success && entries.Entity != null)
        {
            log.Info("Successfully read from the store");
            var responses = entries.Entity.Select(ResponseFromDocument).ToList();
            log.Info("Generated the responses");
            return new OkObjectResult(JsonSerializer.Serialize(responses));
        }
        else
        {
            log.OperationResult("Failed to read the entries", entries);
            return new BadRequestResult();
        }
    }

    [Function("AddressBookReadOne")]
    public async Task<IActionResult> AddressBookReadOneUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("AddressBookReadOne", req, AddressBookReadOne);
    private async Task<IActionResult> AddressBookReadOne(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForAddressBook(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string? entryId = req.Query["id"];
        if (string.IsNullOrEmpty(entryId))
        {
            log.Error("Missing the id query parameter");
            return new BadRequestObjectResult("Missing the id query parameter");
        }

        var entry = await _store.ReadAsync(AddressBookDocument.PartitionKey, entryId);
        if (entry.Code == ResultCode.Success && entry.Entity != null)
        {
            return new OkObjectResult(JsonSerializer.Serialize(ResponseFromDocument(entry.Entity)));
        }
        else
        {
            log.OperationResult("Failed to read the entry", entry);
            return new BadRequestResult();
        }
    }

    [Function("AddressBookGetCanned")]
    public async Task<IActionResult> AddressBookGetCannedUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("AddressBookGetCanned", req, AddressBookGetCanned);
    private async Task<IActionResult> AddressBookGetCanned(HttpRequest req, LogBuffer log)
    {
        await Task.Yield();
        var doc = new AddressBookDocument
        {
            id = "id123",
            _etag = "etag",
            FirstName = "Dummy",
            LastName = "Name",
            Street = "123 Happy Street",
            Apartment = "Apt B",
            City = "Springfield",
            State = "MU",
            ZipCode = "12345",
            PhoneNumber = "123.456.7890",
            MailingName = "Mr. and Mrs. Name",
            OtherPeople = "Other",
            HolidayCard = "Yes"
        };
        return new OkObjectResult(JsonSerializer.Serialize(ResponseFromDocument(doc)));
    }
}
