using Magnar.AI.Application.Dto.AI;
using Magnar.AI.Application.Models.Responses.AI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Magnar.AI.Application.Interfaces.Managers;

public interface IAIManager
{
    /// <summary>
    /// Generates embeddings for the provided text.
    /// The provided text can be chunked into smaller parts if specified.
    /// </summary>
    /// <param name="text">The input text to generate embeddings for.</param>
    /// <param name="chunk">Indicates whether the text should be chunked into smaller parts.</param>
    /// <param name="chunkSize">The size of each chunk if chunking is enabled.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A collection of <see cref="ChunkDto"/> containing the generated embeddings.</returns>
    Task<IEnumerable<ChunkDto>> GenerateEmbeddingsAsync(string text, bool chunk = true, int chunkSize = 450, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an embedding for the provided text.
    /// </summary>
    /// <param name="text">The input text to generate an embedding for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ChunkDto"/> containing the generated embedding, or null if generation fails.</returns>
    Task<ChunkDto> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a semantic search using the original query and vector search results.
    /// </summary>
    /// <param name="systemMessage">The system message for the semantic search.</param>
    /// <param name="userMessage">The user message for the semantic search.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="string"/> containing the semantic search result.</returns>
    Task<string> SemanticSearchAsync(string systemMessage, string userMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a chat completion response based on the provided chat history.
    /// </summary>
    /// <param name="chatHistory">The chat history containing previous messages for context.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ChatCompletionResponse"/> representing the chat completion response, or null if generation fails.</returns>
    Task<ChatCompletionResponse> GetChatCompletionAsync(ChatHistory chatHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a chat completion response based on the provided system and user messages.
    /// </summary>
    /// <param name="systemMessage">The system message providing context for the chat completion.</param>
    /// <param name="userMessage">The user message to which the chat completion should respond.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ChatCompletionResponse"/> representing the chat completion response, or null if generation fails.</returns>
    Task<ChatCompletionResponse> GetChatCompletionAsync(string systemMessage, string userMessage, CancellationToken cancellationToken = default);
}
