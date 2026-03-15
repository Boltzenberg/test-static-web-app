using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Boltzenberg.Functions.Comms
{
    public static class TelegramLogger
    {
        private static readonly HttpClient _http = new HttpClient();

        private static readonly string? Token = Environment.GetEnvironmentVariable("BOLTZENBERG_BOT_TOKEN");

        private static readonly string? ChatId = "-1003401427386";

        public enum Level
        {
            Info,
            Warn,
            Error
        }

        public static async Task LogAsync(Level level, string message, object? meta = null)
        {
            if (string.IsNullOrWhiteSpace(Token) || string.IsNullOrWhiteSpace(ChatId))
            {
                Console.WriteLine("Telegram logging skipped: missing TELEGRAM_TOKEN or TELEGRAM_CHANNEL_ID");
                return;
            }

            var timestamp = DateTime.UtcNow.ToString("o");

            var sb = new StringBuilder();
            sb.AppendLine($"📝 *{level}* — `{timestamp}`");
            sb.AppendLine(message);

            if (meta != null)
            {
                string json = JsonSerializer.Serialize(
                    meta,
                    new JsonSerializerOptions { WriteIndented = true }
                );

                sb.AppendLine("```json");
                sb.AppendLine(json);
                sb.AppendLine("```");
            }

            var payload = new
            {
                chat_id = ChatId,
                text = sb.ToString(),
                parse_mode = "Markdown"
            };

            var url = $"https://api.telegram.org/bot{Token}/sendMessage";

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                using var response = await _http.PostAsync(url, content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                // Avoid recursive logging loops
                Console.WriteLine($"Telegram logging failed: {ex.Message}");
            }
        }

        public static Task InfoAsync(string message, object? meta = null)
            => LogAsync(Level.Info, message, meta);

        public static Task WarnAsync(string message, object? meta = null)
            => LogAsync(Level.Warn, message, meta);

        public static Task ErrorAsync(string message, object? meta = null)
            => LogAsync(Level.Error, message, meta);
    }
}