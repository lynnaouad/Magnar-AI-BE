using Magnar.AI.Application.Dto.AI;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Interfaces.Stores;
using Magnar.AI.Application.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;

namespace Magnar.Recruitment.Application.Features.Dashboard.Commands;

public sealed record ExecutePromptCommand(PromptDto Parameters, int WorkspaceId) : IRequest<Result<IEnumerable<ChatMessageDto>>>;

public class ExecutePromptCommandHandler : IRequestHandler<ExecutePromptCommand, Result<IEnumerable<ChatMessageDto>>>
{
    #region Members
    private readonly IAIManager aiManager;
    private readonly IKernelPluginService kernelPluginService;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;
    private readonly IAuthorizationService authorizationService;
    #endregion

    #region Constructor

    public ExecutePromptCommandHandler(
        IUnitOfWork unitOfWork,
        IAIManager aiManager,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IKernelPluginService kernelPluginService)
    {
        this.aiManager = aiManager;
        this.kernelPluginService = kernelPluginService;
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
        this.authorizationService = authorizationService;
    }
    #endregion

    public async Task<Result<IEnumerable<ChatMessageDto>>> Handle(ExecutePromptCommand request, CancellationToken cancellationToken)
    {
        // Authorization check
        var canAccessWorkspace = await authorizationService.CanAccessWorkspace(request.WorkspaceId, cancellationToken);
        if (!canAccessWorkspace)
        {
            return Result<IEnumerable<ChatMessageDto>>.CreateFailure([new(Constants.Errors.Unauthorized)], StatusCodes.Status401Unauthorized);
        }

        // Ensure default provider exist
        var defaultProvider = await unitOfWork.ProviderRepository.GetDefaultProviderAsync(request.WorkspaceId, ProviderTypes.API, cancellationToken);
        if (defaultProvider is null)
        {
            return Result<IEnumerable<ChatMessageDto>>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
        }

        var currentUserId = currentUserService.GetId();

        var prompt = request.Parameters.Prompt;

        // Load previous chat history
        var history = aiManager.BuildChatHistory(request.Parameters.History);

        var systemMessage = await PromptLoader.LoadPromptAsync("execute-registered-functions-system.txt");
        systemMessage = string.Format(systemMessage, DateTime.UtcNow);

        // Prepend system message so it's first
        history.Insert(0, new ChatMessageContent(AuthorRole.System, systemMessage));

        // Add user message
        history.AddUserMessage(prompt);

        // Build kernel + functions
        var kernel = kernelPluginService.GetKernel(request.WorkspaceId, defaultProvider.Id).Kernel;
        var functions = kernel.Plugins.SelectMany(p => p);

        var execSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = kernel is not null && functions.Any() ? FunctionChoiceBehavior.Auto(functions) : FunctionChoiceBehavior.None()
        };

        // Run AI
        await aiManager.ExecutePrompt(history, execSettings, kernel, cancellationToken);

        // Convert ChatHistory (with trimming)
        var messagesToSave = aiManager.BuildChatMessages(request.WorkspaceId, currentUserId,  history);

        // Build messages for DTO (skip system)
        var allMessages = history
          .Where(x => x.Role != AuthorRole.System && x.Role != AuthorRole.Tool)
          .Select(m => new ChatMessageDto
          {
              Role = m.Role.ToString(),
              Content = m.Content ?? string.Empty
          })
          .ToList();

        return Result<IEnumerable<ChatMessageDto>>.CreateSuccess(allMessages);
    }
}
