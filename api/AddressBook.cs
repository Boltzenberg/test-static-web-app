using System.Text;
using System.Text.Json;
using Boltzenberg.Functions.DataModels.Auth;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Boltzenberg.Functions;

public class ReadAddressBook
{
    private readonly ILogger<ReadAddressBook> _logger;

    public ReadAddressBook(ILogger<ReadAddressBook> logger)
    {
        _logger = logger;
    }

    [Function("ReadAddressBook")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        try
        {
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null)
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