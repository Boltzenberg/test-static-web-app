using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class SubmitContactForm
{
    private readonly ILogger<SubmitContactForm> _logger;

    public SubmitContactForm(ILogger<SubmitContactForm> logger)
    {
        _logger = logger;
    }

    [Function("SubmitContactForm")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}