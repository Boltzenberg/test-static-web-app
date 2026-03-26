using System.Text;
using Boltzenberg.Functions.Domain;
using Boltzenberg.Functions.Domain.Telegram;
using Boltzenberg.Functions.Storage;
using Boltzenberg.Functions.Storage.Documents;

namespace Boltzenberg.Functions.Commands.Telegram
{
    public class AddItemCommand : ICommand
    {
        private readonly IJsonStore<GroceryListDocument> _store;

        public AddItemCommand(IJsonStore<GroceryListDocument> store)
        {
            _store = store;
        }

        public string Name => "/add";
        public bool RequiresAuthorization => true;

        public async Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            if (string.IsNullOrWhiteSpace(context.Arg))
            {
                return CommandResult.Fail("❌ Usage: /add <item>");
            }

            var itemToAdd = context.Arg;
            context.Log.Info("Adding item: {0}", itemToAdd);

            OperationResult<GroceryListDocument>? result = null;
            do
            {
                result = await _store.ReadAsync(GroceryListDocument.PartitionKey, "Test");
                if (result.Code != ResultCode.Success || result.Entity == null)
                {
                    context.Log.Error("Failed to read grocery list");
                    return CommandResult.Fail("❌ Failed to read the grocery list. Check the logs.");
                }

                var domainList = new Domain.GroceryList
                {
                    ListId = result.Entity.id,
                    Items = result.Entity.ToItemStrings()
                };

                result.Entity.SetItems(domainList.Apply(new[] { itemToAdd }, Array.Empty<string>()).Items);

                result = await _store.UpdateAsync(result.Entity);
            } while (result.Code == ResultCode.PreconditionFailed);

            if (result.Code != ResultCode.Success || result.Entity == null)
            {
                context.Log.Error("Failed to update grocery list");
                return CommandResult.Fail("❌ Failed to update the grocery list. Check the logs.");
            }

            var sb = new StringBuilder();
            sb.AppendLine("🟢");
            foreach (var item in result.Entity.ToItemStrings())
            {
                sb.AppendLine(item);
            }

            return CommandResult.Ok(sb.ToString());
        }
    }
}
