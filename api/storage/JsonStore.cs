using Boltzenberg.Functions.DataModels.AddressBook;
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

        public static async Task<GroceryListDB> GetGroceryList(string listId)
        {
            QueryRequestOptions queryRequestOptions = new QueryRequestOptions() { PartitionKey = new PartitionKey(GroceryListDB.GroceryListAppId) };

            string query = "SELECT * FROM c WHERE c.AppId = @appId AND c.id = @listId";
            QueryDefinition queryDefinition = new QueryDefinition(query)
                .WithParameter("@appId", GroceryListDB.GroceryListAppId)
                .WithParameter("@listId", listId);

            FeedIterator<GroceryListDB> queryResultSetIterator = container.GetItemQueryIterator<GroceryListDB>(queryDefinition, requestOptions: queryRequestOptions);

            GroceryListDB dataset = null;
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<GroceryListDB> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                dataset = currentResultSet.FirstOrDefault();
            }

            return dataset;
        }

        public static async Task<GroceryListDB> GetOrCreateGroceryList(string listId)
        {
            GroceryListDB dataset = await GetGroceryList(listId);

            if (dataset == null)
            {
                dataset = new GroceryListDB(listId);
            }

            return dataset;
        }

        public static async Task<GroceryListDB> CreateGroceryList(GroceryListDB dataset)
        {
            ItemResponse<GroceryListDB> createResponse = await container.CreateItemAsync<GroceryListDB>(
                dataset,
                new PartitionKey(GroceryListDB.GroceryListAppId));
            return createResponse.Resource;
        }

        public static async Task<GroceryListDB> UpdateGroceryList(GroceryListDB dataset)
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
                    new PartitionKey(GroceryListDB.GroceryListAppId),
                    requestOptions);
                return updateResponse.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                // Handle the case where the ETag does not match
            }

            return null;
        }


        public static async Task<AddressBookEntry> CreateAddressBookEntry(AddressBookEntry entry)
        {
            ItemResponse<AddressBookEntry> createResponse = await container.CreateItemAsync<AddressBookEntry>(
                entry,
                new PartitionKey(AddressBookEntry.AddressBookEntryAppId));
            return createResponse.Resource;
        }

        public static async Task<AddressBookEntry> UpdateAddressBookEntry(AddressBookEntry entry)
        {
            try
            {
                ItemRequestOptions requestOptions = new ItemRequestOptions
                {
                    IfMatchEtag = entry._etag
                };

                ItemResponse<AddressBookEntry> updateResponse = await container.ReplaceItemAsync<AddressBookEntry>(
                    entry,
                    entry.id,
                    new PartitionKey(AddressBookEntry.AddressBookEntryAppId),
                    requestOptions);
                return updateResponse.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                // Handle the case where the ETag does not match
            }

            return null;
        }

        public static async Task<bool> DeleteAddressBookEntry(AddressBookEntry entry)
        {
            try
            {
                ItemRequestOptions requestOptions = new ItemRequestOptions
                {
                    IfMatchEtag = entry._etag
                };

                ItemResponse<AddressBookEntry> deleteResponse = await container.DeleteItemAsync<AddressBookEntry>(
                    entry.id,
                    new PartitionKey(AddressBookEntry.AddressBookEntryAppId),
                    requestOptions);
                return deleteResponse.StatusCode == System.Net.HttpStatusCode.NoContent;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                // Handle the case where the ETag does not match
            }

            return false;
        }

        public static async Task<List<AddressBookEntry>> GetAddressBookEntries()
        {
            QueryRequestOptions queryRequestOptions = new QueryRequestOptions() { PartitionKey = new PartitionKey(AddressBookEntry.AddressBookEntryAppId) };

            string query = "SELECT * FROM c WHERE c.AppId = @appId";
            QueryDefinition queryDefinition = new QueryDefinition(query)
                .WithParameter("@appId", AddressBookEntry.AddressBookEntryAppId);

            FeedIterator<AddressBookEntry> iterator = container.GetItemQueryIterator<AddressBookEntry>(queryDefinition, requestOptions: queryRequestOptions);

            List<AddressBookEntry> results = new List<AddressBookEntry>();
            while (iterator.HasMoreResults)
            {
                FeedResponse<AddressBookEntry> currentResults = await iterator.ReadNextAsync();
                results.AddRange(currentResults);
            }

            return results;
        }
    }
}