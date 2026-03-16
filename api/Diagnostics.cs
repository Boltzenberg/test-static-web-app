using System.Drawing.Text;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Boltzenberg.Functions.Comms;
using Boltzenberg.Functions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class Diagnostics
{
    [Function("SendTestMail")]
    public async Task<IActionResult> SendTestMailUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SendTestMail", req, SendTestMail);
    private async Task<IActionResult> SendTestMail(HttpRequest req, LogBuffer log)
    {
        string? summary = req.Query["summary"];
        if (summary == null || summary != "send")
        {
            log.Error("Someone called the SendTestMail endpoint without the right parameters");
            return new OkResult();
        }

        bool result = await Email.SendWeeklyMailAsync();
        if (result)
        {
            log.Info("Success!");
            return new OkObjectResult("Email successfully sent!");
        }
        
        log.Error("Failure :(");
        return new BadRequestObjectResult("Tried and failed to send the email!");
    }
}