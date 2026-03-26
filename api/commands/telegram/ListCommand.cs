using System.Text;
using Boltzenberg.Functions.Domain.Telegram;
using Boltzenberg.Functions.Storage;
using Boltzenberg.Functions.Storage.Documents;

namespace Boltzenberg.Functions.Commands.Telegram
{
    public class ListCommand : ICommand
    {
        private readonly IJsonStore<GroceryListDocument> _store;

        public ListCommand(IJsonStore<GroceryListDocument> store)
        {
            _store = store;
        }

        public string Name => "/list";
        public bool RequiresAuthorization => true;

        public async Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            context.Log.Info("Fetching grocery list");

            var result = await _store.ReadAsync(GroceryListDocument.PartitionKey, "Test");
            if (result.Code != ResultCode.Success || result.Entity == null)
            {
                context.Log.Error("Failed to read grocery list");
                return CommandResult.Fail("❌ Failed to get the grocery list. Check the logs.");
            }

            var sb = new StringBuilder();
            sb.AppendLine("🟢");
            foreach (var item in result.Entity.Items)
            {
                sb.AppendLine(item);
            }

            return CommandResult.Ok(sb.ToString());
        }
    }
}
