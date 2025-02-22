using System.ComponentModel.DataAnnotations;

namespace Boltzenberg.Functions.DataModels.GroceryList
{
    public class GroceryListDB
    {
        public string ListId { get; set; }
        public List<GroceryListItem> Items { get; set; }
        public string id { get; set; }
        public string _etag { get; set; }

        public GroceryListDB()
        {
            this.ListId = string.Empty;
            this.Items = new List<GroceryListItem>();
            this.id = string.Empty;
            this._etag = string.Empty;
        }

        public GroceryListDB(string listId)
        {
            this.ListId = listId;
            this.Items = new List<GroceryListItem>();
            this.id = string.Empty;
            this._etag = string.Empty;
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