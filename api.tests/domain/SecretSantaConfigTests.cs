using Boltzenberg.Functions.Domain;
using Xunit;

namespace ApiTests.Domain
{
    public class SecretSantaConfigTests
    {
        private static SecretSantaConfig MakeValidConfig()
        {
            return new SecretSantaConfig
            {
                People = new List<SecretSantaConfig.Person>
                {
                    new("Alice", "alice@example.com"),
                    new("Bob", "bob@example.com"),
                    new("Carol", "carol@example.com")
                },
                Restrictions = new List<SecretSantaConfig.Restriction>
                {
                    new("alice@example.com", "bob@example.com")
                }
            };
        }

        [Fact]
        public void Validate_PassesForValidConfig()
        {
            var config = MakeValidConfig();
            // Should not throw
            config.Validate();
        }

        [Fact]
        public void Validate_PassesForConfigWithNoRestrictions()
        {
            var config = new SecretSantaConfig
            {
                People = new List<SecretSantaConfig.Person>
                {
                    new("Alice", "alice@example.com"),
                    new("Bob", "bob@example.com")
                },
                Restrictions = new List<SecretSantaConfig.Restriction>()
            };
            config.Validate(); // should not throw
        }

        [Fact]
        public void Validate_FailsWhenRestrictionReferencesUnknownPerson1Email()
        {
            var config = MakeValidConfig();
            config.Restrictions.Add(new SecretSantaConfig.Restriction("unknown@example.com", "alice@example.com"));
            Assert.Throws<InvalidDataException>(() => config.Validate());
        }

        [Fact]
        public void Validate_FailsWhenRestrictionReferencesUnknownPerson2Email()
        {
            var config = MakeValidConfig();
            config.Restrictions.Add(new SecretSantaConfig.Restriction("alice@example.com", "nobody@example.com"));
            Assert.Throws<InvalidDataException>(() => config.Validate());
        }

        [Fact]
        public void Validate_FailsWhenBothRestrictionEmailsAreUnknown()
        {
            var config = MakeValidConfig();
            config.Restrictions.Add(new SecretSantaConfig.Restriction("x@example.com", "y@example.com"));
            Assert.Throws<InvalidDataException>(() => config.Validate());
        }
    }
}
