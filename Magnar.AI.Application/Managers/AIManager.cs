using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Dto.AI;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Models.Responses.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
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

    public async Task<ChatCompletionResponse> GetChatCompletionAsync(ChatHistory chatHistory, CancellationToken cancellationToken = default)
    {
        var validateResult = ValidateOpenAIConfiguration();
        if (validateResult is not null)
        {
            return new ChatCompletionResponse() { Success = false, Error = new Error(validateResult) };
        }

        var result = await chatClient.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);

        return new ChatCompletionResponse() { Success = true, Content = result?.Content ?? string.Empty };
    }

    public async Task<ChatCompletionResponse> GetChatCompletionAsync(string systemMessage, string userMessage, CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();

        chatHistory.AddSystemMessage(systemMessage);
        chatHistory.AddUserMessage(userMessage);

        return await GetChatCompletionAsync(chatHistory, cancellationToken: cancellationToken);
    }

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

    #endregion
}
