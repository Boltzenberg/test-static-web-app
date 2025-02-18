using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Boltzenberg.Functions.DataModels.GroceryList;

namespace Boltzenberg.Functions
{
    public class UpdateGroceryList
    {
        private readonly ILogger<UpdateGroceryList> _logger;

        public UpdateGroceryList(ILogger<UpdateGroceryList> logger)
        {
            _logger = logger;
        }

        [Function("UpdateGroceryList")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            List<GroceryListItem> dataset = GroceryListItem.GetCannedItems();

            if (req.Method.ToLowerInvariant() == "post")
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                if (!string.IsNullOrEmpty(body))
                {
                    UpdateGroceryListPayload reqBody = JsonSerializer.Deserialize<UpdateGroceryListPayload>(body);
                    if (reqBody != null)
                    {
                        foreach (GroceryListItem item in reqBody.ToRemove)
                        {
                            GroceryListItem itemToRemove = dataset.Where(i => i.Item == item.Item).FirstOrDefault();
                            if (itemToRemove != null)
                            {
                                dataset.Remove(itemToRemove);
                            }
                        }

                        foreach (GroceryListItem item in reqBody.ToAdd)
                        {
                            dataset.Add(item);
                        }
                    }
                }
            }

            UpdateGroceryListPayload response = new UpdateGroceryListPayload();
            response.ToAdd = dataset;
            response.ToRemove = new List<GroceryListItem>();

            return new OkObjectResult(JsonSerializer.Serialize(response));
        }
    }
}
