using Boltzenberg.Functions.DataModels.Auth;
using Boltzenberg.Functions.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Boltzenberg.Functions;

public class TokenAuth
{
    private readonly ILogger<SubmitContactForm> _logger;

    public TokenAuth(ILogger<SubmitContactForm> logger)
    {
        _logger = logger;
    }

    [Function("SignIn")]
    public async Task<IActionResult> SignIn([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        if (!AuthZChecker.IsAuthorizedForSignIn(req))
        {
            return new UnauthorizedResult();
        }

        OperationResult<RefreshableToken> result = await JsonStore.Upsert<RefreshableToken>(RefreshableToken.Generate());
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            return new OkObjectResult(result.Entity.Token);
        }
        
        return new BadRequestObjectResult("Failed to generate the refreshable token");
    }

    [Function("Refresh")]
    public async Task<IActionResult> Refresh([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        if (!await AuthZChecker.IsAuthorizedForRefreshToken(req))
        {
            return new UnauthorizedResult();
        }

        OperationResult<RefreshableToken> result = await JsonStore.Upsert<RefreshableToken>(RefreshableToken.Generate());
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            return new OkObjectResult(result.Entity.Token);
        }
        
        return new BadRequestObjectResult("Failed to generate the refreshable token");
    }

    [Function("SignOut")]
    public async Task<IActionResult> SignOut([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        if (!await AuthZChecker.IsAuthorizedForRefreshToken(req))
        {
            return new UnauthorizedResult();
        }

        OperationResult<RefreshableToken> result = await JsonStore.DeleteUnconditionally<RefreshableToken>(RefreshableToken.RefreshableTokenAppId, RefreshableToken.RefreshableTokenId);
        if (result.Code == ResultCode.Success)
        {
            return new OkResult();
        }
        
        return new BadRequestObjectResult("Failed to sign out");
    }
}