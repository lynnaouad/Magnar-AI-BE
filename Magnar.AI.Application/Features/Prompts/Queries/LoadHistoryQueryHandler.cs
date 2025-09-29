using Magnar.AI.Application.Dto.AI;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Stores;
using Magnar.AI.Application.Stores;
using Microsoft.AspNetCore.Http;

namespace Magnar.Recruitment.Application.Features.Dashboard.Commands;

public sealed record LoadHistoryQuery(int WorkspaceId) : IRequest<Result<IEnumerable<ChatMessageDto>>>;

public class LoadHistoryQueryHandler : IRequestHandler<LoadHistoryQuery, Result<IEnumerable<ChatMessageDto>>>
{
    #region Members
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;
    private readonly IAuthorizationService authorizationService;
    private readonly IChatMemoryStore chatMemoryStore;
    #endregion

    #region Constructor

    public LoadHistoryQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IChatMemoryStore chatMemoryStore,
        IAuthorizationService authorizationService)
    {
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
        this.authorizationService = authorizationService;
        this.chatMemoryStore = chatMemoryStore;
    }
    #endregion

    public async Task<Result<IEnumerable<ChatMessageDto>>> Handle(LoadHistoryQuery request, CancellationToken cancellationToken)
    {
        // Authorization check
        var canAccessWorkspace = await authorizationService.CanAccessWorkspace(request.WorkspaceId, cancellationToken);
        if (!canAccessWorkspace)
        {
            return Result<IEnumerable<ChatMessageDto>>.CreateFailure([new(Constants.Errors.Unauthorized)], StatusCodes.Status401Unauthorized);
        }

        var currentUserId = currentUserService.GetId();

        var storedMessages = chatMemoryStore.Load(request.WorkspaceId, currentUserId);

        var allMessages = storedMessages
          .Where(x => x.Role != "system")
          .Select(m => new ChatMessageDto
          {
              Role = m.Role.ToString(),
              Content = m.Content ?? string.Empty
          });

        return Result<IEnumerable<ChatMessageDto>>.CreateSuccess(allMessages);
    }
}
