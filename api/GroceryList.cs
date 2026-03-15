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
using Boltzenberg.Functions.Logging;

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
            return await LogBuffer.Wrap("CreateGroceryList", req, CreateGroceryListWrapped);
        }

        private async Task<IActionResult> CreateGroceryListWrapped(HttpRequest req, LogBuffer log)
        {
            string? listId = req.Headers["X-List-ID"];
            if (string.IsNullOrEmpty(listId))
            {
                log.Error("Invalid or missing list id '{0}'", listId ?? "<null>");
                return new BadRequestObjectResult("X-List-ID header is required");
            }

            GroceryListDB list = new GroceryListDB(listId);
            var result = await JsonStore.Create(list);
            if (result.Code != ResultCode.Success)
            {
                log.Error("Failed to create the grocery list: {0}", (result.Error != null) ? result.Error.ToString() : "<no exception returned>");
                return new BadRequestObjectResult(result.Error);
            }

            log.Info("Created grocery list '{0}'", listId);
            return new CreatedResult();
        }

        [Function("UpdateGroceryList")]
        public async Task<IActionResult> UpdateGroceryList([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            return await LogBuffer.Wrap("UpdateGroceryList", req, UpdateGroceryListWrapped);
        }

        private async Task<IActionResult> UpdateGroceryListWrapped(HttpRequest req, LogBuffer log)
        {
            string? listId = req.Headers["X-List-ID"];
            if (string.IsNullOrEmpty(listId))
            {
                log.Error("Invalid list id '{0}'", listId ?? "<null>");
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
                        result = await DoUpdateGroceryList(listId, reqBody, log);
                    }
                    else
                    {
                        log.Error("Failed to deserialize request body '{0}'", body);
                    }
                }
                else
                {
                    log.Error("No request body!");
                }
            }
            else
            {
                result = await JsonStore.Read<GroceryListDB>(GroceryListDB.GroceryListAppId, listId);
            }

            if (result == null || result.Entity == null)
            {
                log.Error("Failed to get the grocery list to return.  Returning bad request.");
                return new BadRequestResult();
            }

            string response = JsonSerializer.Serialize(result.Entity.Items);
            return new OkObjectResult(response);
        }

        public static async Task<OperationResult<GroceryListDB>?> DoUpdateGroceryList(string listId, UpdateGroceryListPayload payload, LogBuffer log)
        {
            StringBuilder sbLog = new StringBuilder();
            OperationResult<GroceryListDB>? result = null;
            do
            {
                sbLog.Clear();
                result = await JsonStore.Read<GroceryListDB>(GroceryListDB.GroceryListAppId, listId);
                if (result == null || result.Entity == null || result.Code == ResultCode.GenericError)
                {
                    log.Error("Failed to find the grocery list with id '{0}'", listId);
                    throw new InvalidOperationException("Failed to find grocery list");
                }

                foreach (GroceryListItem item in payload.ToRemove)
                {
                    GroceryListItem? itemToRemove = result.Entity.Items.Where(i => i.Item == item.Item).FirstOrDefault();
                    if (itemToRemove != null)
                    {
                        sbLog.AppendLine("🔴 Removing '" + itemToRemove.Item + "'");
                        result.Entity.Items.Remove(itemToRemove);
                    }
                }

                foreach (GroceryListItem item in payload.ToAdd)
                {
                    sbLog.AppendLine("🟢 Adding '" + item.Item + "'");
                    result.Entity.Items.Add(item);
                }

                result = await JsonStore.Update<GroceryListDB>(result.Entity);
            } while (result.Code == ResultCode.PreconditionFailed);

            log.Info(sbLog.ToString());

            return result;
        }
    }
}
