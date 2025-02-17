namespace Boltzenberg.Functions
{
    public class GroceryListItem
    {
        public int ListId { get; private set; }
        public string Item { get; private set; }
        public bool IsDeleted { get; private set; }

        public GroceryListItem(int listId, string item, bool isDeleted)
        {
            this.ListId = listId;
            this.Item = item;
            this.IsDeleted = isDeleted;
        }

        public static List<GroceryListItem> GetCannedItems()
        {
            List<GroceryListItem> items = new List<GroceryListItem>();
            items.Add(new GroceryListItem(1, "Item 1", true));
            items.Add(new GroceryListItem(1, "Item 2", false));
            items.Add(new GroceryListItem(1, "Item 3", false));
            items.Add(new GroceryListItem(1, "Item 4", false));
            items.Add(new GroceryListItem(1, "Item 5", true));
            items.Add(new GroceryListItem(1, "Item 6", false));

            return items;
        }
    }
}