using System.Text;
using Boltzenberg.Functions.Comms;
using Boltzenberg.Functions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class SubmitContactForm
{
    public SubmitContactForm()
    {
    }

    private async Task<IActionResult> SendEmail(string toAddress, string toName, HttpRequest req, LogBuffer log)
    {
        string message = "This is a test mail";

        if (req.Method.ToLowerInvariant() == "post")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Name: {0}{1}", req.Form["name"], Environment.NewLine);
            sb.AppendFormat("Email Address: {0}{1}", req.Form["email"], Environment.NewLine);
            sb.AppendFormat("Message: {0}{1}", req.Form["message"], Environment.NewLine);
            message = sb.ToString();
        }

        string responseMessage = string.Empty;
        if (await Email.SendContactFormMailAsync(toName, toAddress, message))
        {
            log.Info("Successfully sent email to {0}", toAddress);
            responseMessage = "<H2>Great! Thanks for filling out my form!</H2>";
        }
        else
        {
            log.Error("Failed to send email to {0}", toAddress);
            responseMessage = "<H2>Oops! There was a problem submitting the form.</H2>";
        }

        return new ContentResult {
            Content = responseMessage,
            ContentType = "text/html",
            StatusCode = 200
        };
    }

    [Function("ContactDanRosenberg")]
    public async Task<IActionResult> ContactDanRosenbergUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("ContactDanRosenberg", req, ContactDanRosenberg);
    public async Task<IActionResult> ContactDanRosenberg(HttpRequest req, LogBuffer log)
    {
        return await SendEmail("danrosenberg@gmail.com", "Dan Rosenberg", req, log);
    }

    [Function("ContactJonRosenberg")]
    public async Task<IActionResult> ContactJonRosenbergUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req) =>
        await LogBuffer.Wrap("ContactJonRosenberg", req, ContactJonRosenberg);
    public async Task<IActionResult> ContactJonRosenberg(HttpRequest req, LogBuffer log)
    {
        return await SendEmail("jon.p.rosenberg@gmail.com", "Jon Rosenberg", req, log);
    }
}