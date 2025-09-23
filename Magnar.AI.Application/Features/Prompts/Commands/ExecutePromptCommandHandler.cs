using Magnar.AI.Application.Dto.AI;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Magnar.Recruitment.Application.Features.Dashboard.Commands;

public sealed record ExecutePromptCommand(PromptDto Parameters, int WorkspaceId) : IRequest<Result<ChatResponseDto>>;

public class ExecutePromptCommandHandler : IRequestHandler<ExecutePromptCommand, Result<ChatResponseDto>>
{
    #region Members
    private readonly IAIManager aiManager;
    private readonly IKernelPluginService kernelPluginService;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;
    #endregion

    #region Constructor

    public ExecutePromptCommandHandler(
        IUnitOfWork unitOfWork,
        IAIManager aiManager,
        ICurrentUserService currentUserService,
        IKernelPluginService kernelPluginService)
    {
        this.aiManager = aiManager;
        this.kernelPluginService = kernelPluginService;
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
    }
    #endregion

    public async Task<Result<ChatResponseDto>> Handle(ExecutePromptCommand request, CancellationToken cancellationToken)
    {
        var username = currentUserService.GetUsername();

        var canAccessWorkspace = await unitOfWork.WorkspaceRepository.FirstOrDefaultAsync(x => x.CreatedBy == username && x.Id == request.WorkspaceId, false, cancellationToken);
        if (canAccessWorkspace is null)
        {
            return Result<ChatResponseDto>.CreateFailure([new(Constants.Errors.Unauthorized)]);
        }

        var defaultProvider = await unitOfWork.ProviderRepository.GetDefaultProviderAsync(request.WorkspaceId, ProviderTypes.API, cancellationToken);
        if (defaultProvider is null)
        {
            return Result<ChatResponseDto>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
        }

        var prompt = request.Parameters.Prompt;

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

         var systemMessage = await PromptLoader.LoadPromptAsync("execute-registered-functions-system.txt");
         history.AddSystemMessage(systemMessage);

        var kernel = kernelPluginService.GetKernel(request.WorkspaceId, defaultProvider.Id).Kernel;

        var functions = kernel.Plugins.SelectMany(p => p);

        var allMessages = new List<ChatMessageDto>();

        var result = await aiManager.GetChatCompletionAsync(history, executionSettings: new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(functions)
            }, kernel: kernel, cancellationToken: cancellationToken);

        string responseText = result.Content ?? string.Empty;

        // Add main history
        allMessages.AddRange(history.Where(x => x.Role != AuthorRole.System).Select(m => new ChatMessageDto
        {
            Role = m.Role.ToString(),
            Content = m.Content ?? string.Empty
        }));

        if (responseText.Contains("No suitable function available", StringComparison.OrdinalIgnoreCase))
        {
            responseText = await ExecuteDefaultSqlFallbackFunction(allMessages, request.WorkspaceId, prompt, cancellationToken) ?? responseText;
        }

        var dto = new ChatResponseDto
        {
            LatestResult = responseText ?? string.Empty,
            Messages = allMessages
        };

        return Result<ChatResponseDto>.CreateSuccess(dto);
    }

    #region Private Methods
    private async Task<string> ExecuteDefaultSqlFallbackFunction(List<ChatMessageDto> allMessages, int workspaceId, string prompt, CancellationToken cancellationToken)
    {
        allMessages.AddRange(new ChatMessageDto
        {
            Role = AuthorRole.Assistant.ToString(),
            Content = "No suitable function available, will try to generate sql query using configured database schema, if exists."
        });

        kernelPluginService.RegisterDefaultSqlFunction(workspaceId);
        var sqlKernel = kernelPluginService.GetDefaultKernel(workspaceId).Kernel;
        var defaultFunctions = sqlKernel.Plugins.SelectMany(p => p);

        var sqlHistory = new ChatHistory();
        sqlHistory.AddUserMessage(prompt);

        var sqlResult = await aiManager.GetChatCompletionAsync(sqlHistory, executionSettings: new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(defaultFunctions)
        }, kernel: sqlKernel, cancellationToken: cancellationToken);

        // Capture assistant messages (but skip tool unless error)
        foreach (var msg in sqlHistory)
        {
            if (msg.Role == AuthorRole.User)
            {
                continue;
            }

            if (msg.Role == AuthorRole.Tool)
            {
                var toolOutput = msg.Content ?? string.Empty;

                // only show if it looks like an error
                if (toolOutput.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    toolOutput.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                    toolOutput.Contains("cannot", StringComparison.OrdinalIgnoreCase))
                {
                    allMessages.Add(new ChatMessageDto
                    {
                        Role = msg.Role.ToString(),
                        Content = toolOutput
                    });
                }
            }
            else
            {
                allMessages.Add(new ChatMessageDto
                {
                    Role = msg.Role.ToString(),
                    Content = msg.Content ?? string.Empty
                });
            }
        }

        return sqlResult.Content;
    }
    #endregion
}
