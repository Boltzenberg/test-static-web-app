using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Boltzenberg.Functions.DataModels.GroceryList;
using Boltzenberg.Functions.Storage;

namespace Boltzenberg.Functions
{
    public class GroceryList
    {
        private readonly ILogger<GroceryList> _logger;

        public GroceryList(ILogger<GroceryList> logger)
        {
            _logger = logger;
        }

        [Function("CreateGroceryList")]
        public async Task<IActionResult> CreateGroceryList([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            string listId = req.Headers["X-List-ID"];
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
            string listId = req.Headers["X-List-ID"];
            if (string.IsNullOrEmpty(listId))
            {
                return new BadRequestObjectResult("X-List-ID header is required");
            }

            OperationResult<GroceryListDB> result = null;

            if (req.Method.ToLowerInvariant() == "post")
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogInformation("Request Body: " + body);
                    UpdateGroceryListPayload reqBody = JsonSerializer.Deserialize<UpdateGroceryListPayload>(body);
                    if (reqBody != null)
                    {
                        do
                        {
                            result = await JsonStore.Read<GroceryListDB>(GroceryListDB.GroceryListAppId, listId);
                            if (result.Code == ResultCode.GenericError)
                            {
                                return new BadRequestObjectResult("Failed to find the grocery list");
                            }

                            foreach (GroceryListItem item in reqBody.ToRemove)
                            {
                                GroceryListItem itemToRemove = result.Entity.Items.Where(i => i.Item == item.Item).FirstOrDefault();
                                if (itemToRemove != null)
                                {
                                    result.Entity.Items.Remove(itemToRemove);
                                }
                            }

                            foreach (GroceryListItem item in reqBody.ToAdd)
                            {
                                result.Entity.Items.Add(item);
                            }

                            result = await JsonStore.Update<GroceryListDB>(result.Entity);
                        } while (result.Code == ResultCode.PreconditionFailed);
                    }
                }
            }
            else
            {
                result = await JsonStore.Read<GroceryListDB>(GroceryListDB.GroceryListAppId, listId);
            }

            string response = JsonSerializer.Serialize(result.Entity.Items);
            return new OkObjectResult(response);
        }
    }
}
