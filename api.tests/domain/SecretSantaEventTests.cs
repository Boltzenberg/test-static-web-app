using Boltzenberg.Functions.Domain;
using Xunit;

namespace ApiTests.Domain
{
    public class SecretSantaEventTests
    {
        private static SecretSantaConfig MakeConfig()
        {
            return new SecretSantaConfig
            {
                People = new List<SecretSantaConfig.Person>
                {
                    new("Alice", "alice@example.com"),
                    new("Bob", "bob@example.com"),
                    new("Carol", "carol@example.com"),
                    new("Dave", "dave@example.com")
                },
                Restrictions = new List<SecretSantaConfig.Restriction>
                {
                    new("alice@example.com", "bob@example.com")
                }
            };
        }

        // A valid running event: alice->carol, bob->dave, carol->alice, dave->bob
        // (alice-bob restriction satisfied: alice gives to carol, not bob)
        private static SecretSantaEvent MakeValidRunningEvent()
        {
            return new SecretSantaEvent
            {
                EventId = "event-2024",
                IsRunning = true,
                GroupName = "Family",
                Year = 2024,
                Participants = new List<SecretSantaEvent.Participant>
                {
                    new("Alice", "alice@example.com", "Carol", "carol@example.com"),
                    new("Bob", "bob@example.com", "Dave", "dave@example.com"),
                    new("Carol", "carol@example.com", "Alice", "alice@example.com"),
                    new("Dave", "dave@example.com", "Bob", "bob@example.com")
                }
            };
        }

        [Fact]
        public void Validate_PassesForCompleteValidAssignment()
        {
            var evt = MakeValidRunningEvent();
            var config = MakeConfig();
            // Should not throw
            evt.Validate(config);
        }

        [Fact]
        public void Validate_PassesForNotRunningEvent()
        {
            var evt = new SecretSantaEvent
            {
                EventId = "event-2024",
                IsRunning = false,
                GroupName = "Family",
                Year = 2024,
                Participants = new List<SecretSantaEvent.Participant>
                {
                    new("Alice", "alice@example.com", null, null),
                    new("Bob", "bob@example.com", null, null)
                }
            };
            var config = MakeConfig();
            evt.Validate(config); // should not throw
        }

        [Fact]
        public void Validate_FailsWhenEventIdMissing()
        {
            var evt = new SecretSantaEvent
            {
                EventId = "",
                IsRunning = true,
                GroupName = "Family",
                Year = 2024,
                Participants = new List<SecretSantaEvent.Participant>
                {
                    new("Alice", "alice@example.com", "Carol", "carol@example.com"),
                    new("Bob", "bob@example.com", "Dave", "dave@example.com"),
                    new("Carol", "carol@example.com", "Alice", "alice@example.com"),
                    new("Dave", "dave@example.com", "Bob", "bob@example.com")
                }
            };
            var config = MakeConfig();
            Assert.Throws<InvalidDataException>(() => evt.Validate(config));
        }

        [Fact]
        public void Validate_FailsWhenSomeoneIsTheirOwnSanta()
        {
            var config = MakeConfig();
            var evt = new SecretSantaEvent
            {
                EventId = "event-2024",
                IsRunning = true,
                GroupName = "Family",
                Year = 2024,
                Participants = new List<SecretSantaEvent.Participant>
                {
                    new("Alice", "alice@example.com", "Alice", "alice@example.com"),
                    new("Bob", "bob@example.com", "Carol", "carol@example.com"),
                    new("Carol", "carol@example.com", "Dave", "dave@example.com"),
                    new("Dave", "dave@example.com", "Bob", "bob@example.com")
                }
            };
            Assert.Throws<InvalidDataException>(() => evt.Validate(config));
        }

        [Fact]
        public void Validate_FailsWhenRestrictionViolated()
        {
            // alice->bob violates the restriction
            var config = MakeConfig();
            var evt = new SecretSantaEvent
            {
                EventId = "event-2024",
                IsRunning = true,
                GroupName = "Family",
                Year = 2024,
                Participants = new List<SecretSantaEvent.Participant>
                {
                    new("Alice", "alice@example.com", "Bob", "bob@example.com"),
                    new("Bob", "bob@example.com", "Dave", "dave@example.com"),
                    new("Carol", "carol@example.com", "Alice", "alice@example.com"),
                    new("Dave", "dave@example.com", "Carol", "carol@example.com")
                }
            };
            Assert.Throws<InvalidDataException>(() => evt.Validate(config));
        }

        [Fact]
        public void Validate_FailsWhenParticipantNotInConfig()
        {
            var config = MakeConfig();
            var evt = new SecretSantaEvent
            {
                EventId = "event-2024",
                IsRunning = false,
                GroupName = "Family",
                Year = 2024,
                Participants = new List<SecretSantaEvent.Participant>
                {
                    new("Stranger", "stranger@example.com", null, null)
                }
            };
            Assert.Throws<InvalidDataException>(() => evt.Validate(config));
        }

        [Fact]
        public void Validate_FailsWhenAssignmentIsNotACompleteCycle()
        {
            // Carol and Dave both give to Alice — Bob has no santa
            var config = MakeConfig();
            var evt = new SecretSantaEvent
            {
                EventId = "event-2024",
                IsRunning = true,
                GroupName = "Family",
                Year = 2024,
                Participants = new List<SecretSantaEvent.Participant>
                {
                    new("Alice", "alice@example.com", "Carol", "carol@example.com"),
                    new("Bob", "bob@example.com", "Dave", "dave@example.com"),
                    // Carol gives to Alice (second person giving to Alice)
                    new("Carol", "carol@example.com", "Alice", "alice@example.com"),
                    new("Dave", "dave@example.com", "Alice", "alice@example.com")
                }
            };
            // Bob has no santa (nobody's SantaForEmail == bob@example.com)
            Assert.Throws<InvalidDataException>(() => evt.Validate(config));
        }
    }
}
