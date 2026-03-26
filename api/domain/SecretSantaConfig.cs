namespace Boltzenberg.Functions.Domain
{
    public class SecretSantaConfig
    {
        public List<Person> People { get; init; } = new();
        public List<Restriction> Restrictions { get; init; } = new();

        public void Validate()
        {
            foreach (var restriction in Restrictions)
            {
                if (People.Find(p => p.Email == restriction.Person1Email) == null)
                {
                    throw new InvalidDataException(
                        "A restriction references email address '" + restriction.Person1Email +
                        "' as Person1Email but no person has that email address!");
                }

                if (People.Find(p => p.Email == restriction.Person2Email) == null)
                {
                    throw new InvalidDataException(
                        "A restriction references email address '" + restriction.Person2Email +
                        "' as Person2Email but no person has that email address!");
                }
            }
        }

        public record Person(string Name, string Email);
        public record Restriction(string Person1Email, string Person2Email);
    }
}
