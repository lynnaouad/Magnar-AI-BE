using Magnar.AI.Application.Dto.AI;
using Magnar.AI.Application.Interfaces.Stores;

namespace Magnar.AI.Application.Stores
{
    public class ChatMemoryStore : IChatMemoryStore
    {
        private readonly Dictionary<(int, int), List<ChatMessageDto>> store = [];

        public IEnumerable<ChatMessageDto> Load(int workspaceId, int userId)
        {
            return store.TryGetValue((workspaceId, userId), out var messages)
                ? [.. messages]
                : [];
        }

        public void Set(int workspaceId, int userId, IEnumerable<ChatMessageDto> messages)
        {
            store[(workspaceId, userId)] = [.. messages];
        }

        public void Clear(int workspaceId, int userId)
        {
            store.Remove((workspaceId, userId));
        } 
    }
}
