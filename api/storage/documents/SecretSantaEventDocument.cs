using Boltzenberg.Functions.DataModels;

namespace Boltzenberg.Functions.Storage.Documents
{
    public class SecretSantaEventDocument : CosmosDocument
    {
        public const string PartitionKey = "SecretSantaEvent";

        public bool IsRunning { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int Year { get; set; }
        public List<ParticipantDocument> Participants { get; set; } = new();

        public SecretSantaEventDocument()
            : base(PartitionKey)
        {
        }
    }

    public class ParticipantDocument
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? SantaForName { get; set; }
        public string? SantaForEmail { get; set; }
    }
}
