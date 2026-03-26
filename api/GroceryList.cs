using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Boltzenberg.Functions.Storage;
using Boltzenberg.Functions.Storage.Documents;
using Boltzenberg.Functions.Domain;
using Boltzenberg.Functions.Dtos.GroceryList;
using Boltzenberg.Functions.Logging;

namespace Boltzenberg.Functions
{
    public class GroceryList
    {
        private readonly IJsonStore<GroceryListDocument> _store;

        public GroceryList(IJsonStore<GroceryListDocument> store)
        {
            _store = store;
        }

        [Function("CreateGroceryList")]
        public async Task<IActionResult> CreateGroceryListUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
            await LogBuffer.Wrap("CreateGroceryList", req, CreateGroceryList);
        private async Task<IActionResult> CreateGroceryList(HttpRequest req, LogBuffer log)
        {
            string? listId = req.Headers["X-List-ID"];
            if (string.IsNullOrEmpty(listId))
            {
                log.Error("Invalid or missing list id '{0}'", listId ?? "<null>");
                return new BadRequestObjectResult("X-List-ID header is required");
            }

            var doc = new GroceryListDocument { id = listId };
            var result = await _store.CreateAsync(doc);
            if (result.Code != ResultCode.Success)
            {
                log.Error("Failed to create the grocery list: {0}", (result.Error != null) ? result.Error.ToString() : "<no exception returned>");
                return new BadRequestObjectResult(result.Error);
            }

            log.Info("Created grocery list '{0}'", listId);
            return new CreatedResult();
        }

        [Function("UpdateGroceryList")]
        public async Task<IActionResult> UpdateGroceryListUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req) =>
            await LogBuffer.Wrap("UpdateGroceryList", req, UpdateGroceryList);
        private async Task<IActionResult> UpdateGroceryList(HttpRequest req, LogBuffer log)
        {
            string? listId = req.Headers["X-List-ID"];
            if (string.IsNullOrEmpty(listId))
            {
                log.Error("Invalid list id '{0}'", listId ?? "<null>");
                return new BadRequestObjectResult("X-List-ID header is required");
            }

            OperationResult<GroceryListDocument>? result = null;

            if (string.Equals(req.Method, "post", StringComparison.OrdinalIgnoreCase))
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                if (string.IsNullOrEmpty(body))
                {
                    log.Error("No request body!");
                    return new BadRequestResult();
                }

                UpdateGroceryListRequest? payload = JsonSerializer.Deserialize<UpdateGroceryListRequest>(body);
                if (payload == null)
                {
                    log.Error("Failed to deserialize request body '{0}'", body);
                    return new BadRequestResult();
                }

                foreach (string item in payload.ToRemove) log.Info("Removing '{0}'", item);
                foreach (string item in payload.ToAdd) log.Info("Adding '{0}'", item);

                do
                {
                    result = await _store.ReadAsync(GroceryListDocument.PartitionKey, listId);
                    if (result == null || result.Entity == null || result.Code == ResultCode.GenericError)
                    {
                        log.Error("Failed to find the grocery list with id '{0}'", listId);
                        return new BadRequestResult();
                    }

                    var domain = new Domain.GroceryList { ListId = result.Entity.id, Items = result.Entity.ToItemStrings() };
                    result.Entity.SetItems(domain.Apply(payload.ToAdd, payload.ToRemove).Items);
                    result = await _store.UpdateAsync(result.Entity);
                } while (result.Code == ResultCode.PreconditionFailed);
            }
            else
            {
                result = await _store.ReadAsync(GroceryListDocument.PartitionKey, listId);
            }

            if (result == null || result.Entity == null)
            {
                log.Error("Failed to get the grocery list to return.  Returning bad request.");
                return new BadRequestResult();
            }

            var response = new GroceryListResponse(result.Entity.id, result.Entity.ToItemStrings());
            return new OkObjectResult(JsonSerializer.Serialize(response));
        }
    }
}
