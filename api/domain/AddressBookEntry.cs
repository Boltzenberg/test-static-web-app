namespace Boltzenberg.Functions.Domain
{
    public class AddressBookEntry
    {
        public string Id { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Street { get; init; } = string.Empty;
        public string? Apartment { get; init; }
        public string City { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string ZipCode { get; init; } = string.Empty;
        public string? PhoneNumber { get; init; }
        public string? MailingName { get; init; }
        public string? OtherPeople { get; init; }
        public bool HolidayCard { get; init; }

        public string MailingLabel => string.Join("\n",
            new[] { MailingName ?? $"{FirstName} {LastName}", Street,
                    Apartment, $"{City}, {State} {ZipCode}" }
                .Where(l => !string.IsNullOrWhiteSpace(l)));
    }
}
