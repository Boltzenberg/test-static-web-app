using Boltzenberg.Functions.DataModels;
using Microsoft.Azure.Cosmos;

namespace Boltzenberg.Functions.Storage
{
    public static class JsonStore
    {
        private const string EndpointUri = "https://gunga-test-cosmosdb.documents.azure.com:443/";
        private static string? PrimaryKey = Environment.GetEnvironmentVariable("GROCERY_LIST_PRIMARY_KEY");
        private static CosmosClient cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
        private static Database database = cosmosClient.GetDatabase("GungaDB");
        private static Container container = database.GetContainer("DocumentsContainer");

        public static async Task<OperationResult<T>> Create<T>(T entity) where T : CosmosDocument
        {
            try
            {
                ItemResponse<T> createResponse = await container.CreateItemAsync<T>(
                    entity,
                    new PartitionKey(entity.AppId));

                if (createResponse.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    return new OperationResult<T>(ResultCode.Success, createResponse.Resource, null);
                }
                else
                {
                    return new OperationResult<T>(ResultCode.GenericError, createResponse.Resource, null);
                }
            }
            catch (Exception ex)
            {
                return new OperationResult<T>(ResultCode.GenericError, null, ex);
            }
        }

        public static async Task<OperationResult<T>> Read<T>(string appId, string docId) where T : CosmosDocument
        {
            QueryRequestOptions queryRequestOptions = new QueryRequestOptions() { PartitionKey = new PartitionKey(appId) };

            string query = "SELECT * FROM c WHERE c.AppId = @appId AND c.id = @docId";
            QueryDefinition queryDefinition = new QueryDefinition(query)
                .WithParameter("@appId", appId)
                .WithParameter("@docId", docId);

            try
            {
                FeedIterator<T> queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition, requestOptions: queryRequestOptions);

                T? entity = null;
                if (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<T> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                    entity = currentResultSet.FirstOrDefault();
                }

                return new OperationResult<T>(ResultCode.Success, entity, null);
            }
            catch (Exception ex)
            {
                return new OperationResult<T>(ResultCode.GenericError, null, ex);
            }
        }

        public static async Task<OperationResult<T>> Update<T>(T entity) where T : CosmosDocument
        {
            try
            {
                ItemRequestOptions requestOptions = new ItemRequestOptions
                {
                    IfMatchEtag = entity._etag
                };

                ItemResponse<T> updateResponse = await container.ReplaceItemAsync<T>(
                    entity,
                    entity.id,
                    new PartitionKey(entity.AppId),
                    requestOptions);

                if (updateResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return new OperationResult<T>(ResultCode.Success, updateResponse.Resource, null);
                }
                else
                {
                    return new OperationResult<T>(ResultCode.GenericError, updateResponse.Resource, null);
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                return new OperationResult<T>(ResultCode.PreconditionFailed, null, null);
            }
        }

        public static async Task<OperationResult<T>> Upsert<T>(T entity) where T : CosmosDocument
        {
            try
            {
                ItemResponse<T> upsertResponse = await container.UpsertItemAsync<T>(
                    entity,
                    new PartitionKey(entity.AppId));

                if (upsertResponse.StatusCode == System.Net.HttpStatusCode.OK ||
                    upsertResponse.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    return new OperationResult<T>(ResultCode.Success, upsertResponse.Resource, null);
                }
                else
                {
                    return new OperationResult<T>(ResultCode.GenericError, upsertResponse.Resource, null);
                }
            }
            catch (CosmosException)
            {
                return new OperationResult<T>(ResultCode.GenericError, null, null);
            }
        }

        public static async Task<OperationResult<T>> Delete<T>(T entity) where T : CosmosDocument
        {
            try
            {
                ItemRequestOptions requestOptions = new ItemRequestOptions
                {
                    IfMatchEtag = entity._etag
                };

                ItemResponse<T> deleteResponse = await container.DeleteItemAsync<T>(
                    entity.id,
                    new PartitionKey(entity.AppId),
                    requestOptions);

                if (deleteResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return new OperationResult<T>(ResultCode.Success, null, null);
                }
                else
                {
                    return new OperationResult<T>(ResultCode.GenericError, null, null);
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                return new OperationResult<T>(ResultCode.PreconditionFailed, null, null);
            }
        }

        public static async Task<OperationResult<T>> DeleteUnconditionally<T>(string appId, string docId) where T : CosmosDocument
        {
            try
            {
                ItemResponse<T> deleteResponse = await container.DeleteItemAsync<T>(
                    docId,
                    new PartitionKey(appId));

                if (deleteResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    return new OperationResult<T>(ResultCode.Success, null, null);
                }
                else
                {
                    return new OperationResult<T>(ResultCode.GenericError, null, null);
                }
            }
            catch (CosmosException)
            {
                return new OperationResult<T>(ResultCode.GenericError, null, null);
            }
        }

        public static async Task<OperationResult<List<T>>> ReadAll<T>(string appId) where T : CosmosDocument
        {
            QueryRequestOptions queryRequestOptions = new QueryRequestOptions() { PartitionKey = new PartitionKey(appId) };

            string query = "SELECT * FROM c WHERE c.AppId = @appId";
            QueryDefinition queryDefinition = new QueryDefinition(query)
                .WithParameter("@appId", appId);

            try
            {
                FeedIterator<T> iterator = container.GetItemQueryIterator<T>(queryDefinition, requestOptions: queryRequestOptions);

                List<T> results = new List<T>();
                while (iterator.HasMoreResults)
                {
                    FeedResponse<T> currentResults = await iterator.ReadNextAsync();
                    results.AddRange(currentResults);
                }

                return new OperationResult<List<T>>(ResultCode.Success, results, null);
            }
            catch (Exception ex)
            {
                return new OperationResult<List<T>>(ResultCode.GenericError, null, ex);
            }
        }
    }
}