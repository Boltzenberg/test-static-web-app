using System.Net;
using System.Text;
using Boltzenberg.Functions.Comms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class SubmitContactForm
{
    private readonly ILogger<SubmitContactForm> _logger;
    private const string FromAddress = "santamail@boltzenberg.com";
    private const string FromName = "Website Contact Form";
    private const string Subject = "Message from your website";
    private static string MailJetAPIKey = Environment.GetEnvironmentVariable("MAILJET_API_KEY") ?? string.Empty;
    private static string MailJetSecretKey = Environment.GetEnvironmentVariable("MAILJET_SECRET_KEY") ?? string.Empty;
    private static readonly HttpClient httpClient = new HttpClient();

    public SubmitContactForm(ILogger<SubmitContactForm> logger)
    {
        _logger = logger;
    }

    private static bool MailJetSantaMail(string toAddress, string toName, string body)
    {
        HttpWebRequest? req = HttpWebRequest.CreateHttp("https://api.mailjet.com/v3.1/send");
        req.Method = "POST";
        req.ContentType = "application/json";
        req.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(MailJetAPIKey + ":" + MailJetSecretKey)));

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine(" \"Messages\":[");
        sb.AppendLine("  {");
        sb.AppendLine("    \"From\": { \"Email\": \"" + FromAddress + "\", \"Name\": \"" + FromName + "\" },");
        sb.AppendLine("    \"To\": [ { \"Email\": \"" + toAddress + "\", \"Name\": \"" + toName + "\" } ],");
        sb.AppendLine("    \"Subject\": \"" + Subject + "\",");
        sb.AppendLine("    \"HTMLPart\": \"" + body.Replace(Environment.NewLine, "<BR>").Replace("\"", "\\\"") + "\"");
        sb.AppendLine("  }");
        sb.AppendLine(" ]");
        sb.AppendLine("}");

        using (StreamWriter writer = new StreamWriter(req.GetRequestStream()))
        {
            writer.Write(sb.ToString());
            writer.Flush();
        }

        using (HttpWebResponse? res = req.GetResponse() as HttpWebResponse)
        {
            if (res != null)
            {
                return res.StatusCode == HttpStatusCode.OK;
            }
            else
            {
                return false;
            }
        }
    }

    private IActionResult SendEmail(string toAddress, string toName, HttpRequest req)
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
        if (MailJetSantaMail(toAddress, toName, message))
        {
            responseMessage = "<H2>Great! Thanks for filling out my form!</H2>";
        }
        else
        {
            responseMessage = "<H2>Oops! There was a problem submitting the form.</H2>";
        }

        return new ContentResult {
            Content = responseMessage,
            ContentType = "text/html",
            StatusCode = 200
        };
    }

    [Function("ContactDanRosenberg")]
    public IActionResult ContactDanRosenberg([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        return SendEmail("danrosenberg@gmail.com", "Dan Rosenberg", req);
    }

    [Function("ContactJonRosenberg")]
    public async Task<IActionResult> ContactJonRosenberg([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        string toAddress = "jon.p.rosenberg@gmail.com";
        string toName = "Jon Rosenberg";
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
            responseMessage = "<H2>Great! Thanks for filling out my form!</H2>";
        }
        else
        {
            responseMessage = "<H2>Oops! There was a problem submitting the form.</H2>";
        }

        return new ContentResult {
            Content = responseMessage,
            ContentType = "text/html",
            StatusCode = 200
        };
    }
}