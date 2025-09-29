using Microsoft.SemanticKernel.ChatCompletion;

namespace Magnar.AI.Application.Interfaces.Stores
{
    public interface IConversationStore
    {
        Task<ChatHistory> LoadAsync(int workspaceId, int userId);
      
        Task SaveAsync(int workspaceId, int userId, ChatHistory history);
      
        Task ClearAsync(int workspaceId, int userId);
    }

}
