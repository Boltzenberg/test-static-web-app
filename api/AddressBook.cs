using System.Text.Json;
using Boltzenberg.Functions.DataModels.Auth;
using Microsoft.AspNetCore.Http;
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

    [Function("ReadAddressBook")]
    public IActionResult ReadAddressBook([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        try
        {
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForAddressBook())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }

            return new OkObjectResult(JsonSerializer.Serialize(principal));
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }
}