using Boltzenberg.Functions.DataModels;

namespace Boltzenberg.Functions.Storage.Documents
{
    public class GroceryListDocument : CosmosDocument
    {
        public const string PartitionKey = "GroceryList";

        /// <summary>
        /// Stored as [{"Item": "milk"}, ...] in Cosmos to match the existing document shape.
        /// Use ToItemStrings() / SetItems() to work with plain strings.
        /// </summary>
        public List<ItemRecord> Items { get; set; } = new();

        public GroceryListDocument()
            : base(PartitionKey)
        {
        }

        public List<string> ToItemStrings() => Items.Select(i => i.Item).ToList();

        public void SetItems(IEnumerable<string> items)
            => Items = items.Select(s => new ItemRecord { Item = s }).ToList();

        public class ItemRecord
        {
            public string Item { get; set; } = string.Empty;
        }
    }
}
