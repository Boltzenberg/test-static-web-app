using System.Text.Json;
using Boltzenberg.Functions.Commands.Telegram;
using Boltzenberg.Functions.DataModels.Telegram;
using Boltzenberg.Functions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

public class TelegramWebhook
{
    private readonly CommandDispatcher _dispatcher;

    public TelegramWebhook(CommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [Function("TelegramWebhook")]
    public async Task<IActionResult> InvokeWebhookUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        => await LogBuffer.Wrap("TelegramWebhook", req, InvokeWebhookImpl);

    private async Task<IActionResult> InvokeWebhookImpl(HttpRequest req, LogBuffer log)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var update = JsonSerializer.Deserialize<TelegramUpdate>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (update?.Message == null)
        {
            return new OkResult();
        }

        await _dispatcher.DispatchAsync(update, log);
        return new OkResult();
    }
}
