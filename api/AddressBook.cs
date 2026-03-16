using System.Net;
using System.Text.Json;
using Boltzenberg.Functions.DataModels.AddressBook;
using Boltzenberg.Functions.DataModels.Auth;
using Boltzenberg.Functions.Logging;
using Boltzenberg.Functions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class AddressBook
{
    public AddressBook(ILogger<AddressBook> logger)
    {
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

        AddressBookEntry? entry = null;
        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!string.IsNullOrEmpty(body))
        {
            entry = JsonSerializer.Deserialize<AddressBookEntry>(body);
            if (entry == null)
            {
                log.Error("Failed to deserialize request body '{0}'", body);
                return new BadRequestObjectResult("Failed to deserialize the entry to add");
            }

            entry.id = Guid.NewGuid().ToString();
            var result = await JsonStore.Create(entry);
            if (result != null)
            {
                if (result.Entity != null && result.Code == ResultCode.Success)
                {
                    log.Info("Created address book entry for '{0}'", entry.ToString());
                    return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
                }
                else
                {
                    log.Error("Failed to create the address book entry: {0}", result.Code);
                }
            }
            else
            {
                log.Error("Failed to create the address book entry with a null result");
            }
        }
        else
        {
            log.Error("No request body");         
        }

        return new BadRequestResult();
    }

    [Function("AddressBookUpdateEntry")]
    public async Task<IActionResult> AddressBookUpdateEntryUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("AddressBookUpdateEntry", req, AddressBookUpdateEntry);
    public async Task<IActionResult> AddressBookUpdateEntry(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForAddressBook(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!string.IsNullOrEmpty(body))
        {
            AddressBookEntry? entry = JsonSerializer.Deserialize<AddressBookEntry>(body);
            if (entry == null)
            {
                log.Error("Failed to deserialize request body {0}", body);
                return new BadRequestResult();
            }

            var result = await JsonStore.Update(entry);
            if (result.Code == ResultCode.Success && result.Entity != null)
            {
                return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
            }
            else if (result.Code == ResultCode.PreconditionFailed)
            {
                log.Error("OCC failure");
                return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
            }
            else
            {
                log.OperationResult("Failed to update the entry", result);
            }
        }
        else
        {
            log.Error("No request body!");
        }

        return new BadRequestResult();
    }

    [Function("AddressBookDeleteEntry")]
    public async Task<IActionResult> AddressBookDeleteEntryUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("AddressBookDeleteEntry", req, AddressBookDeleteEntry);
    public async Task<IActionResult> AddressBookDeleteEntry(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForAddressBook(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!string.IsNullOrEmpty(body))
        {
            AddressBookEntry? entry = JsonSerializer.Deserialize<AddressBookEntry>(body);
            if (entry == null)
            {
                log.Error("Failed to deserialized request body '{0}'", body);
                return new BadRequestObjectResult("Failed to deserialize the entry to delete");
            }

            var result = await JsonStore.Delete(entry);
            if (result.Code == ResultCode.Success)
            {
                return new NoContentResult();
            }
            else if (result.Code == ResultCode.PreconditionFailed)
            {
                log.Error("OCC failure");
                return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
            }
            else
            {
                log.OperationResult("Failed to delete the entry", result);
            }
        }
        else
        {
            log.Error("No request body!");
        }

        return new BadRequestResult();
    }

    [Function("AddressBookReadAll")]
    public async Task<IActionResult> AddressBookReadAllUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("AddressBookReadAll", req, AddressBookReadAll);
    public async Task<IActionResult> AddressBookReadAll(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForAddressBook(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        var entries = await JsonStore.ReadAll<AddressBookEntry>(AddressBookEntry.AddressBookEntryAppId);
        if (entries.Code == ResultCode.Success)
        {
            return new OkObjectResult(JsonSerializer.Serialize(entries.Entity));
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
    public async Task<IActionResult> AddressBookReadOne(HttpRequest req, LogBuffer log)
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

        var entry = await JsonStore.Read<AddressBookEntry>(AddressBookEntry.AddressBookEntryAppId, entryId);
        if (entry.Code == ResultCode.Success)
        {
            return new OkObjectResult(JsonSerializer.Serialize(entry.Entity));
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
    public async Task<IActionResult> AddressBookGetCanned(HttpRequest req, LogBuffer log)
    {
        await Task.Yield();
        AddressBookEntry entry = new AddressBookEntry();
        entry.id = "id123";
        entry._etag = "etag";
        entry.FirstName = "Dummy";
        entry.LastName = "Name";
        entry.Street = "123 Happy Street";
        entry.Apartment = "Apt B";
        entry.City = "Springfield";
        entry.State = "MU";
        entry.ZipCode = "12345";
        entry.PhoneNumber = "123.456.7890";
        entry.MailingName = "Mr. and Mrs. Name";
        entry.OtherPeople = "Other";
        entry.HolidayCard = "Yes";
        return new OkObjectResult(JsonSerializer.Serialize(entry));
    }
}