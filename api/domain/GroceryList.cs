namespace Boltzenberg.Functions.Domain
{
    public class GroceryList
    {
        public string ListId { get; init; } = string.Empty;
        public IReadOnlyList<string> Items { get; init; } = new List<string>();

        public GroceryList Apply(IEnumerable<string> toAdd, IEnumerable<string> toRemove)
            => new GroceryList
            {
                ListId = ListId,
                Items = Items
                    .Except(toRemove, StringComparer.OrdinalIgnoreCase)
                    .Concat(toAdd)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList()
            };
    }
}
