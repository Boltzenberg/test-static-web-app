using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Boltzenberg.Functions.Logging;
using Boltzenberg.Functions.Comms;

namespace Boltzenberg.Functions
{
    public class ChandlerMemeGen
    {
        public ChandlerMemeGen()
        {
        }

        [Function("Chandler")]
        public async Task<IActionResult> ChandlerUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
            await LogBuffer.Wrap("Chandler", req, Chandler);
        private async Task<IActionResult> Chandler(HttpRequest req, LogBuffer log)
        {
            string top = req.Query["text"].ToString() ?? "";
            string bottom = req.Query["bottom"].ToString() ?? "";

            var bytes = await ImgFlip.ChandlerizeImageAsync(top, bottom, log);

            return new FileContentResult(bytes, "image/jpeg");        
        }
    }
}
