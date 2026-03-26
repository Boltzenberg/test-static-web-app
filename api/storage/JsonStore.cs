using Boltzenberg.Functions.DataModels;
using Microsoft.Azure.Cosmos;

namespace Boltzenberg.Functions.Storage
{
    /// <summary>
    /// Generic Cosmos DB store. Use via DI as IJsonStore&lt;T&gt;.
    /// </summary>
    public class JsonStore<T> : IJsonStore<T> where T : CosmosDocument
    {
        private const string EndpointUri = "https://gunga-test-cosmosdb.documents.azure.com:443/";
        private static readonly string? PrimaryKey = Environment.GetEnvironmentVariable("GROCERY_LIST_PRIMARY_KEY");
        private static readonly CosmosClient _cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);
        private static readonly Database _database = _cosmosClient.GetDatabase("GungaDB");
        private static readonly Container _container = _database.GetContainer("DocumentsContainer");

        public async Task<OperationResult<T>> CreateAsync(T entity)
        {
            try
            {
                ItemResponse<T> createResponse = await _container.CreateItemAsync<T>(
                    entity,
                    new PartitionKey(entity.AppId));

                if (createResponse.StatusCode == System.Net.HttpStatusCode.Created)
                    return new OperationResult<T>(ResultCode.Success, createResponse.Resource, null);
                else
                    return new OperationResult<T>(ResultCode.GenericError, createResponse.Resource, null);
            }
            catch (Exception ex)
            {
                return new OperationResult<T>(ResultCode.GenericError, null, ex);
            }
        }

        public async Task<OperationResult<T>> ReadAsync(string appId, string docId)
        {
            QueryRequestOptions queryRequestOptions = new QueryRequestOptions() { PartitionKey = new PartitionKey(appId) };

            string query = "SELECT * FROM c WHERE c.AppId = @appId AND c.id = @docId";
            QueryDefinition queryDefinition = new QueryDefinition(query)
                .WithParameter("@appId", appId)
                .WithParameter("@docId", docId);

            try
            {
                FeedIterator<T> queryResultSetIterator = _container.GetItemQueryIterator<T>(queryDefinition, requestOptions: queryRequestOptions);

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

        public async Task<OperationResult<T>> UpdateAsync(T entity)
        {
            try
            {
                ItemRequestOptions requestOptions = new ItemRequestOptions
                {
                    IfMatchEtag = entity._etag
                };

                ItemResponse<T> updateResponse = await _container.ReplaceItemAsync<T>(
                    entity,
                    entity.id,
                    new PartitionKey(entity.AppId),
                    requestOptions);

                if (updateResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    return new OperationResult<T>(ResultCode.Success, updateResponse.Resource, null);
                else
                    return new OperationResult<T>(ResultCode.GenericError, updateResponse.Resource, null);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                return new OperationResult<T>(ResultCode.PreconditionFailed, null, null);
            }
        }

        public async Task<OperationResult<T>> UpsertAsync(T entity)
        {
            try
            {
                ItemResponse<T> upsertResponse = await _container.UpsertItemAsync<T>(
                    entity,
                    new PartitionKey(entity.AppId));

                if (upsertResponse.StatusCode == System.Net.HttpStatusCode.OK ||
                    upsertResponse.StatusCode == System.Net.HttpStatusCode.Created)
                    return new OperationResult<T>(ResultCode.Success, upsertResponse.Resource, null);
                else
                    return new OperationResult<T>(ResultCode.GenericError, upsertResponse.Resource, null);
            }
            catch (CosmosException)
            {
                return new OperationResult<T>(ResultCode.GenericError, null, null);
            }
        }

        public async Task<OperationResult<T>> DeleteAsync(T entity)
        {
            try
            {
                ItemRequestOptions requestOptions = new ItemRequestOptions
                {
                    IfMatchEtag = entity._etag
                };

                ItemResponse<T> deleteResponse = await _container.DeleteItemAsync<T>(
                    entity.id,
                    new PartitionKey(entity.AppId),
                    requestOptions);

                if (deleteResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return new OperationResult<T>(ResultCode.Success, null, null);
                else
                    return new OperationResult<T>(ResultCode.GenericError, null, null);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
            {
                return new OperationResult<T>(ResultCode.PreconditionFailed, null, null);
            }
        }

        public async Task<OperationResult<T>> DeleteUnconditionallyAsync(string appId, string docId)
        {
            try
            {
                ItemResponse<T> deleteResponse = await _container.DeleteItemAsync<T>(
                    docId,
                    new PartitionKey(appId));

                if (deleteResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return new OperationResult<T>(ResultCode.Success, null, null);
                else
                    return new OperationResult<T>(ResultCode.GenericError, null, null);
            }
            catch (CosmosException)
            {
                return new OperationResult<T>(ResultCode.GenericError, null, null);
            }
        }

        public async Task<OperationResult<List<T>>> ReadAllAsync(string appId)
        {
            QueryRequestOptions queryRequestOptions = new QueryRequestOptions() { PartitionKey = new PartitionKey(appId) };

            string query = "SELECT * FROM c WHERE c.AppId = @appId";
            QueryDefinition queryDefinition = new QueryDefinition(query)
                .WithParameter("@appId", appId);

            try
            {
                FeedIterator<T> iterator = _container.GetItemQueryIterator<T>(queryDefinition, requestOptions: queryRequestOptions);

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
