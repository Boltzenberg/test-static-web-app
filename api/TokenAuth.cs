using Boltzenberg.Functions.DataModels.Auth;
using Boltzenberg.Functions.Logging;
using Boltzenberg.Functions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class TokenAuth
{
    public TokenAuth()
    {
    }

    [Function("SignIn")]
    public async Task<IActionResult> SignInUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("TokenAuth.SignIn", req, SignIn);
    public async Task<IActionResult> SignIn(HttpRequest req, LogBuffer log)
    {
        if (!AuthZChecker.IsAuthorizedForSignIn(req))
        {
            log.Error("Authorization failed for sign in");
            return new UnauthorizedResult();
        }

        OperationResult<RefreshableToken> result = await JsonStore.Upsert<RefreshableToken>(RefreshableToken.Generate());
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            return new OkObjectResult(result.Entity.Token);
        }
        
        log.Error("Failed to generate the refreshable token");
        return new BadRequestObjectResult("Failed to generate the refreshable token");
    }

    [Function("Refresh")]
    public async Task<IActionResult> RefreshUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("TokenAuth.Refresh", req, Refresh);
    public async Task<IActionResult> Refresh(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForRefreshToken(req))
        {
            log.Error("Authorization failed for refresh");
            return new UnauthorizedResult();
        }

        OperationResult<RefreshableToken> result = await JsonStore.Upsert<RefreshableToken>(RefreshableToken.Generate());
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            return new OkObjectResult(result.Entity.Token);
        }
        
        log.Error("Failed to generate the refreshable token");
        return new BadRequestObjectResult("Failed to generate the refreshable token");
    }

    [Function("SignOut")]
    public async Task<IActionResult> SignOut([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("TokenAuth.SignOut", req, SignOut);
    public async Task<IActionResult> SignOut(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForRefreshToken(req))
        {
            log.Error("AUthorization failed for signout");
            return new UnauthorizedResult();
        }

        OperationResult<RefreshableToken> result = await JsonStore.DeleteUnconditionally<RefreshableToken>(RefreshableToken.RefreshableTokenAppId, RefreshableToken.RefreshableTokenId);
        if (result.Code == ResultCode.Success)
        {
            return new OkResult();
        }
        
        log.Error("Failed to sign out");
        return new BadRequestObjectResult("Failed to sign out");
    }
}