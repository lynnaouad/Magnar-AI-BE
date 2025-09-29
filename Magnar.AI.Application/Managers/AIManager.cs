using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Dto.AI;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Models.Responses.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using Error = Magnar.AI.Application.Models.Error;

namespace Magnar.AI.Application.Managers;

public class AIManager : IAIManager
{
    #region Members
    private readonly OpenAIConfiguration openAiConfiguration;
    private readonly IChatCompletionService chatClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> embeddingClient;
    #endregion

    #region Constructor
    public AIManager(
        IOptions<OpenAIConfiguration> openAiConfiguration,
        IEmbeddingGenerator<string, Embedding<float>> embeddingClient,
        IChatCompletionService chatClient)
    {
        this.openAiConfiguration = openAiConfiguration.Value;
        this.chatClient = chatClient;
        this.embeddingClient = embeddingClient;
    }
    #endregion

    public async Task<IEnumerable<ChunkDto>> GenerateEmbeddingsAsync(string text, bool chunk = true, int chunkSize = 450, CancellationToken cancellationToken = default)
    {
        var validateResult = ValidateOpenAIConfiguration();
        if (validateResult is not null || string.IsNullOrEmpty(text))
        {
            return [];
        }

        var chunks = chunk ? TextChunker.ChunkText(text, chunkSize, true, true) : [text];

        try
        {
            var embeddings = await embeddingClient.GenerateAsync(chunks, cancellationToken: cancellationToken);
            if (embeddings is null || embeddings.Count == 0)
            {
                return [];
            }

            var indexedChunks = chunks.Select((chunk, index) => new { Index = index, Chunk = chunk });

            var result = indexedChunks.Zip(embeddings, (chunkInfo, embedding) => new ChunkDto
            {
                Index = chunkInfo.Index,
                Text = chunkInfo.Chunk,
                Embedding = embedding.Vector.ToArray(),
            });

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
        }

        return [];
    }

