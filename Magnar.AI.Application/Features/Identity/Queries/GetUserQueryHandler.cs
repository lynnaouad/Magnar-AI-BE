using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.Recruitment.Application.Features.Identity.Queries;

public sealed record GetUserQuery(int UserId)
    : IRequest<Result<ApplicationUserDto>>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Result<ApplicationUserDto>>
{
    private readonly IMapper mapper;
    private readonly IUnitOfWork unitOfWork;

    public GetUserQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        this.mapper = mapper;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<ApplicationUserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await unitOfWork.IdentityRepository.GetUserAsync(request.UserId, cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        ApplicationUserDto mappedUser = mapper.Map<ApplicationUserDto>(user);

        return Result<ApplicationUserDto>.CreateSuccess(mappedUser);
    }
}
