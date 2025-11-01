
namespace LiaXP.Application.DTOs.Chat;

public class ChatHistoryItem
{
    public Guid Id { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public string AssistantResponse { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}