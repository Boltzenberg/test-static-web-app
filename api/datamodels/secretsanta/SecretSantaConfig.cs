namespace Boltzenberg.Functions.DataModels.SecretSanta
{
    public class SecretSantaConfig : CosmosDocument
    {
        public static string SecretSantaAppId = "SecretSanta";
        public static string SecretSantaConfigId = "SecretSantaConfig";

        public List<SecretSantaPerson> People { get; set; } = new();

        public List<SecretSantaRestriction> Restrictions { get; set; } = new();

        public SecretSantaConfig()
            : base(SecretSantaAppId)
        {
            this.id = SecretSantaConfigId;
        }

        public void Validate()
        {
            // All restrictions reference people
            foreach (var restriction in this.Restrictions)
            {
                if (this.People.Find(p => p.Email == restriction.Person1Email) == null)
                {
                    throw new InvalidDataException("A restriction references email address '" + restriction.Person1Email + "' as Person1Email but no person has that email address!");
                }

                if (this.People.Find(p => p.Email == restriction.Person2Email) == null)
                {
                    throw new InvalidDataException("A restriction references email address '" + restriction.Person2Email + "' as Person2Email but no person has that email address!");
                }
            }
        }

        public static SecretSantaConfig GetCanned()
        {
            SecretSantaConfig config = new SecretSantaConfig();
            config.People.Add(new SecretSantaPerson() { Name = "Test1", Email = "test.person.1@gmail.com" });
            config.People.Add(new SecretSantaPerson() { Name = "Test2", Email = "test.person.2@gmail.com" });
            config.People.Add(new SecretSantaPerson() { Name = "Test3", Email = "test.person.3@gmail.com" });
            config.People.Add(new SecretSantaPerson() { Name = "Test4", Email = "test.person.4@gmail.com" });
            config.Restrictions.Add(new SecretSantaRestriction() { Person1Email = "test.person.1@gmail.com", Person2Email = "test.person.2@gmail.com" });
            config.Restrictions.Add(new SecretSantaRestriction() { Person1Email = "test.person.3@gmail.com", Person2Email = "test.person.4@gmail.com" });
            return config;
        }
    }

    public class SecretSantaPerson
    {
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
    }

    public class SecretSantaRestriction
    {
        public string Person1Email { get; set; } = default!;
        public string Person2Email { get; set; } = default!;
    }
}
