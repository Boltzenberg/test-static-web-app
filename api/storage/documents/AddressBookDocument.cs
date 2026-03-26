using Boltzenberg.Functions.DataModels;

namespace Boltzenberg.Functions.Storage.Documents
{
    public class AddressBookDocument : CosmosDocument
    {
        public const string PartitionKey = "AddressBookEntry";

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string? Apartment { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? MailingName { get; set; }
        public string? OtherPeople { get; set; }
        public bool HolidayCard { get; set; }

        public AddressBookDocument()
            : base(PartitionKey)
        {
        }
    }
}
