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
        // 1. Read the header 
        if (!req.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL", out var headerValues))
        {
            return new UnauthorizedObjectResult("No auth header found");
        }

        var encoded = headerValues.First();
        if (encoded == null)
        {
            return new UnauthorizedObjectResult("No auth header value");
        }

        // 2. Decode Base64 → JSON 
        var decodedBytes = Convert.FromBase64String(encoded);
        var json = Encoding.UTF8.GetString(decodedBytes);

        // 3. Deserialize
        //var principal = JsonSerializer.Deserialize<ClientPrincipal>(json);

        return new OkObjectResult(json);
    }
}