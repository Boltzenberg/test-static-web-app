namespace Boltzenberg.Functions
{
    public class UpdateGroceryListPayload
    {
        public List<GroceryListItem> ToAdd { get; set; }
        public List<GroceryListItem> ToRemove { get; set; }

        public UpdateGroceryListPayload()
        {
            this.ToAdd = new List<GroceryListItem>();
            this.ToRemove = new List<GroceryListItem>();
        }
    }
}