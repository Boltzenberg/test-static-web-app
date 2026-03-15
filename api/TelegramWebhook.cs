using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Boltzenberg.Functions;
using Boltzenberg.Functions.Comms;
using Boltzenberg.Functions.DataModels.GroceryList;
using Boltzenberg.Functions.DataModels.Telegram;
using Boltzenberg.Functions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class TelegramWebhook
{
    private static readonly long JonUserId = 5241310949;
    private static readonly long TeresaUserId = 5411752675;

    [Function("TelegramWebhook")]
    public static async Task<HttpResponseData> InvokeWebhook([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        var response = req.CreateResponse();
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();

            var update = JsonSerializer.Deserialize<TelegramUpdate>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            var chatId = update?.Message?.Chat?.Id.ToString();
            var fromId = update?.Message?.From?.Id;
            var text = update?.Message?.Text;

            if (chatId == null || text == null)
                return response;

            await Telegram.LogInfoAsync("Received command '" + text + "' from '" + fromId + "'");

            // Command router
            if (text == "/ping")
            {
                await Telegram.SendAsync(chatId, "🟢 Pong");
            }
            else if (text.StartsWith("/add "))
            {
                // Add to the grocery list
                if (fromId != JonUserId && fromId != TeresaUserId)
                {
                    await Telegram.SendAsync(chatId, "❌ Unauthorized");
                    return response;
                }

                LogBuffer log = new LogBuffer("Webhook - /add");
                string item = text.Substring(text.IndexOf(' ')).Trim();
                UpdateGroceryListPayload payload = new UpdateGroceryListPayload();
                payload.ToAdd.Add(new GroceryListItem(item));
                var result = await GroceryList.DoUpdateGroceryList("Test", payload, log);
                if (result == null || result.Entity == null || result.Code != Boltzenberg.Functions.Storage.ResultCode.Success)
                {
                    await Telegram.SendAsync(chatId, "❌ failed to update the grocery list!  Check the logs.");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("🟢");
                    foreach (var glItem in result.Entity.Items)
                    {
                        sb.AppendLine(glItem.Item);
                    }
                    await Telegram.SendAsync(chatId, sb.ToString());
                }
                await log.Close();
            }
            else if (text.StartsWith("/remove "))
            {
                // Add to the grocery list
                if (fromId != JonUserId && fromId != TeresaUserId)
                {
                    await Telegram.SendAsync(chatId, "❌ Unauthorized");
                    return response;
                }

                LogBuffer log = new LogBuffer("Webhook - /remove");
                string item = text.Substring(text.IndexOf(' ')).Trim();
                UpdateGroceryListPayload payload = new UpdateGroceryListPayload();
                payload.ToRemove.Add(new GroceryListItem(item));
                var result = await GroceryList.DoUpdateGroceryList("Test", payload, log);
                if (result == null || result.Entity == null || result.Code != Boltzenberg.Functions.Storage.ResultCode.Success)
                {
                    await Telegram.SendAsync(chatId, "❌ failed to update the grocery list!  Check the logs.");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("🟢");
                    foreach (var glItem in result.Entity.Items)
                    {
                        sb.AppendLine(glItem.Item);
                    }
                    await Telegram.SendAsync(chatId, sb.ToString());
                }
                await log.Close();
            }
            else
            {
                await Telegram.SendAsync(chatId, "🤖 Unknown command");
            }

            return response;
        }
        catch (Exception ex)
        {
            await Telegram.LogErrorAsync("Telegram Webhook: " + ex.ToString());
        }

        return response;
    }
}
