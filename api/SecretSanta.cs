using System.Net;
using System.Text.Json;
using Boltzenberg.Functions.DataModels.SecretSanta;
using Boltzenberg.Functions.DataModels.Auth;
using Boltzenberg.Functions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Boltzenberg.Functions.Algorithms.SecretSanta;
using Boltzenberg.Functions.Comms;
using Boltzenberg.Functions.Logging;
using System.Threading.Tasks;

namespace Boltzenberg.Functions;

public class SecretSanta
{
    public SecretSanta(ILogger<SecretSanta> logger)
    {
    }

    [Function("SecretSantaAdminGetConfig")]
    public async Task<IActionResult> SecretSantaAdminGetConfigUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminGetConfig", req, SecretSantaAdminGetConfig);
    public async Task<IActionResult> SecretSantaAdminGetConfig(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        var config = await JsonStore.Read<SecretSantaConfig>(SecretSantaConfig.SecretSantaAppId, SecretSantaConfig.SecretSantaConfigId);
        if (config.Code == ResultCode.Success)
        {
            return new OkObjectResult(JsonSerializer.Serialize(config.Entity));
        }
        else
        {
            log.OperationResult("Failed to get the secret santa config", config);
            return new BadRequestResult();
        }
    }

    [Function("SecretSantaAdminUpdateConfig")]
    public async Task<IActionResult> SecretSantaAdminUpdateConfigUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminUpdateConfig", req, SecretSantaAdminUpdateConfig);
    public async Task<IActionResult> SecretSantaAdminUpdateConfig(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!string.IsNullOrEmpty(body))
        {
            SecretSantaConfig? entry = JsonSerializer.Deserialize<SecretSantaConfig>(body);
            if (entry == null)
            {
                log.Error("Failed to deserialize request body '{0}'", body);
                return new BadRequestResult();
            }

            // Throws if the entry is invalid
            entry.Validate();

            var result = await JsonStore.Update(entry);
            if (result.Code == ResultCode.Success && result.Entity != null)
            {
                return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
            }
            else
            {
                log.OperationResult("Failed to update the config", result);
                if (result.Code == ResultCode.PreconditionFailed)
                {
                    return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
                }
            }
        }
        else
        {
            log.Error("No request body!");        
        }

