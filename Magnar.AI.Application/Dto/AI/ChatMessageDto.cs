namespace Magnar.AI.Application.Dto.AI
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = ""; 

        public string Content { get; set; } = "";

        public DateTime CreatedAt { get; set; }
    }
}
