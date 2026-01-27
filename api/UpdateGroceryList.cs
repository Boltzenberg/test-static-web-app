using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Boltzenberg.Functions.DataModels.GroceryList;
using Boltzenberg.Functions.Storage;

namespace Boltzenberg.Functions
{
    public class UpdateGroceryList
    {
        private readonly ILogger<UpdateGroceryList> _logger;
        private const string EndpointUri = "https://gunga-test-cosmosdb.documents.azure.com:443/";
        private static string PrimaryKey = Environment.GetEnvironmentVariable("GROCERY_LIST_PRIMARY_KEY");
        private static CosmosClient cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
        private static Database database = cosmosClient.GetDatabase("GungaDB");
        private static Container container = database.GetContainer("DocumentsContainer");
        private static string AppId = "GroceryList";
        private static QueryRequestOptions queryRequestOptions = new QueryRequestOptions() { PartitionKey = new PartitionKey(AppId) };

        public UpdateGroceryList(ILogger<UpdateGroceryList> logger)
        {
            _logger = logger;
        }

        [Function("UpdateGroceryList")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            string listId = req.Headers["X-List-ID"];
            if (string.IsNullOrEmpty(listId))
            {
                return new BadRequestObjectResult("X-List-ID header is required");
            }

            GroceryListDB dataset = null;

            if (req.Method.ToLowerInvariant() == "post")
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogInformation("Request Body: " + body);
                    UpdateGroceryListPayload reqBody = JsonSerializer.Deserialize<UpdateGroceryListPayload>(body);
                    if (reqBody != null)
                    {
                        _logger.LogInformation("Deserialized successfully");
                        do
                        {
                            dataset = await JsonStore.GetGroceryList(listId);
                            bool mustCreateDataset = (dataset == null);
                            if (dataset == null)
                            {
                                dataset = new GroceryListDB(listId);
                            }

                            foreach (GroceryListItem item in reqBody.ToRemove)
                                {
                                    GroceryListItem itemToRemove = dataset.Items.Where(i => i.Item == item.Item).FirstOrDefault();
                                    if (itemToRemove != null)
                                    {
                                        dataset.Items.Remove(itemToRemove);
                                    }
                                }

                            foreach (GroceryListItem item in reqBody.ToAdd)
                            {
                                dataset.Items.Add(item);
                            }

                            _logger.LogInformation("dataset has " + dataset.Items.Count + " items");
                            _logger.LogInformation(JsonSerializer.Serialize(dataset));

                            if (mustCreateDataset)
                            {
                                dataset = await JsonStore.CreateGroceryList(dataset);
                            }
                            else
                            {
                                dataset = await JsonStore.UpdateGroceryList(dataset);
                            }
                        } while (dataset == null);
                    }
                }
            }
            else
            {
                dataset = await JsonStore.GetOrCreateGroceryList(listId);
            }

            string response = JsonSerializer.Serialize(dataset.Items);
            return new OkObjectResult(response);
        }
    }
}
