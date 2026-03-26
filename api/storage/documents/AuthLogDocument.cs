using Boltzenberg.Functions.DataModels;

namespace Boltzenberg.Functions.Storage.Documents
{
    public class AuthLogDocument : CosmosDocument
    {
        public const string PartitionKey = "AuthLog";

        public string Line { get; set; } = string.Empty;

        public AuthLogDocument()
            : base(PartitionKey)
        {
        }
    }
}
