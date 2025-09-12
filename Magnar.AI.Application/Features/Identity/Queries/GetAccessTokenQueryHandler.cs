using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Models.Responses;

namespace Magnar.AI.Application.Features.Identity.Queries;

public sealed record GetAccessTokenQuery(UserCredentials UserCredentials)
    : IRequest<Result<AuthenticateResponse>>;

public sealed class GetAccessTokenQueryHandler : IRequestHandler<GetAccessTokenQuery, Result<AuthenticateResponse>>
{
    private readonly IUnitOfWork unitOfWork;

    public GetAccessTokenQueryHandler(
        IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthenticateResponse>> Handle(GetAccessTokenQuery request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await unitOfWork.IdentityRepository.FindByNameOrEmailAsync(request.UserCredentials.UserName, cancellationToken);
        if (string.IsNullOrEmpty(user.Id))
        {
            return Result<AuthenticateResponse>.CreateFailure([new(Constants.Errors.UserNotFound)]);
        }

        if (!user.Active)
        {
            return Result<AuthenticateResponse>.CreateFailure([new(Constants.Errors.UserNotActive)]);
        }

        request = request with { UserCredentials = request.UserCredentials with { UserName = user.UserName } };

        AuthenticateResponse authResponse = await unitOfWork.IdentityRepository.GetAccessTokenAsync(request.UserCredentials, cancellationToken);
        if (!string.IsNullOrEmpty(authResponse.Error))
        {
            if (!await unitOfWork.IdentityRepository.EmailConfirmedAsync(user, cancellationToken))
            {
                return Result<AuthenticateResponse>.CreateFailure([new(Constants.Errors.UserEmailNotConfirmed)]);
            }

            if (!await unitOfWork.IdentityRepository.CheckPasswordAsync(user, request.UserCredentials.Password, cancellationToken))
            {
                return Result<AuthenticateResponse>.CreateFailure([new(Constants.Errors.InvalidUsernameOrPassword)]);
            }

            return Result<AuthenticateResponse>.CreateFailure([new(authResponse.Error)]);
        }

        authResponse.UsernameOrEmail = request.UserCredentials.UserName;
        authResponse.UserId = user!.Id;

        return Result<AuthenticateResponse>.CreateSuccess(authResponse);
    }
}