    public async Task<ChunkDto> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        return (await GenerateEmbeddingsAsync(text, false, cancellationToken: cancellationToken)).FirstOrDefault();
    }

    public async Task<string> SemanticSearchAsync(string systemMessage, string userMessage, CancellationToken cancellationToken)
    {
        var result = await GetChatCompletionAsync(systemMessage, userMessage, cancellationToken);
        if (!result.Success)
        {
            return string.Empty;
        }

        return result.Content;
    }

    public async Task<ChatCompletionResponse> GetChatCompletionAsync(ChatHistory chatHistory, OpenAIPromptExecutionSettings? executionSettings = null, Microsoft.SemanticKernel.Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        var validateResult = ValidateOpenAIConfiguration();
        if (validateResult is not null)
        {
            return new ChatCompletionResponse() { Success = false, Error = new Error(validateResult) };
        }

        executionSettings ??= new OpenAIPromptExecutionSettings() { FunctionChoiceBehavior  = FunctionChoiceBehavior.None() };

        var result = await chatClient.GetChatMessageContentAsync(chatHistory, executionSettings: executionSettings, kernel: kernel, cancellationToken: cancellationToken);

        return new ChatCompletionResponse() { Success = true, Content = result?.Content ?? string.Empty, Items = result.Items };
    }

    public async Task<ChatCompletionResponse> GetChatCompletionAsync(string systemMessage, string userMessage, CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();

        chatHistory.AddSystemMessage(systemMessage);
        chatHistory.AddUserMessage(userMessage);

        return await GetChatCompletionAsync(chatHistory, cancellationToken: cancellationToken);
    }

    public async Task ExecutePrompt(ChatHistory history, OpenAIPromptExecutionSettings? executionSettings = null, Microsoft.SemanticKernel.Kernel? kernel = null, CancellationToken cancellationToken = default)
    {
        string responseText = string.Empty;

        // Tool-execution loop
        while (true)
        {
            var result = await chatClient.GetChatMessageContentAsync(history, executionSettings: executionSettings, kernel: kernel, cancellationToken: cancellationToken); ;

            // If no tool calls → grab assistant text and break
            if (!result.Items.Any(IsToolCall))
            {
                responseText = result.Items.Select(GetAssistantText).FirstOrDefault(t => !string.IsNullOrEmpty(t)) ?? string.Empty;

                if (!string.IsNullOrEmpty(responseText))
                {
                    history.AddAssistantMessage(responseText);
                }
                   
                break;
            }

            // Otherwise process tool calls
            foreach (var toolCall in result.Items.OfType<Microsoft.SemanticKernel.FunctionCallContent>())
            {
                try
                {
                    // Resolve plugin + function
                    var (pluginName, functionName) = SplitFunctionName(toolCall.FunctionName);
                    var function = kernel.Plugins.GetFunction(pluginName, functionName);

                    if (function is null)
                    {
                        history.AddMessage(AuthorRole.Tool,  $"Tool execution failed: function {toolCall.FunctionName} not found.");
                        continue;
                    }

                    // Build KernelArguments
                    var arguments = new KernelArguments();
                    foreach (var kv in toolCall.Arguments)
                    {
                        arguments[kv.Key] = kv.Value?.ToString() ?? string.Empty;
                    }

                    // Invoke function
                    var toolResult = await kernel.InvokeAsync(function, arguments, cancellationToken);

                    // Add back into history
                    history.AddMessage(AuthorRole.Tool, toolResult?.ToString() ?? "No result");
                }
                catch (Exception ex)
                {
                    history.AddMessage(AuthorRole.Tool, $"Tool execution failed: {ex.Message}");
                }
            }
        }

    }   
   
    public IEnumerable<ChatMessageDto> BuildChatMessages(int workspaceId, int userId, ChatHistory history)
    {
        var trimmed = TrimHistory(history);

        return trimmed
            .Where(m => m.Role != AuthorRole.Tool) // skip tool requests
            .Select(m => new ChatMessageDto()
            {
                Role = m.Role.ToString(),
                Content = m.Content ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            });
    }

    public ChatHistory BuildChatHistory(IEnumerable<ChatMessageDto> messages)
    {
        var history = new ChatHistory();

        if(messages is null || !messages.Any())
        {
            return history;
        }

        foreach (var m in messages.OrderBy(x => x.CreatedAt))
        {
            switch (m.Role)
            {
                case "user": history.AddUserMessage(m.Content); break;
                case "assistant": history.AddAssistantMessage(m.Content); break;
                case "system": history.AddSystemMessage(m.Content); break;
            }
        }

        return history;
    }

    public int GetModelMaxTokenLimit() => ModelContextLimits.GetLimit(openAiConfiguration.Model);

    public int CountTokenNumber(string text) => TokenCounter.CountTokens(text ?? string.Empty);

    #region Private Methods

    private string ValidateOpenAIConfiguration()
    {
        if (!openAiConfiguration.Enabled)
        {
            return Constants.Errors.OpenAiAccess;
        }

        if (string.IsNullOrEmpty(openAiConfiguration.ApiKey))
        {
            return Constants.Errors.MissingOpenAiApiKey;
        }

        return null;
    }

    private bool IsToolCall(KernelContent content) => content is Microsoft.SemanticKernel.FunctionCallContent;

    private string? GetAssistantText(KernelContent content) => (content as Microsoft.SemanticKernel.TextContent)?.Text;

    /// <summary>
    /// Splits "Plugin.Function" into (plugin, function).
    /// If no dot → plugin = "", function = fullName.
    /// </summary>
    private static (string plugin, string function) SplitFunctionName(string name)
    {
        var parts = name.Split('.', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (string.Empty, parts[0]);
    }

    private IEnumerable<ChatMessageContent> TrimHistory(ChatHistory history)
    {
        // Get the model-specific limit
        int maxTokens = GetModelMaxTokenLimit();

        // Separate system vs conversation
        var systemMessages = history.Where(h => h.Role == AuthorRole.System).ToList();
        var conversation = history.Where(h => h.Role != AuthorRole.System).ToList();

        // Always keep system messages, even if they push you over budget
        int systemTokenCount = systemMessages.Sum(m => CountTokenNumber(m.Content ?? string.Empty));
        int availableBudget = Math.Max(0, maxTokens - systemTokenCount);

        var trimmedConversation = new List<ChatMessageContent>();
        int tokenCount = 0;

        // Walk backwards (newest → oldest) until budget exceeded
        for (int i = conversation.Count - 1; i >= 0; i--)
        {
            var msg = conversation[i];
            var msgTokens = CountTokenNumber(msg.Content ?? string.Empty);

            if (tokenCount + msgTokens > availableBudget)
            {
                break; // stop adding older messages
            }

            if (!string.IsNullOrEmpty(msg.Content))
            {
                trimmedConversation.Insert(0, msg); // keep message at front
            }
           
            tokenCount += msgTokens;
        }

        return systemMessages.Concat(trimmedConversation);
    }

    #endregion
}
