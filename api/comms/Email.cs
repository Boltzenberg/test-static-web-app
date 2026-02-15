using System.Text;

namespace Boltzenberg.Functions.Comms
{
    public class Email
    {
        private const string FromAddress = "santamail@boltzenberg.com";
        private const string SecretSantaFromName = "Secret Santa";
        private const string ContactFromName = "Website Contact Form";
        private const string MailJetUrl = "https://api.mailjet.com/v3.1/send";
        private static string MailJetAPIKey = Environment.GetEnvironmentVariable("MAILJET_API_KEY") ?? string.Empty;
        private static string MailJetSecretKey = Environment.GetEnvironmentVariable("MAILJET_SECRET_KEY") ?? string.Empty;
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<bool> SendWeeklyMailAsync()
        {
            return await SendAsync(
                ContactFromName,
                [new Tuple<string, string>("Jon Rosenberg", "jon.p.rosenberg@gmail.com")],
                "Mail Check",
                "Mail is still working!"
                );
        }

        public static async Task<bool> SendContactFormMailAsync(string toName, string toAddress, string body)
        {
            return await SendAsync(
                ContactFromName,
                [new Tuple<string, string>(toName, toAddress)],
                "Message from your Website",
                body
            );
        }

        public static async Task<bool> SendSantaMailAsync(string toName, string toAddress, string subject, string body)
        {
            return await SendAsync(
                SecretSantaFromName, 
                [new Tuple<string, string>(toName, toAddress)],
                subject,
                body);
        }

        public static async Task<bool> SendSantaMailAsync(IEnumerable<Tuple<string, string>> toAddresses, string subject, string body)
        {
            return await SendAsync(
                SecretSantaFromName,
                toAddresses,
                subject,
                body);
        }

        private static async Task<bool> SendAsync(string fromName, IEnumerable<Tuple<string, string>> toAddresses, string subject, string body)
        {
            // Convert tuples → MailJet "To" objects 
            var toList = toAddresses.Select(t => new 
            { 
                Email = t.Item2, 
                Name = t.Item1 
            }).ToArray();

            // Build the request payload using anonymous objects
            var payload = new
            {
                Messages = new[]
                {
                    new
                    {
                        From = new { Email = FromAddress, Name = fromName },
                        To = toList,
                        Subject = subject,
                        HTMLPart = body.Replace(Environment.NewLine, "<br>")
                    }
                }
            };

            // Serialize to JSON
            string json = System.Text.Json.JsonSerializer.Serialize(payload);

            // Build the request
            var request = new HttpRequestMessage(HttpMethod.Post, MailJetUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Add Basic Auth header
            string auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{MailJetAPIKey}:{MailJetSecretKey}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

            // Send
            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
    }
}