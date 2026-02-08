namespace Boltzenberg.Functions.DataModels.SecretSanta
{
    public class SecretSantaEvent : CosmosDocument
    {
        public static string SecretSantaEventAppId = "SecretSantaEvent";

        public bool IsRunning { get; set; }

        public string GroupName { get; set; } = default!;

        public int Year { get; set; } = default!;

        public List<SecretSantaParticipant> Participants { get; set; } = new();

        public SecretSantaEvent()
            : base(SecretSantaEventAppId)
        {}

        public void Validate(SecretSantaConfig config)
        {
            if (string.IsNullOrEmpty(this.id))
            {
                throw new InvalidDataException("Event id must be set");    
            }

            foreach (var participant in this.Participants)
            {
                // All Participants are people in the config
                if (config.People.Find(p => p.Email == participant.Email) == null)
                {
                    throw new InvalidDataException("Event references participant '" + participant.Email + "' but that email address isn't in the config!");
                }

                if (this.IsRunning)
                {
                    if (config.People.Find(p => p.Email == participant.SantaForEmail) == null)
                    {
                        throw new InvalidDataException("Event participant '" + participant.Email + "' is Santa for '" + participant.SantaForEmail + "' but that Santa for address isn't in the config!");
                    }

                    // Assignments don't validate restrictions
                    if (config.Restrictions.Find(r => (r.Person1Email == participant.Email && r.Person2Email == participant.SantaForEmail) ||
                                                    (r.Person1Email == participant.SantaForEmail && r.Person2Email == participant.Email)) != null)
                    {
                        throw new InvalidDataException("Event assigns '" + participant.Email + "' as santa for '" + participant.SantaForEmail + "' but that violates the restrictions!");
                    }

                    // All participants are someone else's assignment
                    if (this.Participants.Find(p => p.SantaForEmail == participant.Email) == null)
                    {
                        throw new InvalidDataException("Event participant '" + participant.Email + "' doesn't have anybody assigned as their santa!");
                    }

                    // You can't be your own santa
                    if (participant.Email == participant.SantaForEmail)
                    {
                        throw new InvalidDataException("Event participant '" + participant.Email + "' is their own Santa!");
                    }
                }
            }
        }

        public static SecretSantaEvent GetCanned()
        {
            SecretSantaEvent evt = new SecretSantaEvent();
            evt.id = "Secret Santa Canned Event";
            evt.IsRunning = false;
            evt.Participants.Add(new SecretSantaParticipant() { Email = "test.person.1@gmail.com", SantaForEmail = "test.person.3@gmail.com" });
            evt.Participants.Add(new SecretSantaParticipant() { Email = "test.person.2@gmail.com", SantaForEmail = "test.person.4@gmail.com" });
            evt.Participants.Add(new SecretSantaParticipant() { Email = "test.person.3@gmail.com", SantaForEmail = "test.person.2@gmail.com" });
            evt.Participants.Add(new SecretSantaParticipant() { Email = "test.person.4@gmail.com", SantaForEmail = "test.person.1@gmail.com" });
            return evt;
        }
    }

    public class SecretSantaParticipant
    {
        public string Email { get; set; } = default!;
        public string SantaForEmail { get; set; } = default!;
    }
}