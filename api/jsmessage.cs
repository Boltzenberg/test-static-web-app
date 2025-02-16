using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions
{
    public class jsmessage
    {
        private readonly ILogger<jsmessage> _logger;

        public jsmessage(ILogger<jsmessage> logger)
        {
            _logger = logger;
        }

        [Function("jsmessage")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
