using System.Security.Claims;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;

namespace Magnar.Recruitment.Infrastructure.Services;

public class CustomProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        // Get the subject (user ID) from the context
        string subjectId = context.Subject.GetSubjectId();
        ApplicationUser user = await _userManager.FindByIdAsync(subjectId);

        if (user == null)
        {
            return;
        }

        // Add the username and email as claims
        List<Claim> claims =
        [
                new Claim(Constants.IdentityApi.ApiClaims.Username, user.UserName),
                new Claim(Constants.IdentityApi.ApiClaims.Email, user.Email),
            ];

        // Add the claims to the issued token
        context.IssuedClaims.AddRange(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        // Get the subject (user ID) from the context
        string subjectId = context.Subject.GetSubjectId();
        ApplicationUser user = await _userManager.FindByIdAsync(subjectId);

        // Set the user as active if they exist
        context.IsActive = user != null;
    }
}