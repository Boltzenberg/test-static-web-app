namespace Boltzenberg.Functions.DataModels.GroceryList
{
    public class GroceryListItem
    {
        public string Item { get; private set; }

        public GroceryListItem(string item)
        {
            this.Item = item;
        }
    }
}