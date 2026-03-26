using Boltzenberg.Functions.DataModels;

namespace Boltzenberg.Functions.Storage.Documents
{
    public class GroceryListDocument : CosmosDocument
    {
        public const string PartitionKey = "GroceryList";

        public List<string> Items { get; set; } = new();

        public GroceryListDocument()
            : base(PartitionKey)
        {
        }
    }
}
