namespace Boltzenberg.Functions.DataModels.AddressBook
{
    public class AddressBookEntry : CosmosDocument
    {
        public static string AddressBookEntryAppId = "AddressBookEntry";

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Street { get; set; }
        public string Apartment { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string PhoneNumber { get; set; }
        public string MailingName { get; set; }
        public string OtherPeople { get; set; }
        public string HolidayCard { get; set; }

        public AddressBookEntry()
            : base(AddressBookEntryAppId)
        {
            this.FirstName = string.Empty;
            this.LastName = string.Empty;
            this.Street = string.Empty;
            this.Apartment = string.Empty;
            this.City = string.Empty;
            this.State = string.Empty;
            this.ZipCode = string.Empty;
            this.PhoneNumber = string.Empty;
            this.MailingName = string.Empty;
            this.OtherPeople = string.Empty;
            this.HolidayCard = string.Empty;
        }

        public override string ToString()
        {
            return this.MailingName + Environment.NewLine + this.Street + " " + this.Apartment + Environment.NewLine + this.City + " " + this.State + " " + this.ZipCode;
        }
    }
}
