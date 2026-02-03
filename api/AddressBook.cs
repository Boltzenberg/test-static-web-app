using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Boltzenberg.Functions.DataModels.AddressBook;
using Boltzenberg.Functions.DataModels.Auth;
using Boltzenberg.Functions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class AddressBook
{
    private readonly ILogger<AddressBook> _logger;

    public AddressBook(ILogger<AddressBook> logger)
    {
        _logger = logger;
    }

    [Function("AddressBookAddEntry")]
    public async Task<IActionResult> AddressBookAddEntry([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            /*
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForAddressBook())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }
            */
            AddressBookEntry? entry = null;
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Request Body: " + body);
                entry = JsonSerializer.Deserialize<AddressBookEntry>(body);
                if (entry == null)
                {
                    return new BadRequestObjectResult("Failed to deserialize the entry to add");
                }

                entry.id = Guid.NewGuid().ToString();
                var result = await JsonStore.Create(entry);
                return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
            }

            return new BadRequestResult();
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("AddressBookUpdateEntry")]
    public async Task<IActionResult> AddressBookUpdateEntry([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            /*
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForAddressBook())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }
            */

            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Request Body: " + body);
                AddressBookEntry? entry = JsonSerializer.Deserialize<AddressBookEntry>(body);
                if (entry == null)
                {
                    return new BadRequestResult();
                }

                var result = await JsonStore.Update(entry);
                if (result.Code == ResultCode.Success && result.Entity != null)
                {
                    return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
                }
                else if (result.Code == ResultCode.PreconditionFailed)
                {
                    return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
                }
            }

            return new BadRequestResult();
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("AddressBookDeleteEntry")]
    public async Task<IActionResult> AddressBookDeleteEntry([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            /*
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForAddressBook())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }
            */
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Request Body: " + body);
                AddressBookEntry? entry = JsonSerializer.Deserialize<AddressBookEntry>(body);
                if (entry == null)
                {
                    return new BadRequestObjectResult("Failed to deserialize the entry to delete");
                }

                var result = await JsonStore.Delete(entry);
                if (result.Code == ResultCode.Success)
                {
                    return new NoContentResult();
                }
                else if (result.Code == ResultCode.PreconditionFailed)
                {
                    return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
                }
            }

            return new BadRequestResult();
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("AddressBookReadAll")]
    public async Task<IActionResult> AddressBookReadAll([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            /*
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForAddressBook())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }
            */

            var entries = await JsonStore.ReadAll<AddressBookEntry>(AddressBookEntry.AddressBookEntryAppId);
            if (entries.Code == ResultCode.Success)
            {
                return new OkObjectResult(JsonSerializer.Serialize(entries.Entity));
            }
            else
            {
                return new BadRequestResult();
            }
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("AddressBookReadOne")]
    public async Task<IActionResult> AddressBookReadOne([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            /*
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForAddressBook())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }
            */

            string? entryId = req.Query["id"];
            if (string.IsNullOrEmpty(entryId))
            {
                return new BadRequestObjectResult("Missing the id query parameter");
            }

            var entry = await JsonStore.Read<AddressBookEntry>(AddressBookEntry.AddressBookEntryAppId, entryId);
            if (entry.Code == ResultCode.Success)
            {
                return new OkObjectResult(JsonSerializer.Serialize(entry.Entity));
            }
            else
            {
                return new BadRequestResult();
            }
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("AddressBookGetCanned")]
    public IActionResult AddressBookGetCanned([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
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
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }
}