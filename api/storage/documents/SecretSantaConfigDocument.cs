using Boltzenberg.Functions.DataModels;

namespace Boltzenberg.Functions.Storage.Documents
{
    public class SecretSantaConfigDocument : CosmosDocument
    {
        public const string PartitionKey = "SecretSanta";
        public const string DocId = "SecretSantaConfig";

        public List<PersonRecord> People { get; set; } = new();
        public List<RestrictionRecord> Restrictions { get; set; } = new();

        public SecretSantaConfigDocument()
            : base(PartitionKey)
        {
            this.id = DocId;
        }

        public class PersonRecord
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class RestrictionRecord
        {
            public string Person1Email { get; set; } = string.Empty;
            public string Person2Email { get; set; } = string.Empty;
        }
    }
}
