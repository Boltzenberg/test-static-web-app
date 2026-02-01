using System.ComponentModel.DataAnnotations;

namespace Boltzenberg.Functions.DataModels.GroceryList
{
    public class GroceryListDB : CosmosDocument
    {
        public static string GroceryListAppId = "GroceryList";

        public List<GroceryListItem> Items { get; set; }

        public GroceryListDB()
            : base(GroceryListAppId)
        {
            this.Items = new List<GroceryListItem>();
        }

        public GroceryListDB(string listId)
            : base(GroceryListAppId)
        {
            this.Items = new List<GroceryListItem>();
            this.id = listId;
        }

        public static GroceryListDB GetCannedDB(string listId)
        {
            GroceryListDB db = new GroceryListDB(listId);
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