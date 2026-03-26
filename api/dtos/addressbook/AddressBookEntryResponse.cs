namespace Boltzenberg.Functions.Dtos.AddressBook
{
    public record AddressBookEntryResponse(
        string Id,
        string VersionToken,
        string FirstName,
        string LastName,
        string Street,
        string? Apartment,
        string City,
        string State,
        string ZipCode,
        string? PhoneNumber,
        string? MailingName,
        string? OtherPeople,
        bool HolidayCard,
        string MailingLabel
    );
}
