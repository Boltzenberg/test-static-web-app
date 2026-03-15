using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Boltzenberg.Functions.Comms;
using Boltzenberg.Functions.DataModels.Telegram;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class TelegramWebhook
{
    private static readonly long AllowedUserId =
        long.Parse(Environment.GetEnvironmentVariable("TELEGRAM_ADMIN_USER_ID") ?? "0");

    [Function("TelegramWebhook")]
    public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        await Telegram.LogInfoAsync("TelegramWebhook invoked");

        var body = await new StreamReader(req.Body).ReadToEndAsync();

        await Telegram.LogInfoAsync("Received body '" + body + "'");
        
        var update = JsonSerializer.Deserialize<TelegramUpdate>(body);

        var chatId = update?.Message?.Chat?.Id.ToString();
        var fromId = update?.Message?.From?.Id;
        var text = update?.Message?.Text;

        var response = req.CreateResponse();

        if (chatId == null || text == null)
            return response;

        await Telegram.LogInfoAsync("Received command '" + text + "' from '" + fromId + "'");

        // Security: Only YOU can run commands
        if (fromId != AllowedUserId)
        {
            await Telegram.SendAsync(chatId, "❌ Unauthorized");
            return response;
        }

        // Command router
        if (text == "/ping")
        {
            await Telegram.SendAsync(chatId, "🟢 Pong");
        }
        else
        {
            await Telegram.SendAsync(chatId, "🤖 Unknown command");
        }

        return response;
    }
}
