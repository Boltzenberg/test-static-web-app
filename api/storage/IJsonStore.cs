using Boltzenberg.Functions.DataModels;

namespace Boltzenberg.Functions.Storage
{
    public interface IJsonStore<T> where T : CosmosDocument
    {
        Task<OperationResult<T>> CreateAsync(T entity);
        Task<OperationResult<T>> ReadAsync(string appId, string docId);
        Task<OperationResult<T>> UpdateAsync(T entity);
        Task<OperationResult<T>> UpsertAsync(T entity);
        Task<OperationResult<T>> DeleteAsync(T entity);
        Task<OperationResult<T>> DeleteUnconditionallyAsync(string appId, string docId);
        Task<OperationResult<List<T>>> ReadAllAsync(string appId);
    }
}
