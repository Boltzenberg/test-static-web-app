namespace Boltzenberg.Functions.Domain
{
    public class SecretSantaEvent
    {
        public string EventId { get; init; } = string.Empty;
        public bool IsRunning { get; init; }
        public string GroupName { get; init; } = string.Empty;
        public int Year { get; init; }
        public List<Participant> Participants { get; init; } = new();

        public void Validate(SecretSantaConfig config)
        {
            if (string.IsNullOrEmpty(EventId))
            {
                throw new InvalidDataException("Event id must be set");
            }

            foreach (var participant in Participants)
            {
                if (config.People.Find(p => p.Email == participant.Email) == null)
                {
                    throw new InvalidDataException(
                        "Event references participant '" + participant.Email +
                        "' but that email address isn't in the config!");
                }

                if (IsRunning)
                {
                    if (config.People.Find(p => p.Email == participant.SantaForEmail) == null)
                    {
                        throw new InvalidDataException(
                            "Event participant '" + participant.Email + "' is Santa for '" +
                            participant.SantaForEmail + "' but that Santa for address isn't in the config!");
                    }

                    if (config.Restrictions.Find(r =>
                        (r.Person1Email == participant.Email && r.Person2Email == participant.SantaForEmail) ||
                        (r.Person1Email == participant.SantaForEmail && r.Person2Email == participant.Email)) != null)
                    {
                        throw new InvalidDataException(
                            "Event assigns '" + participant.Email + "' as santa for '" +
                            participant.SantaForEmail + "' but that violates the restrictions!");
                    }

                    if (Participants.Find(p => p.SantaForEmail == participant.Email) == null)
                    {
                        throw new InvalidDataException(
                            "Event participant '" + participant.Email +
                            "' doesn't have anybody assigned as their santa!");
                    }

                    if (participant.Email == participant.SantaForEmail)
                    {
                        throw new InvalidDataException(
                            "Event participant '" + participant.Email + "' is their own Santa!");
                    }
                }
            }
        }

        public record Participant(
            string Name,
            string Email,
            string? SantaForName,
            string? SantaForEmail
        );
    }
}
