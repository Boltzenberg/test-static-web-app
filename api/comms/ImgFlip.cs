using System.Text.Json;
using Boltzenberg.Functions.Logging;

namespace Boltzenberg.Functions.Comms
{
    public static class ImgFlip
    {
        private static readonly HttpClient _http = new HttpClient();
        private static readonly string? USERNAME = Environment.GetEnvironmentVariable("IMGFLIP_USERNAME");
        private static readonly string? PASSWORD = Environment.GetEnvironmentVariable("IMGFLIP_PASSWORD");
        private static readonly string CHANDLER_MEME_TEMPLATE_ID = "119759877";

        public static async Task<string> ChandlerizeUrlAsync(string top, string bottom, LogBuffer log)
        {
            if (string.IsNullOrWhiteSpace(USERNAME) || string.IsNullOrWhiteSpace(PASSWORD))
            {
                throw new InvalidOperationException("ImgFlip credentials are not configured");
            }

            if (string.IsNullOrWhiteSpace(top) && string.IsNullOrEmpty(bottom))
            {
                throw new ArgumentException("Chandlerize - top and bottom can't both be empty string");
            }

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["template_id"] = CHANDLER_MEME_TEMPLATE_ID,
                ["username"] = USERNAME,
                ["password"] = PASSWORD,
                ["text0"] = top ?? "",
                ["text1"] = bottom ?? ""
            });

            log.Info("Generating Chandler Meme with top text '{0}' and bottom text '{1}'", top ?? "", bottom ?? "");
            var apiResponse = await _http.PostAsync("https://api.imgflip.com/caption_image", form);
            var jsonString = await apiResponse.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<ImgflipResponse>(jsonString);

            if (json == null || !json.success || json.data == null || json.data.url == null)
            {
                throw new InvalidOperationException(json?.error_message ?? "ImgFlip API error");
            }

            // Fetch the generated image
            return json.data.url;
        }

        public static async Task<byte[]> ChandlerizeImageAsync(string top, string bottom, LogBuffer log)
        {
            var imageUrl = await ChandlerizeUrlAsync(top, bottom, log);
            var imageResponse = await _http.GetAsync(imageUrl);

            if (!imageResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(string.Format("Failed to fetch the generated image from '{0}'", imageUrl));
            }

            return await imageResponse.Content.ReadAsByteArrayAsync();
        }

        private class ImgflipResponse
        {
            public bool success { get; set; }
            public ImgflipData? data { get; set; }
            public string? error_message { get; set; }
        }

        private class ImgflipData
        {
            public string? url { get; set; }
            public string? page_url { get; set; }
        }
    }
}