namespace Magnar.AI.Application.Dto.AI;

public sealed record PromptDto
{
    public string Prompt { get; set; } = string.Empty;

    public IEnumerable<ChatMessageDto> History { get; set; } = [];
}