using Boltzenberg.Functions.DataModels;

namespace Boltzenberg.Functions.Storage.Documents
{
    public class RefreshableTokenDocument : CosmosDocument
    {
        public const string PartitionKey = "RefreshableTokenApp";

        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }

        public RefreshableTokenDocument()
            : base(PartitionKey)
        {
        }
    }
}
