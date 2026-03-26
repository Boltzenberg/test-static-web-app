using Boltzenberg.Functions.Domain;
using Xunit;

namespace ApiTests.Domain
{
    public class AddressBookEntryTests
    {
        private static AddressBookEntry MakeEntry(
            string firstName = "John",
            string lastName = "Doe",
            string street = "123 Main St",
            string? apartment = null,
            string city = "Springfield",
            string state = "IL",
            string zipCode = "62701",
            string? mailingName = null)
            => new AddressBookEntry
            {
                Id = "id1",
                FirstName = firstName,
                LastName = lastName,
                Street = street,
                Apartment = apartment,
                City = city,
                State = state,
                ZipCode = zipCode,
                MailingName = mailingName
            };

        [Fact]
        public void MailingLabel_OmitsNullApartment()
        {
            var entry = MakeEntry(apartment: null);
            var label = entry.MailingLabel;
            Assert.DoesNotContain("\n\n", label); // no blank line
            var lines = label.Split('\n');
            Assert.Equal(3, lines.Length);
        }

        [Fact]
        public void MailingLabel_IncludesApartmentWhenSet()
        {
            var entry = MakeEntry(apartment: "Apt 4B");
            var label = entry.MailingLabel;
            Assert.Contains("Apt 4B", label);
            var lines = label.Split('\n');
            Assert.Equal(4, lines.Length);
        }

        [Fact]
        public void MailingLabel_UsesMailingNameOverFullName()
        {
            var entry = MakeEntry(firstName: "John", lastName: "Doe", mailingName: "The Doe Family");
            var label = entry.MailingLabel;
            Assert.StartsWith("The Doe Family", label);
            Assert.DoesNotContain("John Doe", label);
        }

        [Fact]
        public void MailingLabel_UsesFullNameWhenNoMailingName()
        {
            var entry = MakeEntry(firstName: "John", lastName: "Doe", mailingName: null);
            var label = entry.MailingLabel;
            Assert.StartsWith("John Doe", label);
        }

        [Fact]
        public void MailingLabel_FormatsCorrectly()
        {
            var entry = MakeEntry(
                firstName: "Jane",
                lastName: "Smith",
                street: "456 Oak Ave",
                apartment: null,
                city: "Chicago",
                state: "IL",
                zipCode: "60601");
            var label = entry.MailingLabel;
            var lines = label.Split('\n');
            Assert.Equal("Jane Smith", lines[0]);
            Assert.Equal("456 Oak Ave", lines[1]);
            Assert.Equal("Chicago, IL 60601", lines[2]);
        }
    }
}
