using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Boltzenberg.Functions.DataModels.GroceryList;

namespace Boltzenberg.Functions
{
    public class UpdateGroceryList
    {
        private readonly ILogger<UpdateGroceryList> _logger;
        private const string EndpointUri = "https://gunga-test-cosmosdb.documents.azure.com:443/";
        private static string PrimaryKey = Environment.GetEnvironmentVariable("GROCERY_LIST_PRIMARY_KEY");
        private static CosmosClient cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
        private static Database database = cosmosClient.GetDatabase("GungaDB");
        private static Container container = database.GetContainer("GroceryList");

        public UpdateGroceryList(ILogger<UpdateGroceryList> logger)
        {
            _logger = logger;
        }

        private async Task<GroceryListDB> GetGroceryListFromCosmos(string listId)
        {
            string query = "SELECT * FROM c WHERE c.ListId = @listId";
            QueryDefinition queryDefinition = new QueryDefinition(query).WithParameter("@listId", listId);
            FeedIterator<GroceryListDB> queryResultSetIterator = container.GetItemQueryIterator<GroceryListDB>(queryDefinition);

            GroceryListDB dataset = null;
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<GroceryListDB> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                dataset = currentResultSet.FirstOrDefault();
            }

            if (dataset == null)
            {
                dataset = new GroceryListDB();
            }

            return dataset;
        }

        private async Task<GroceryListDB> UpdateGroceryListToCosmos(GroceryListDB dataset)
        {
            try
            {
                ItemRequestOptions requestOptions = new ItemRequestOptions
                {
                    IfMatchEtag = dataset._etag
                };

                ItemResponse<GroceryListDB> updateResponse = await container.ReplaceItemAsync<GroceryListDB>(
                    dataset,
                    dataset.id,
                    new PartitionKey(dataset.ListId),
                    requestOptions);
                return updateResponse.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                // Handle the case where the ETag does not match
            }

            return null;
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
                    UpdateGroceryListPayload reqBody = JsonSerializer.Deserialize<UpdateGroceryListPayload>(body);
                    if (reqBody != null)
                    {
                        do
                        {
                            dataset = await this.GetGroceryListFromCosmos(listId);

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

                            dataset = await this.UpdateGroceryListToCosmos(dataset);
                        } while (dataset == null);
                    }
                }
            }
            else
            {
                dataset = await this.GetGroceryListFromCosmos(listId);
            }

            string response = JsonSerializer.Serialize(dataset.Items);
            return new OkObjectResult(response);
        }
    }
}
