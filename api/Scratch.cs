using Boltzenberg.Functions.Comms;
using Boltzenberg.Functions.Logging;
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
    public async Task<IActionResult> GavinTestUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        => await LogBuffer.Wrap("GavinTest", req, GavinTest);
    private async Task<IActionResult> GavinTest(HttpRequest req, LogBuffer log)
    {
        var targetUrl =
            "http://api.team-manager.gc.com/ics-calendar-documents/user/ea72f54e-b371-4e08-80eb-1aa8eeb837c0.ics?teamId=740ab415-f6c9-4d37-a5e1-18073565e746&token=ba5acd8667c647377252ad9953705dc80e4a3d18d1ccc7025f0f21ab4fdd57c3";

        var upstream = await _http.GetAsync(targetUrl);
        var content = await upstream.Content.ReadAsStringAsync();
        var contentType = upstream.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

        log.Info("GavinTest fetched upstream content, status={0}", (int)upstream.StatusCode);

        return new ContentResult
        {
            Content = content,
            ContentType = contentType,
            StatusCode = (int)upstream.StatusCode
        };
    }

    [Function("SendTelegram")]
    public async Task<IActionResult> SendTelegramUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        => await LogBuffer.Wrap("SendTelegram", req, SendTelegram);
    private async Task<IActionResult> SendTelegram(HttpRequest req, LogBuffer log)
    {
        await Telegram.LogInfoAsync("Send Telegram API invoked!");
        log.Info("SendTelegram invoked");
        return new OkObjectResult("Telegram Message Sent!");
    }

    [Function("ThrowExceptionLoggingTest")]
    public async Task<IActionResult> ThrowExceptionLoggingTestUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
        => await LogBuffer.Wrap("ThrowExceptionLoggingTest", req, ThrowExceptionLoggingTest);
    private async Task<IActionResult> ThrowExceptionLoggingTest(HttpRequest req, LogBuffer log)
    {
        log.Info("Adding a log line before throwing the exception");
        throw new InvalidOperationException("This is the message to the InvalidOperationException");
    }
}
