using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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
        var header = req.Headers["X-MS-TOKEN-AAD-ACCESS-TOKEN"];
        if (!String.IsNullOrEmpty(header))
        {
            var accessToken = header.ToString(); // You now have the raw JWT 
        }

        string response = JsonSerializer.Serialize(req.Headers);
        return new OkObjectResult(response);
    }
}