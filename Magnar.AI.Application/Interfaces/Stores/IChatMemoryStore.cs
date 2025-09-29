using Magnar.AI.Application.Dto.AI;

namespace Magnar.AI.Application.Interfaces.Stores
{
    public interface IChatMemoryStore
    {
        IEnumerable<ChatMessageDto> Load(int workspaceId, int userId);

        void Set(int workspaceId, int userId, IEnumerable<ChatMessageDto> messages);

        void Clear(int workspaceId, int userId);
    }
}
