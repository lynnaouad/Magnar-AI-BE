namespace Magnar.AI.Application.Dto.AI
{
    public class ChatResponseDto
    {
        public List<ChatMessageDto> Messages { get; set; } = [];

        public string LatestResult { get; set; } = string.Empty;
    }
}
