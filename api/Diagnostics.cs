using System.Net;
using System.Text;
using System.Threading.Tasks;
using Boltzenberg.Functions.Comms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class Diagnostics
{
    private readonly ILogger<SubmitContactForm> _logger;

    public Diagnostics(ILogger<SubmitContactForm> logger)
    {
        _logger = logger;
    }

    [Function("SendTestMail")]
    public async Task<IActionResult> SendTestMail([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        string? summary = req.Query["summary"];
        if (summary == null || summary != "send")
        {
            return new OkResult();
        }

        bool result = await Email.SendWeeklyMailAsync();
        if (result)
        {
            return new OkObjectResult("Email successfully sent!");
        }
        
        return new BadRequestObjectResult("Tried and failed to send the email!");
    }
}