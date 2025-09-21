using Magnar.AI.Application.Dto.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Magnar.Recruitment.Application.Features.Dashboard.Commands;

public sealed record ExecutePromptCommand(PromptDto parameters, int workspaceId) : IRequest<Result<ChatResponseDto>>;

public class ExecutePromptCommandHandler : IRequestHandler<ExecutePromptCommand, Result<ChatResponseDto>>
{
    #region Members
    private readonly IChatCompletionService completionService;
    private readonly IApiProviderService apiProviderService;
    #endregion

    #region Constructor

    public ExecutePromptCommandHandler(
        IChatCompletionService completionService,
        IApiProviderService apiProviderService)
    {
        this.completionService = completionService;
        this.apiProviderService = apiProviderService;
    }
    #endregion

    public async Task<Result<ChatResponseDto>> Handle(ExecutePromptCommand request, CancellationToken cancellationToken)
    {
        var prompt = request.parameters.Prompt;

        var history = new ChatHistory();
        history.AddUserMessage(prompt);

        history.AddSystemMessage(@"
You are not allowed to answer directly. 
You must call one of the registered functions. 
Every parameter in the schema must be checked.
If the user provided a value for a parameter, you must include it exactly as stated.
Never ignore parameters explicitly mentioned by the user.
Use the exact values from the user prompt when filling parameters. 
If none apply, say 'No suitable function available.' 
Never return lists or facts directly.");


        var kernel = apiProviderService.GetKernel(request.workspaceId).Kernel;

        var functions = kernel.Plugins.SelectMany(p => p);

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(functions)
        };

        var result = await completionService.GetChatMessageContentAsync(
            history,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel);

        var dto = new ChatResponseDto
        {
            LatestResult = result.Content ?? string.Empty, 
            Messages = [.. history.Select(m => new ChatMessageDto
            {
                Role = m.Role.ToString(),
                Content = m.Content ?? string.Empty
            })]
        };

        return Result<ChatResponseDto>.CreateSuccess(dto);
    }
}
