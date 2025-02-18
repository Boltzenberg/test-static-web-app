namespace Boltzenberg.Functions.DataModels.GroceryList
{
    public class GroceryListItem
    {
        public string Item { get; private set; }

        public GroceryListItem(string item)
        {
            this.Item = item;
        }

        public static List<GroceryListItem> GetCannedItems()
        {
            List<GroceryListItem> items = new List<GroceryListItem>();
            items.Add(new GroceryListItem("Item 1"));
            items.Add(new GroceryListItem("Item 2"));
            items.Add(new GroceryListItem("Item 3"));
            items.Add(new GroceryListItem("Item 4"));
            items.Add(new GroceryListItem("Item 5"));
            items.Add(new GroceryListItem("Item 6"));

            return items;
        }
    }
}