using Boltzenberg.Functions.Domain;
using Boltzenberg.Functions.Domain.Algorithms;
using Xunit;

namespace ApiTests.Algorithms
{
    public class AssignTests
    {
        private static SecretSantaConfig MakeConfig(params (string name, string email)[] people)
        {
            return new SecretSantaConfig
            {
                People = people.Select(p => new SecretSantaConfig.Person(p.name, p.email)).ToList(),
                Restrictions = new List<SecretSantaConfig.Restriction>()
            };
        }

        private static SecretSantaEvent MakeEvent(SecretSantaConfig config, string groupName = "Test", int year = 2024)
        {
            return new SecretSantaEvent
            {
                EventId = $"{groupName}-{year}",
                IsRunning = false,
                GroupName = groupName,
                Year = year,
                Participants = config.People
                    .Select(p => new SecretSantaEvent.Participant(p.Name, p.Email, null, null))
                    .ToList()
            };
        }

        [Fact]
        public void Assign_NobodyIsTheirOwnSanta()
        {
            var config = MakeConfig(
                ("Alice", "alice@example.com"),
                ("Bob", "bob@example.com"),
                ("Carol", "carol@example.com")
            );
            var evt = MakeEvent(config);

            bool result = SecretSantaAssign.AssignSantas(evt, new List<SecretSantaEvent>(), config);

            Assert.True(result);
            foreach (var p in evt.Participants)
            {
                Assert.NotEqual(p.Email, p.SantaForEmail);
            }
        }

        [Fact]
        public void Assign_AllParticipantsReceiveASanta()
        {
            var config = MakeConfig(
                ("Alice", "alice@example.com"),
                ("Bob", "bob@example.com"),
                ("Carol", "carol@example.com"),
                ("Dave", "dave@example.com")
            );
            var evt = MakeEvent(config);

            bool result = SecretSantaAssign.AssignSantas(evt, new List<SecretSantaEvent>(), config);

            Assert.True(result);
            var receivers = evt.Participants.Select(p => p.SantaForEmail!).ToHashSet();
            var participants = evt.Participants.Select(p => p.Email).ToHashSet();
            Assert.Equal(participants, receivers);
        }

        [Fact]
        public void Assign_RespectsRestrictions()
        {
            var config = new SecretSantaConfig
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
            var evt = MakeEvent(config);

            // Run many times to be sure the restriction is consistently respected
            for (int i = 0; i < 10; i++)
            {
                var testEvt = MakeEvent(config);
                bool ok = SecretSantaAssign.AssignSantas(testEvt, new List<SecretSantaEvent>(), config);
                Assert.True(ok);

                var alice = testEvt.Participants.First(p => p.Email == "alice@example.com");
                Assert.NotEqual("bob@example.com", alice.SantaForEmail);

                var bob = testEvt.Participants.First(p => p.Email == "bob@example.com");
                Assert.NotEqual("alice@example.com", bob.SantaForEmail);
            }
        }

        [Fact]
        public void Assign_ProducesValidAssignment()
        {
            var config = MakeConfig(
                ("Alice", "alice@example.com"),
                ("Bob", "bob@example.com"),
                ("Carol", "carol@example.com"),
                ("Dave", "dave@example.com")
            );
            var evt = MakeEvent(config);

            bool result = SecretSantaAssign.AssignSantas(evt, new List<SecretSantaEvent>(), config);

            Assert.True(result);
            Assert.All(evt.Participants, p =>
            {
                Assert.NotNull(p.SantaForEmail);
                Assert.NotNull(p.SantaForName);
            });
        }

        [Fact]
        public void Assign_ProducesACompleteCycle()
        {
            var config = MakeConfig(
                ("Alice", "alice@example.com"),
                ("Bob", "bob@example.com"),
                ("Carol", "carol@example.com")
            );
            var evt = MakeEvent(config);

            SecretSantaAssign.AssignSantas(evt, new List<SecretSantaEvent>(), config);

            // Every participant should be giving to exactly one unique person
            var givers = evt.Participants.Select(p => p.Email).ToList();
            var receivers = evt.Participants.Select(p => p.SantaForEmail!).ToList();
            Assert.Equal(givers.Count, receivers.Distinct().Count());
        }

        [Fact]
        public void Assign_ReturnsFalseWhenAlreadyRunning()
        {
            var config = MakeConfig(
                ("Alice", "alice@example.com"),
                ("Bob", "bob@example.com")
            );
            // Create an event that is already running
            var evt = new SecretSantaEvent
            {
                EventId = "event-1",
                IsRunning = true,
                GroupName = "Test",
                Year = 2024,
                Participants = new List<SecretSantaEvent.Participant>
                {
                    new("Alice", "alice@example.com", "Bob", "bob@example.com"),
                    new("Bob", "bob@example.com", "Alice", "alice@example.com")
                }
            };

            bool result = SecretSantaAssign.AssignSantas(evt, new List<SecretSantaEvent>(), config);
            Assert.False(result);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void Assign_WorksForVariousGroupSizes(int participantCount)
        {
            var people = Enumerable.Range(1, participantCount)
                .Select(i => ($"Person{i}", $"person{i}@example.com"))
                .ToArray();

            var config = MakeConfig(people);
            var evt = MakeEvent(config);

            bool result = SecretSantaAssign.AssignSantas(evt, new List<SecretSantaEvent>(), config);

            Assert.True(result);
            Assert.Equal(participantCount, evt.Participants.Count);
            foreach (var p in evt.Participants)
            {
                Assert.NotNull(p.SantaForEmail);
                Assert.NotEqual(p.Email, p.SantaForEmail);
            }
        }

        [Fact]
        public void Assign_FailsWhenNoValidAssignmentExists()
        {
            // Two people with a restriction between them — impossible to assign
            var config = new SecretSantaConfig
            {
                People = new List<SecretSantaConfig.Person>
                {
                    new("Alice", "alice@example.com"),
                    new("Bob", "bob@example.com")
                },
                Restrictions = new List<SecretSantaConfig.Restriction>
                {
                    new("alice@example.com", "bob@example.com")
                }
            };
            var evt = MakeEvent(config);

            bool result = SecretSantaAssign.AssignSantas(evt, new List<SecretSantaEvent>(), config);
            Assert.False(result);
        }
    }
}
