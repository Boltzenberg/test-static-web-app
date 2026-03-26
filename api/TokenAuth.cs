using Boltzenberg.Functions.DataModels.Auth;
using Boltzenberg.Functions.Dtos.Auth;
using Boltzenberg.Functions.Logging;
using Boltzenberg.Functions.Storage;
using Boltzenberg.Functions.Storage.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Boltzenberg.Functions;

public class TokenAuth
{
    private readonly IJsonStore<RefreshableTokenDocument> _store;

    public TokenAuth(IJsonStore<RefreshableTokenDocument> store)
    {
        _store = store;
    }

    private static RefreshableTokenDocument GenerateDocument()
    {
        var bytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        return new RefreshableTokenDocument
        {
            id = DataModels.Auth.RefreshableToken.RefreshableTokenId,
            Token = Convert.ToBase64String(bytes),
            Expiration = DateTime.UtcNow.AddHours(2)
        };
    }

    [Function("SignIn")]
    public async Task<IActionResult> SignInUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("TokenAuth.SignIn", req, SignIn);
    private async Task<IActionResult> SignIn(HttpRequest req, LogBuffer log)
    {
        if (!AuthZChecker.IsAuthorizedForSignIn(req))
        {
            log.Error("Authorization failed for sign in");
            return new UnauthorizedResult();
        }

        var doc = GenerateDocument();
        OperationResult<RefreshableTokenDocument> result = await _store.UpsertAsync(doc);
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            var response = new TokenResponse(result.Entity.Token, result.Entity.Expiration);
            return new OkObjectResult(response);
        }

        log.Error("Failed to generate the refreshable token");
        return new BadRequestObjectResult("Failed to generate the refreshable token");
    }

    [Function("Refresh")]
    public async Task<IActionResult> RefreshUnwrapped([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("TokenAuth.Refresh", req, Refresh);
    private async Task<IActionResult> Refresh(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForRefreshToken(req))
        {
            log.Error("Authorization failed for refresh");
            return new UnauthorizedResult();
        }

        var doc = GenerateDocument();
        OperationResult<RefreshableTokenDocument> result = await _store.UpsertAsync(doc);
        if (result.Code == ResultCode.Success && result.Entity != null)
        {
            var response = new TokenResponse(result.Entity.Token, result.Entity.Expiration);
            return new OkObjectResult(response);
        }

        log.Error("Failed to generate the refreshable token");
        return new BadRequestObjectResult("Failed to generate the refreshable token");
    }

    [Function("SignOut")]
    public async Task<IActionResult> SignOut([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req) =>
        await LogBuffer.Wrap("TokenAuth.SignOut", req, SignOut);
    private async Task<IActionResult> SignOut(HttpRequest req, LogBuffer log)
    {
        if (!await AuthZChecker.IsAuthorizedForRefreshToken(req))
        {
            log.Error("Authorization failed for signout");
            return new UnauthorizedResult();
        }

        OperationResult<RefreshableTokenDocument> result = await _store.DeleteUnconditionallyAsync(
            RefreshableTokenDocument.PartitionKey,
            DataModels.Auth.RefreshableToken.RefreshableTokenId);
        if (result.Code == ResultCode.Success)
        {
            return new OkResult();
        }

        log.Error("Failed to sign out");
        return new BadRequestObjectResult("Failed to sign out");
    }
}
