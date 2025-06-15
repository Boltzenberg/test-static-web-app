using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class SubmitContactForm
{
    private readonly ILogger<SubmitContactForm> _logger;
    private const string FromAddress = "santamail@boltzenberg.com";
    private const string FromName = "Santa Boltzenberg";
    private const string ToAddress = "jon.p.rosenberg@gmail.com";
    private const string ToName = "Jon Rosenberg";
    private const string Subject = "Message from your website";
    private static string MailJetAPIKey = Environment.GetEnvironmentVariable("MAILJET_API_KEY") ?? string.Empty;
    private static string MailJetSecretKey = Environment.GetEnvironmentVariable("MAILJET_SECRET_KEY") ?? string.Empty;

    public SubmitContactForm(ILogger<SubmitContactForm> logger)
    {
        _logger = logger;
    }

    private static bool MailJetSantaMail(string body)
    {
        HttpWebRequest req = HttpWebRequest.CreateHttp("https://api.mailjet.com/v3.1/send");
        req.Method = "POST";
        req.ContentType = "application/json";
        req.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(MailJetAPIKey + ":" + MailJetSecretKey)));

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine(" \"Messages\":[");
        sb.AppendLine("  {");
        sb.AppendLine("    \"From\": { \"Email\": \"" + FromAddress + "\", \"Name\": \"" + FromName + "\" },");
        sb.AppendLine("    \"To\": [ { \"Email\": \"" + ToAddress + "\", \"Name\": \"" + ToName + "\" } ],");
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

        using (HttpWebResponse res = req.GetResponse() as HttpWebResponse)
        {
            return res.StatusCode == HttpStatusCode.OK;
        }
    }

    [Function("SubmitContactForm")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        string message = "This is a test mail";

        if (req.Method.ToLowerInvariant() == "post")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("First Name: {0}{1}", req.Form["firstName"], Environment.NewLine);
            sb.AppendFormat("Last Name: {0}{1}", req.Form["lastName"], Environment.NewLine);
            sb.AppendFormat("Email Address: {0}{1}", req.Form["email"], Environment.NewLine);
            sb.AppendFormat("Message: {0}{1}", req.Form["message"], Environment.NewLine);
            message = sb.ToString();
        }

        if (MailJetSantaMail(message))
        {
            return new OkObjectResult(message);
        }
        else
        {
            return new OkObjectResult("Failed to send the mail :(");
        }
    }
}