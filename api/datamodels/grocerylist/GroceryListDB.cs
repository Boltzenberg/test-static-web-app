namespace Boltzenberg.Functions.DataModels.GroceryList
{
    public class GroceryListDB
    {
        public string Id { get; set; }
        public List<GroceryListItem> Items { get; set; }

        public GroceryListDB()
        {
            this.Id = string.Empty;
            this.Items = new List<GroceryListItem>();
        }

        public static GroceryListDB GetCannedDB(string listId)
        {
            GroceryListDB db = new GroceryListDB();
            db.Id = listId;
            db.Items.Add(new GroceryListItem("Item 1"));
            db.Items.Add(new GroceryListItem("Item 2"));
            db.Items.Add(new GroceryListItem("Item 3"));
            db.Items.Add(new GroceryListItem("Item 4"));
            db.Items.Add(new GroceryListItem("Item 5"));
            db.Items.Add(new GroceryListItem("Item 6"));

            return db;
        }    
    }
}