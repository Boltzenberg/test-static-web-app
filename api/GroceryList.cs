using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Boltzenberg.Functions.DataModels.GroceryList;
using Boltzenberg.Functions.Storage;
using System.Text;
using Boltzenberg.Functions.Comms;
using Azure;

namespace Boltzenberg.Functions
{
    public class GroceryList
    {
        public GroceryList()
        {
        }

        [Function("CreateGroceryList")]
        public async Task<IActionResult> CreateGroceryList([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            string? listId = req.Headers["X-List-ID"];
            if (string.IsNullOrEmpty(listId))
            {
                return new BadRequestObjectResult("X-List-ID header is required");
            }

            GroceryListDB list = new GroceryListDB(listId);
            var result = await JsonStore.Create(list);
            if (result.Code != ResultCode.Success)
            {
                return new BadRequestObjectResult(result.Error);
            }

            return new CreatedResult();
        }

        [Function("UpdateGroceryList")]
        public async Task<IActionResult> UpdateGroceryList([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            string? listId = req.Headers["X-List-ID"];
            if (string.IsNullOrEmpty(listId))
            {
                return new BadRequestObjectResult("X-List-ID header is required");
            }

            OperationResult<GroceryListDB>? result = null;

            if (string.Equals(req.Method, "post", StringComparison.OrdinalIgnoreCase))
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                if (!string.IsNullOrEmpty(body))
                {
                    UpdateGroceryListPayload? reqBody = JsonSerializer.Deserialize<UpdateGroceryListPayload>(body);
                    if (reqBody != null)
                    {
                        result = await UpdateGroceryList(listId, reqBody);
                    }
                }
            }
            else
            {
                result = await JsonStore.Read<GroceryListDB>(GroceryListDB.GroceryListAppId, listId);
            }

            if (result == null || result.Entity == null)
            {
                await Telegram.LogErrorAsync("Grocery List returning bad request result");
                return new BadRequestResult();
            }

            string response = JsonSerializer.Serialize(result.Entity.Items);
            await Telegram.LogInfoAsync("Grocery List returning '" + response + "'");
            return new OkObjectResult(response);
        }

        public static async Task<OperationResult<GroceryListDB>?> UpdateGroceryList(string listId, UpdateGroceryListPayload payload)
        {
            StringBuilder sbLog = new StringBuilder();
            OperationResult<GroceryListDB>? result = null;
            do
            {
                sbLog.Clear();
                result = await JsonStore.Read<GroceryListDB>(GroceryListDB.GroceryListAppId, listId);
                if (result == null || result.Entity == null || result.Code == ResultCode.GenericError)
                {
                    await Telegram.LogErrorAsync("Failed to find the grocery list with id '" + listId + "'");
                    return null;
                }

                foreach (GroceryListItem item in payload.ToRemove)
                {
                    GroceryListItem? itemToRemove = result.Entity.Items.Where(i => i.Item == item.Item).FirstOrDefault();
                    if (itemToRemove != null)
                    {
                        sbLog.AppendLine("🔴 Grocery List Removing '" + itemToRemove.Item + "'");
                        result.Entity.Items.Remove(itemToRemove);
                    }
                }

                foreach (GroceryListItem item in payload.ToAdd)
                {
                    sbLog.AppendLine("🟢 Grocery List Adding '" + item.Item + "'");
                    result.Entity.Items.Add(item);
                }

                result = await JsonStore.Update<GroceryListDB>(result.Entity);
            } while (result.Code == ResultCode.PreconditionFailed);
            await Telegram.LogInfoAsync(sbLog.ToString());

            return result;
        }
    }
}
