namespace Boltzenberg.Functions.DataModels
{
    public class CosmosDocument
    {
        public string AppId { get; set; }
        public string id { get; set; }
        public string _etag { get; set; }

        public CosmosDocument(string appId)
        {
            this.AppId = appId;
            this.id = string.Empty;
            this._etag = string.Empty;
        }
    }
}