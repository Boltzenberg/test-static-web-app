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
            AddressBookEntry entry = null;
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Request Body: " + body);
                entry = JsonSerializer.Deserialize<AddressBookEntry>(body);
                entry.id = Guid.NewGuid().ToString();

                entry = await JsonStore.CreateAddressBookEntry(entry);
                return new OkObjectResult(JsonSerializer.Serialize(entry));
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
            AddressBookEntry entry = null;
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Request Body: " + body);
                entry = JsonSerializer.Deserialize<AddressBookEntry>(body);

                entry = await JsonStore.UpdateAddressBookEntry(entry);
                if (entry != null)
                {
                    return new OkObjectResult(JsonSerializer.Serialize(entry));
                }
                else
                {
                    // precondition failed
                    return new StatusCodeResult(412);
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
                AddressBookEntry entry = JsonSerializer.Deserialize<AddressBookEntry>(body);

                bool deleted = await JsonStore.DeleteAddressBookEntry(entry);
                if (deleted)
                {
                    return new NoContentResult();
                }
                else
                {
                    // precondition failed
                    return new StatusCodeResult((int)System.Net.HttpStatusCode.PreconditionFailed);
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

            List<AddressBookEntry> entries = await JsonStore.GetAddressBookEntries();
            return new OkObjectResult(JsonSerializer.Serialize(entries.ToArray()));
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("AddressBookGetCanned")]
    public async Task<IActionResult> AddressBookGetCanned([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            AddressBookEntry entry = new AddressBookEntry();
            entry.id = "id123";
            entry._etag = "etag";
            entry.FirstName = "Jon";
            entry.LastName = "Rosenberg";
            entry.Street = "13339 NE 92nd Way";
            entry.Apartment = "Apt B";
            entry.City = "Redmond";
            entry.State = "WA";
            entry.ZipCode = "98052";
            entry.PhoneNumber = "425.591.9019";
            entry.MailingName = "Jon and Teresa Rosenberg";
            entry.OtherPeople = "Teresa";
            entry.HolidayCard = "Christmas";
            return new OkObjectResult(JsonSerializer.Serialize(entry));
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }
}