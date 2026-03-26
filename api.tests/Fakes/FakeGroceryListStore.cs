using Boltzenberg.Functions.DataModels;
using Boltzenberg.Functions.Storage;
using Boltzenberg.Functions.Storage.Documents;

namespace ApiTests.Fakes
{
    public class FakeGroceryListStore : IJsonStore<GroceryListDocument>
    {
        private GroceryListDocument? _doc;
        private bool _failOnRead;
        private bool _failOnUpdate;

        public FakeGroceryListStore(GroceryListDocument? initialDoc = null, bool failOnRead = false, bool failOnUpdate = false)
        {
            _doc = initialDoc;
            _failOnRead = failOnRead;
            _failOnUpdate = failOnUpdate;
        }

        public Task<OperationResult<GroceryListDocument>> ReadAsync(string appId, string docId)
        {
            if (_failOnRead)
                return Task.FromResult(new OperationResult<GroceryListDocument>(ResultCode.GenericError, null, null));
            if (_doc == null)
                return Task.FromResult(new OperationResult<GroceryListDocument>(ResultCode.GenericError, null, null));
            // Return a copy so tests can mutate independently
            var copy = new GroceryListDocument
            {
                AppId = _doc.AppId,
                id = _doc.id,
                _etag = _doc._etag,
                Items = new List<string>(_doc.Items)
            };
            return Task.FromResult(new OperationResult<GroceryListDocument>(ResultCode.Success, copy, null));
        }

        public Task<OperationResult<GroceryListDocument>> UpdateAsync(GroceryListDocument entity)
        {
            if (_failOnUpdate)
                return Task.FromResult(new OperationResult<GroceryListDocument>(ResultCode.GenericError, null, null));
            _doc = new GroceryListDocument
            {
                AppId = entity.AppId,
                id = entity.id,
                _etag = "new-etag",
                Items = new List<string>(entity.Items)
            };
            return Task.FromResult(new OperationResult<GroceryListDocument>(ResultCode.Success, _doc, null));
        }

        public Task<OperationResult<GroceryListDocument>> CreateAsync(GroceryListDocument entity)
            => Task.FromResult(new OperationResult<GroceryListDocument>(ResultCode.Success, entity, null));

        public Task<OperationResult<GroceryListDocument>> UpsertAsync(GroceryListDocument entity)
            => Task.FromResult(new OperationResult<GroceryListDocument>(ResultCode.Success, entity, null));

        public Task<OperationResult<GroceryListDocument>> DeleteAsync(GroceryListDocument entity)
            => Task.FromResult(new OperationResult<GroceryListDocument>(ResultCode.Success, null, null));

        public Task<OperationResult<GroceryListDocument>> DeleteUnconditionallyAsync(string appId, string docId)
            => Task.FromResult(new OperationResult<GroceryListDocument>(ResultCode.Success, null, null));

        public Task<OperationResult<List<GroceryListDocument>>> ReadAllAsync(string appId)
            => Task.FromResult(new OperationResult<List<GroceryListDocument>>(ResultCode.Success, new List<GroceryListDocument>(), null));

        public GroceryListDocument? GetCurrentDoc() => _doc;
    }
}
