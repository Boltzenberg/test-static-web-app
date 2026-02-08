using System.Net;
using System.Text.Json;
using Boltzenberg.Functions.DataModels.SecretSanta;
using Boltzenberg.Functions.DataModels.Auth;
using Boltzenberg.Functions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class SecretSanta
{
    private readonly ILogger<SecretSanta> _logger;

    public SecretSanta(ILogger<SecretSanta> logger)
    {
        _logger = logger;
    }

    [Function("SecretSantaAdminGetConfig")]
    public async Task<IActionResult> SecretSantaAdminGetConfig([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForSecretSantaAdmin())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }

            var config = await JsonStore.Read<SecretSantaConfig>(SecretSantaConfig.SecretSantaAppId, SecretSantaConfig.SecretSantaConfigId);
            if (config.Code == ResultCode.Success)
            {
                return new OkObjectResult(JsonSerializer.Serialize(config.Entity));
            }
            else
            {
                return new BadRequestResult();
            }
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("SecretSantaAdminUpdateConfig")]
    public async Task<IActionResult> SecretSantaAdminUpdateConfig([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForSecretSantaAdmin())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }

            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Request Body: " + body);
                SecretSantaConfig? entry = JsonSerializer.Deserialize<SecretSantaConfig>(body);
                if (entry == null)
                {
                    return new BadRequestResult();
                }

                // Throws if the entry is invalid
                entry.Validate();

                var result = await JsonStore.Update(entry);
                if (result.Code == ResultCode.Success && result.Entity != null)
                {
                    return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
                }
                else if (result.Code == ResultCode.PreconditionFailed)
                {
                    return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
                }
            }

            return new BadRequestResult();
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(ex.ToString());
        }
    }

    [Function("SecretSantaAdminCreateEvent")]
    public async Task<IActionResult> SecretSantaAdminCreateEvent([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForSecretSantaAdmin())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }

            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Request Body: " + body);
                SecretSantaEvent? evt = JsonSerializer.Deserialize<SecretSantaEvent>(body);
                if (evt == null)
                {
                    return new BadRequestObjectResult("Failed to deserialize the event");
                }

                var config = await JsonStore.Read<SecretSantaConfig>(SecretSantaConfig.SecretSantaAppId, SecretSantaConfig.SecretSantaConfigId);
                if (config.Code != ResultCode.Success || config.Entity == null)
                {
                    return new BadRequestResult();
                }

                // Throws if the entry is invalid
                evt.Validate(config.Entity);

                var result = await JsonStore.Create(evt);
                if (result.Code == ResultCode.Success && result.Entity != null)
                {
                    return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
                }
                else if (result.Code == ResultCode.PreconditionFailed)
                {
                    return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
                }
            }

            return new BadRequestResult();
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(ex.ToString());
        }
    }

    [Function("SecretSantaAdminUpdateEvent")]
    public async Task<IActionResult> SecretSantaAdminUpdateEvent([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            ClientPrincipal? principal = ClientPrincipal.FromReq(req);
            if (principal == null || !principal.IsAuthorizedForSecretSantaAdmin())
            {
                return new UnauthorizedObjectResult("No auth header found");
            }

            string body = await new StreamReader(req.Body).ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
            {
                _logger.LogInformation("Request Body: " + body);
                SecretSantaEvent? evt = JsonSerializer.Deserialize<SecretSantaEvent>(body);
                if (evt == null)
                {
                    return new BadRequestResult();
                }

                var config = await JsonStore.Read<SecretSantaConfig>(SecretSantaConfig.SecretSantaAppId, SecretSantaConfig.SecretSantaConfigId);
                if (config.Code != ResultCode.Success || config.Entity == null)
                {
                    return new BadRequestResult();
                }

                // Throws if the entry is invalid
                evt.Validate(config.Entity);

                var result = await JsonStore.Update(evt);
                if (result.Code == ResultCode.Success && result.Entity != null)
                {
                    return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
                }
                else if (result.Code == ResultCode.PreconditionFailed)
                {
                    return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
                }
            }

            return new BadRequestResult();
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(ex.ToString());
        }
    }

    [Function("SecretSantaSendSantaMail")]
    public async Task<IActionResult> SecretSantaSendSantaMail([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            string? entryId = req.Query["id"];
            if (string.IsNullOrEmpty(entryId))
            {
                return new BadRequestObjectResult("Missing the id query parameter");
            }

            return new BadRequestResult();
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(ex.ToString());
        }
    }

    [Function("SecretSantaGetEvent")]
    public async Task<IActionResult> SecretSantaGetEvent([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            string? evtId = req.Query["id"];
            if (string.IsNullOrEmpty(evtId))
            {
                return new BadRequestObjectResult("Missing the id query parameter");
            }

            var evt = await JsonStore.Read<SecretSantaEvent>(SecretSantaEvent.SecretSantaEventAppId, evtId);
            if (evt.Code == ResultCode.Success)
            {
                return new OkObjectResult(JsonSerializer.Serialize(evt.Entity));
            }
            else
            {
                return new BadRequestResult();
            }
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("SecretSantaGetAllEvents")]
    public async Task<IActionResult> SecretSantaGetAllEvents([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            var config = await JsonStore.ReadAll<SecretSantaEvent>(SecretSantaEvent.SecretSantaEventAppId);
            if (config.Code == ResultCode.Success)
            {
                return new OkObjectResult(JsonSerializer.Serialize(config.Entity));
            }
            else
            {
                return new BadRequestResult();
            }
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("SecretSantaAdminGetCannedConfig")]
    public async Task<IActionResult> SecretSantaAdminGetCannedConfig([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            return new OkObjectResult(JsonSerializer.Serialize(SecretSantaConfig.GetCanned()));
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }

    [Function("SecretSantaAdminGetCannedEvent")]
    public async Task<IActionResult> SecretSantaAdminGetCannedEvent([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            return new OkObjectResult(JsonSerializer.Serialize(SecretSantaEvent.GetCanned()));
        }
        catch (Exception ex)
        {
            return new OkObjectResult(ex.ToString());
        }
    }
}