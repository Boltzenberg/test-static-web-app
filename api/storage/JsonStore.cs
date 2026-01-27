using Boltzenberg.Functions.DataModels.GroceryList;
using Microsoft.Azure.Cosmos;

namespace Boltzenberg.Functions.Storage
{
    public static class JsonStore
    {
        private const string EndpointUri = "https://gunga-test-cosmosdb.documents.azure.com:443/";
        private static string PrimaryKey = Environment.GetEnvironmentVariable("GROCERY_LIST_PRIMARY_KEY");
        private static CosmosClient cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
        private static Database database = cosmosClient.GetDatabase("GungaDB");
        private static Container container = database.GetContainer("DocumentsContainer");

        public static async Task<GroceryListDB> GetGroceryListFromCosmos(string listId)
        {
            const string AppId = "GroceryList";
            QueryRequestOptions queryRequestOptions = new QueryRequestOptions() { PartitionKey = new PartitionKey(AppId) };

            string query = "SELECT * FROM c WHERE c.AppId = @appId AND c.ListId = @listId";
            QueryDefinition queryDefinition = new QueryDefinition(query)
                .WithParameter("@appId", AppId)
                .WithParameter("@listId", listId);

            FeedIterator<GroceryListDB> queryResultSetIterator = container.GetItemQueryIterator<GroceryListDB>(queryDefinition, requestOptions: queryRequestOptions);

            GroceryListDB dataset = null;
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<GroceryListDB> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                dataset = currentResultSet.FirstOrDefault();
            }

            if (dataset == null)
            {
                dataset = new GroceryListDB(listId);
            }

            return dataset;
        }

        public static async Task<GroceryListDB> UpdateGroceryListToCosmos(GroceryListDB dataset)
        {
            const string AppId = "GroceryList";

            try
            {
                ItemRequestOptions requestOptions = new ItemRequestOptions
                {
                    IfMatchEtag = dataset._etag
                };

                ItemResponse<GroceryListDB> updateResponse = await container.ReplaceItemAsync<GroceryListDB>(
                    dataset,
                    dataset.id,
                    new PartitionKey(AppId),
                    requestOptions);
                return updateResponse.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                // Handle the case where the ETag does not match
            }

            return null;
        }
    }
}