        return new BadRequestResult();
    }

    [Function("SecretSantaAdminCreateEvent")]
    public async Task<IActionResult> SecretSantaAdminCreateEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminCreateEvent", req, SecretSantaAdminCreateEvent);
    public async Task<IActionResult> SecretSantaAdminCreateEvent(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!string.IsNullOrEmpty(body))
        {
            SecretSantaEvent? evt = JsonSerializer.Deserialize<SecretSantaEvent>(body);
            if (evt == null)
            {
                log.Error("Failed to deserialize the request body '{0}'", body);
                return new BadRequestObjectResult("Failed to deserialize the event");
            }

            // Created events don't start in the running state
            evt.IsRunning = false;

            var config = await JsonStore.Read<SecretSantaConfig>(SecretSantaConfig.SecretSantaAppId, SecretSantaConfig.SecretSantaConfigId);
            if (config.Code != ResultCode.Success || config.Entity == null)
            {
                log.OperationResult("Failed to read the config", config);
                return new BadRequestResult();
            }

            // Throws if the entry is invalid
            evt.Validate(config.Entity);

            var result = await JsonStore.Create(evt);
            if (result.Code == ResultCode.Success && result.Entity != null)
            {
                log.Info("New Secret Santa Event created: {0} {1}", result.Entity.GroupName, result.Entity.Year);
                return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
            }
            else
            {
                log.OperationResult("Failed to create the event", result);
                if (result.Code == ResultCode.PreconditionFailed)
                {
                    return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
                }
            }
        }
        else
        {
            log.Error("No request body!");        
        }

        return new BadRequestResult();
    }

    [Function("SecretSantaAdminUpdateEvent")]
    public async Task<IActionResult> SecretSantaAdminUpdateEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminUpdateEvent", req, SecretSantaAdminUpdateEvent);
    public async Task<IActionResult> SecretSantaAdminUpdateEvent(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!string.IsNullOrEmpty(body))
        {
            SecretSantaEvent? evt = JsonSerializer.Deserialize<SecretSantaEvent>(body);
            if (evt == null)
            {
                log.Error("Failed to deserialize the request body '{0}'", body);
                return new BadRequestResult();
            }

            if (evt.IsRunning)
            {
                log.Error("Tried to update an event that is already running.  {0} {1}", evt.GroupName, evt.Year);
                return new BadRequestObjectResult("Can't update an event that is running!");
            }

            var config = await JsonStore.Read<SecretSantaConfig>(SecretSantaConfig.SecretSantaAppId, SecretSantaConfig.SecretSantaConfigId);
            if (config.Code != ResultCode.Success || config.Entity == null)
            {
                log.OperationResult("Failed to read the config", config);
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
                log.Error("OCC failure");
                return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
            }
            else
            {
                log.OperationResult("Failed to update the event", result);
            }
        }
        else
        {
            log.Error("No request body!");        
        }

        return new BadRequestResult();
    }

    [Function("SecretSantaAdminStartEvent")]
    public async Task<IActionResult> SecretSantaAdminStartEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminStartEvent", req, SecretSantaAdminStartEvent);
    public async Task<IActionResult> SecretSantaAdminStartEvent(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForSecretSantaAdmin(req))
        {
            log.Error("No auth header found");
            return new UnauthorizedObjectResult("No auth header found");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!string.IsNullOrEmpty(body))
        {
            SecretSantaEvent? evt = JsonSerializer.Deserialize<SecretSantaEvent>(body);
            if (evt == null)
            {
                log.Error("Failed to deserialize the request body '{0}'", body);
                return new BadRequestResult();
            }

            if (evt.IsRunning)
            {
                log.Error("Tried to start a running event!");
                return new BadRequestObjectResult("Can't start an event that is running!");
            }

            var config = await JsonStore.Read<SecretSantaConfig>(SecretSantaConfig.SecretSantaAppId, SecretSantaConfig.SecretSantaConfigId);
            if (config.Code != ResultCode.Success || config.Entity == null)
            {
                log.OperationResult("Failed to read the config", config);
                return new BadRequestResult();
            }

            var events = await JsonStore.ReadAll<SecretSantaEvent>(SecretSantaEvent.SecretSantaEventAppId);
            if (events.Code != ResultCode.Success || events.Entity == null)
            {
                log.OperationResult("Failed to read the event list", events);
                return new BadRequestResult();
            }

            // Set Assignments
            Assign.AssignSantas(evt, events.Entity, config.Entity);

            evt.IsRunning = true;

            // Throws if the entry is invalid
            evt.Validate(config.Entity);

            var result = await JsonStore.Update(evt);
            if (result.Code == ResultCode.Success && result.Entity != null)
            {
                return new OkObjectResult(JsonSerializer.Serialize(result.Entity));
            }
            else if (result.Code == ResultCode.PreconditionFailed)
            {
                log.Error("OCC failure");
                return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
            }
            else
            {
                log.OperationResult("Failed to update the event", result);
            }
        }
        else
        {
            log.Error("No request body!");
        }

        return new BadRequestResult();
    }

    [Function("SecretSantaEmailAssignment")]
    public async Task<IActionResult> SecretSantaEmailAssignmentUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaEmailAssignment", req, SecretSantaEmailAssignment);
    public async Task<IActionResult> SecretSantaEmailAssignment(HttpRequest req, LogBuffer log)
    {
        string? participantEmail = req.Query["participant"];
        if (string.IsNullOrEmpty(participantEmail))
        {
            log.Error("No participant query parameter");
            return new BadRequestObjectResult("Missing the participant query parameter");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (!string.IsNullOrEmpty(body))
        {
            SecretSantaEvent? evt = JsonSerializer.Deserialize<SecretSantaEvent>(body);
            if (evt == null)
            {
                log.Error("Failed to deserialize the request body '{0}'", body);
                return new BadRequestObjectResult("Failed to deserialize the event");
            }

            if (!evt.IsRunning)
            {
                log.Error("Event isn't running");
                return new BadRequestObjectResult("Can't email assignments for an event that is running!");
            }

            var participant = evt.Participants.Find(p => p.Email == participantEmail);
            if (participant == null)
            {
                log.Error("Failed to find {0} in the participant list for {1} {2}", participantEmail, evt.GroupName, evt.Year);
                return new BadRequestObjectResult("Participant is not part of the event!");
            }

            bool result = await Email.SendSantaMailAsync(
                participant.Name,
                participant.Email,
                "Secret Santa Assignment",
                string.Format("Hi {0}!  Your secret santa assignment for {1} is {2} ({3}).", participant.Name, evt.id, participant.SantaForName, participant.SantaForEmail));

            if (result)
            {
                return new OkResult();
            }
            else
            {
                log.Error("Failed to send the email");
                return new BadRequestObjectResult("Failed to send the email");
            }
        }

        log.Error("No request body!");
        return new BadRequestObjectResult("No request body!");
    }
    
    [Function("SecretSantaSendSantaMail")]
    public async Task<IActionResult> SecretSantaSendSantaMailUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaSendSantaMail", req, SecretSantaSendSantaMail);
    public async Task<IActionResult> SecretSantaSendSantaMail(HttpRequest req, LogBuffer log)
    {
        string? evtId = req.Query["id"];
        if (string.IsNullOrEmpty(evtId))
        {
            log.Error("Missing the id query parameter");
            return new BadRequestObjectResult("Missing the id query parameter");
        }

        string body = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrEmpty(body))
        {
            log.Error("Missing the email message in the post body");
            return new BadRequestObjectResult("Missing the email message in the POST body");
        }

        var evt = await JsonStore.Read<SecretSantaEvent>(SecretSantaEvent.SecretSantaEventAppId, evtId);
        if (evt.Code != ResultCode.Success || evt.Entity == null)
        {
            log.OperationResult("Failed to get the event with id " + evtId, evt);
            return new BadRequestObjectResult("Failed to find the event with id " + evtId);
        }

        List<Tuple<string, string>> toAddresses = new List<Tuple<string, string>>();
        foreach (var participant in evt.Entity.Participants)
        {
            toAddresses.Add(new Tuple<string, string>(participant.Name, participant.Email));
        }

        bool result = await Email.SendSantaMailAsync(
            toAddresses,
            string.Format("SantaMail for event '{0}'", evt.Entity.id),
            body);

        if (result)
        {
            return new OkResult();
        }
        else
        {
            log.Error("Failed to send the santamail");
            return new BadRequestObjectResult("Failed to send the mail");
        }
    }

    [Function("SecretSantaGetEvent")]
    public async Task<IActionResult> SecretSantaGetEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaGetEvent", req, SecretSantaGetEvent);
    public async Task<IActionResult> SecretSantaGetEvent(HttpRequest req, LogBuffer log)
    {
        string? evtId = req.Query["id"];
        if (string.IsNullOrEmpty(evtId))
        {
            log.Error("Missing the id query parameter");
            return new BadRequestObjectResult("Missing the id query parameter");
        }

        var evt = await JsonStore.Read<SecretSantaEvent>(SecretSantaEvent.SecretSantaEventAppId, evtId);
        if (evt.Code == ResultCode.Success)
        {
            return new OkObjectResult(JsonSerializer.Serialize(evt.Entity));
        }
        else
        {
            log.OperationResult("Failed to read the event", evt);
            return new BadRequestResult();
        }
    }

    [Function("SecretSantaGetAllEvents")]
    public async Task<IActionResult> SecretSantaGetAllEventsUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaGetAllEvents", req, SecretSantaGetAllEvents);
    public async Task<IActionResult> SecretSantaGetAllEvents(HttpRequest req, LogBuffer log)
    {
        var evts = await JsonStore.ReadAll<SecretSantaEvent>(SecretSantaEvent.SecretSantaEventAppId);
        if (evts.Code == ResultCode.Success)
        {
            return new OkObjectResult(JsonSerializer.Serialize(evts.Entity));
        }
        else
        {
            log.OperationResult("Failed to get the events", evts);
            return new BadRequestResult();
        }
    }

    [Function("SecretSantaAdminGetCannedConfig")]
    public async Task<IActionResult> SecretSantaAdminGetCannedConfigUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminGetCannedConfig", req, SecretSantaAdminGetCannedConfig);
    public async Task<IActionResult> SecretSantaAdminGetCannedConfig(HttpRequest req, LogBuffer log)
    {
        await Task.Yield();
        return new OkObjectResult(JsonSerializer.Serialize(SecretSantaConfig.GetCanned()));
    }

    [Function("SecretSantaAdminGetCannedEvent")]
    public async Task<IActionResult> SecretSantaAdminGetCannedEventUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("SecretSantaAdminGetCannedEvent", req, SecretSantaAdminGetCannedEvent);
    public async Task<IActionResult> SecretSantaAdminGetCannedEvent(HttpRequest req, LogBuffer log)
    {
        await Task.Yield();
        return new OkObjectResult(JsonSerializer.Serialize(SecretSantaEvent.GetCanned()));
    }
}