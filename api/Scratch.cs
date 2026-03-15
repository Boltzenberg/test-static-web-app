using Boltzenberg.Functions.Comms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class Scratch
{
    private static readonly HttpClient _http = new HttpClient();

    private readonly ILogger<Scratch> _logger;

    public Scratch(ILogger<Scratch> logger)
    {
        _logger = logger;
    }

    [Function("GavinTest")]
    public static async Task<IActionResult> GavinTest([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        var targetUrl =
            "http://api.team-manager.gc.com/ics-calendar-documents/user/ea72f54e-b371-4e08-80eb-1aa8eeb837c0.ics?teamId=740ab415-f6c9-4d37-a5e1-18073565e746&token=ba5acd8667c647377252ad9953705dc80e4a3d18d1ccc7025f0f21ab4fdd57c3";

        var upstream = await _http.GetAsync(targetUrl);
        var content = await upstream.Content.ReadAsStringAsync();
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

        return new ContentResult
        {
            Content = content,
            ContentType = contentType,
            StatusCode = (int)upstream.StatusCode
        };
    }

    [Function("SendTelegram")]
    public static async Task<IActionResult> SendTelegram([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        await TelegramLogger.InfoAsync("Send Telegram API invoked!");
        return new OkObjectResult("Telegram Message Sent!");
    }
}
