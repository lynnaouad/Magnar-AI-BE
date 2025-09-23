using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Models;
using Magnar.AI.Application.Models.Responses;
using Magnar.AI.Infrastructure.Extensions;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Options;
using Serilog;

namespace Magnar.Recruitment.Infrastructure.Repositories;

public sealed class IdentityRepository : IIdentityRepository
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly MagnarAIDbContext context;
    private readonly UrlsConfiguration urlConfiguration;
    private readonly HttpClient httpClient;

    public IdentityRepository(
        UserManager<ApplicationUser> userManager,
        MagnarAIDbContext context,
        IHttpClientFactory httpClientFactory,
        IOptions<UrlsConfiguration> urlConfiguration)
    {
        this.userManager = userManager;
        this.context = context;
        this.urlConfiguration = urlConfiguration.Value;
        httpClient = httpClientFactory.CreateClient();
    }

    public async Task<AuthenticateResponse> GetAccessTokenAsync(UserCredentials userCredentials, CancellationToken cancellationToken)
    {
        HttpResponseMessage res = await httpClient.SendAsync(CreateAuthenticateRequest(userCredentials.UserName, userCredentials.Password), cancellationToken);

        AuthenticateResponse authenticationResult = (await res.Content.ReadAsStringAsync(cancellationToken))
            .DeserializeJsonString<AuthenticateResponse>();

        return authenticationResult ?? throw new InvalidOperationException();
    }

    public async Task<AuthenticateResponse> RefreshTokenAsync(UserCredentials userCredentials, CancellationToken cancellationToken)
    {
        HttpResponseMessage res = await httpClient.SendAsync(CreateRefreshTokenRequest(userCredentials.Token), cancellationToken);
        AuthenticateResponse authenticationResult = (await res.Content.ReadAsStringAsync(cancellationToken))
            .DeserializeJsonString<AuthenticateResponse>();

        return authenticationResult ?? throw new InvalidOperationException();
    }

    public async Task<ApplicationUser> GetUserAsync(int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == default)
        {
            return new ApplicationUser() { Id = 0 };
        }

        ApplicationUser user = await userManager.FindByIdAsync(userId.ToString());

        return user is null ? new ApplicationUser() { Id = 0 } : user;
    }

    public async Task<bool> EmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<ApplicationUser> FindByNameOrEmailAsync(string emailOrUsername, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(emailOrUsername))
        {
            return new ApplicationUser() { Id = 0 };
        }

        ApplicationUser user = await FindByNameAsync(emailOrUsername, cancellationToken);
        if (user.Id == default)
        {
            user = await FindByEmailAsync(emailOrUsername, cancellationToken);
        }

        return user;
    }

    public async Task<ApplicationUser> FindByNameAsync(string username, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(username))
        {
            return new ApplicationUser() { Id = 0 };
        }

        ApplicationUser user = await userManager.FindByNameAsync(username);

        return user is null ? new ApplicationUser() { Id = 0 } : user;
    }

    public async Task<ApplicationUser> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(email))
        {
            return new ApplicationUser() { Id = 0 };
        }

        ApplicationUser user = await userManager.FindByEmailAsync(email);

        return user is null ? new ApplicationUser() { Id = 0 } : user;
    }

    public async Task<IdentityResult> ConfirmEmailAsync(ApplicationUser user, string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.ConfirmEmailAsync(user, token);
    }

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (user is null || user.Email is null || user.UserName is null)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        IdentityResult result = await userManager.CreateAsync(user, password);
        return !result.Succeeded ? result : await userManager.SetLockoutEnabledAsync(user, false);
    }

    public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (userId == default)
        {
            return false;
        }

        ApplicationUser user = await userManager.FindByIdAsync(userId.ToString());
        return user is null || user == default || await DeleteUserAsync(user, cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser existingUser = await userManager.FindByIdAsync(user.Id.ToString());

        return existingUser == default || (await userManager.DeleteAsync(user)).Succeeded;
    }

    public async Task<bool> ValidatePasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.ResetPasswordAsync(user, token, newPassword);
    }

    public async Task<IdentityResult> RemovePasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.RemovePasswordAsync(user);
    }

    public async Task<IdentityResult> AddPasswordAsync(ApplicationUser user, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.AddPasswordAsync(user, newPassword);
    }

    public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<bool> VerifyUserTokenAsync(ApplicationUser user, string purpose, string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return user is null
            ? throw new InvalidOperationException(Constants.Errors.UserNotFound)
            : await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, purpose, token);
    }

    public IQueryable<T> GetAsQueryable<T>()
        where T : class
    {
        return context.Set<T>().AsQueryable();
    }

    public async Task<OdataResponse<T>> OdataGetAsync<T>(ODataQueryOptions<T> filterOptions = null, CancellationToken cancellationToken = default)
        where T : class
    {
        OdataResponse<T> response = new()
        { Value = [], TotalCount = 0 };

        try
        {
            IQueryable<T> viewData = GetAsQueryable<T>();
            if (viewData is null)
            {
                return response;
            }

            if (filterOptions?.Filter is not null)
            {
                viewData = (IQueryable<T>)filterOptions.Filter.ApplyTo(viewData, new ODataQuerySettings());
            }

            int totalCount = await viewData.CountAsync(cancellationToken);

            if (filterOptions is null)
            {
                response.Value = await viewData.AsNoTracking().ToListAsync(cancellationToken);
                response.TotalCount = totalCount;

                return response;
            }

            if (filterOptions.ApplyTo(viewData, new ODataQuerySettings()) is not IQueryable<T> listQueryable)
            {
                return response;
            }

            if (listQueryable is null || !listQueryable.Any())
            {
                return response;
            }

            List<T> values = await listQueryable.AsNoTracking().ToListAsync(cancellationToken);
            if (values is null || values.Count == 0)
            {
                return response;
            }

            response.Value = values;
            response.TotalCount = totalCount;

            return response;
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
            return response;
        }
    }

    private HttpRequestMessage CreateAuthenticateRequest(string username, string password)
    {
        return new(
        HttpMethod.Post,
        string.Concat(urlConfiguration.Authority, "/", Constants.IdentityApi.Endpoints.Routes.AccessToken))
        {
            Content = new FormUrlEncodedContent([
            new (Constants.IdentityApi.Endpoints.Parameters.GrantType, Constants.IdentityApi.Clients.Api.GrantTypes.Password),
            new (Constants.IdentityApi.Endpoints.Parameters.ClientId, Constants.IdentityApi.Clients.Api.Id),
            new (Constants.IdentityApi.Endpoints.Parameters.ClientSecret, Constants.IdentityApi.Clients.Api.Secret),
            new (Constants.IdentityApi.Endpoints.Parameters.Scope, Constants.IdentityApi.Clients.Api.DefaultScope),
            new (Constants.IdentityApi.Endpoints.Parameters.UserName, username),
            new (Constants.IdentityApi.Endpoints.Parameters.Password, password)]),
        };
    }

    private HttpRequestMessage CreateRefreshTokenRequest(string refreshToken)
    {
        return new(
        HttpMethod.Post,
        string.Concat(urlConfiguration.Authority, "/", Constants.IdentityApi.Endpoints.Routes.AccessToken))
        {
            Content = new FormUrlEncodedContent([
            new (Constants.IdentityApi.Endpoints.Parameters.GrantType, Constants.IdentityApi.Clients.Api.GrantTypes.RefreshToken),
            new (Constants.IdentityApi.Endpoints.Parameters.ClientId, Constants.IdentityApi.Clients.Api.Id),
            new (Constants.IdentityApi.Endpoints.Parameters.ClientSecret, Constants.IdentityApi.Clients.Api.Secret),
            new (Constants.IdentityApi.Endpoints.Parameters.RefreshToken, refreshToken)]),
        };
    }
}
