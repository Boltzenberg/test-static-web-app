namespace Boltzenberg.Functions.DataModels.Auth
{
    public class AuthLog : CosmosDocument
    {
        public static string AuthLogAppId = "AuthLog";

        public string Line { get; set; }

        public AuthLog(string line)
            : base(AuthLogAppId)
        {
            this.id = Guid.NewGuid().ToString();
            this.Line = string.Format("{0}: {1}", DateTime.Now, line);
        }
    }
}
