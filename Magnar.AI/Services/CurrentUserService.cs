using Magnar.AI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Magnar.AI.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public int GetId()
    {
        var userIdString = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdString, out var userId) ? userId : 0;
    }

    public string GetUsername()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirstValue(Constants.IdentityApi.ApiClaims.Username) ?? string.Empty;
    }

    public string GetEmail()
    {
        return httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    }

    public bool IsCurrentUser(int userId)
    {
        var currentUser = GetId();

        return currentUser != default && currentUser == userId;
    }
}
