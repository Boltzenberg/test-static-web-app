using Boltzenberg.Functions.Dtos.AddressBook;
using Xunit;

namespace ApiTests.Dtos
{
    public class VersionTokenTests
    {
        [Fact]
        public void VersionToken_RoundTripsEtagThroughResponse()
        {
            // Simulate: response includes _etag as VersionToken
            const string etag = "\"00000001-0000-0000-0000-000000000001\"";

            var response = new AddressBookEntryResponse(
                Id: "id-123",
                VersionToken: etag,
                FirstName: "John",
                LastName: "Doe",
                Street: "123 Main St",
                Apartment: null,
                City: "Springfield",
                State: "IL",
                ZipCode: "62701",
                PhoneNumber: null,
                MailingName: null,
                OtherPeople: null,
                HolidayCard: false,
                MailingLabel: "John Doe\n123 Main St\nSpringfield, IL 62701"
            );

            // The version token from the response should be the original etag
            Assert.Equal(etag, response.VersionToken);
        }

        [Fact]
        public void VersionToken_UpdateRequestMapsBackToEtag()
        {
            // Simulate: client echoes back the VersionToken in an update request
            const string etag = "\"00000001-0000-0000-0000-000000000001\"";

            var updateRequest = new AddressBookEntryUpdateRequest(
                Id: "id-123",
                VersionToken: etag,  // echoed back from response
                FirstName: "John",
                LastName: "Doe",
                Street: "123 Main St",
                Apartment: null,
                City: "Springfield",
                State: "IL",
                ZipCode: "62701",
                PhoneNumber: null,
                MailingName: null,
                OtherPeople: null,
                HolidayCard: false
            );

            // The function would map VersionToken → _etag for OCC
            string mappedEtag = updateRequest.VersionToken;
            Assert.Equal(etag, mappedEtag);
        }

        [Fact]
        public void VersionToken_IsPreservedAsOpaqueString()
        {
            // The version token should be treated as an opaque string —
            // whatever value was put in should come back out unchanged
            const string versionToken = "some-opaque-value-12345";

            var response = new AddressBookEntryResponse(
                Id: "id-456",
                VersionToken: versionToken,
                FirstName: "Jane",
                LastName: "Smith",
                Street: "456 Oak Ave",
                Apartment: null,
                City: "Chicago",
                State: "IL",
                ZipCode: "60601",
                PhoneNumber: null,
                MailingName: null,
                OtherPeople: null,
                HolidayCard: true,
                MailingLabel: "Jane Smith\n456 Oak Ave\nChicago, IL 60601"
            );

            Assert.Equal(versionToken, response.VersionToken);
        }
    }
}
