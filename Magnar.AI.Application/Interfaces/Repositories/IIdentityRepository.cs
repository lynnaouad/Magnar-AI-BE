using Magnar.AI.Application.Models.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Interfaces.Repositories;

public interface IIdentityRepository
{
    Task<AuthenticateResponse> GetAccessTokenAsync(UserCredentials userCredentials, CancellationToken cancellationToken);

    Task<AuthenticateResponse> RefreshTokenAsync(UserCredentials userCredentials, CancellationToken cancellationToken);

    Task<ApplicationUser> GetUserAsync(int userId, CancellationToken cancellationToken);

    Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, CancellationToken cancellationToken);

    Task<IdentityResult> UpdateUserAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken);

    Task<bool> DeleteUserAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<ApplicationUser> FindByNameOrEmailAsync(string emailOrUsername, CancellationToken cancellationToken);

    Task<ApplicationUser> FindByNameAsync(string username, CancellationToken cancellationToken);

    Task<ApplicationUser> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token, CancellationToken cancellationToken);

    Task<bool> ValidatePasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken);

    Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword, CancellationToken cancellationToken);

    Task<IdentityResult> RemovePasswordAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<IdentityResult> AddPasswordAsync(ApplicationUser user, string newPassword, CancellationToken cancellationToken);

    Task<bool> VerifyUserTokenAsync(ApplicationUser user, string purpose, string token, CancellationToken cancellationToken);

    Task<bool> EmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken);

    Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken);

    Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword, CancellationToken cancellationToken);

    IQueryable<T> GetAsQueryable<T>()
         where T : class;

    Task<OdataResponse<T>> OdataGetAsync<T>(ODataQueryOptions<T>? filterOptions = null, CancellationToken cancellationToken = default)
         where T : class;
}
