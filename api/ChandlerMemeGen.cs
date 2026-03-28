using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Boltzenberg.Functions.Storage;
using Boltzenberg.Functions.Storage.Documents;
using Boltzenberg.Functions.Domain;
using Boltzenberg.Functions.Dtos.GroceryList;
using Boltzenberg.Functions.Logging;

namespace Boltzenberg.Functions
{
    public class ChandlerMemeGen
    {
        private static readonly HttpClient _http = new HttpClient();

        public ChandlerMemeGen()
        {
        }

        [Function("Chandler")]
        public async Task<IActionResult> ChandlerUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
            await LogBuffer.Wrap("Chandler", req, Chandler);
        private async Task<IActionResult> Chandler(HttpRequest req, LogBuffer log)
        {
            string? top = req.Query["text"];
            string? bottom = req.Query["bottom"];

            if (string.IsNullOrWhiteSpace(top))
            {
                return new BadRequestObjectResult("Missing required query parameter: text");
            }

            const string templateId = "119759877"; // Chandler

            var username = Environment.GetEnvironmentVariable("IMGFLIP_USERNAME");
            var password = Environment.GetEnvironmentVariable("IMGFLIP_PASSWORD");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new ObjectResult("Imgflip credentials not configured.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["template_id"] = templateId,
                ["username"] = username,
                ["password"] = password,
                ["text0"] = top,
                ["text1"] = bottom ?? ""
            });

            log.Info("Generating Chandler Meme with top text '{0}' and bottom text '{1}'", top, bottom ?? "");
            var apiResponse = await _http.PostAsync("https://api.imgflip.com/caption_image", form);
            var jsonString = await apiResponse.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<ImgflipResponse>(jsonString);

            if (json == null || !json.success || json.data == null || json.data.url == null)
            {
                log.Error("Failed: {0}", json?.error_message ?? "No error message specified");
                return new BadRequestObjectResult(json?.error_message ?? "Imgflip API error");
            }

            // Fetch the generated image
            var imageUrl = json.data.url;
            var imageResponse = await _http.GetAsync(imageUrl);

            if (!imageResponse.IsSuccessStatusCode)
            {
                log.Error("Failed to fetch the generated image from '{0}'", imageUrl);
                return new ObjectResult("Failed to fetch generated image.")
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }

            var bytes = await imageResponse.Content.ReadAsByteArrayAsync();
            var contentType = imageResponse.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            return new FileContentResult(bytes, contentType);        
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
