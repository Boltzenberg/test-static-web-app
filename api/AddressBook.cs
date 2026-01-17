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
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}