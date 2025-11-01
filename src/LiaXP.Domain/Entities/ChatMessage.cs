using LiaXP.Domain.Common;
using LiaXP.Domain.Enums;

namespace LiaXP.Domain.Entities;

/// <summary>
/// Chat message entity - stores conversation history between users and AI assistant
/// </summary>
public class ChatMessage : BaseEntity
{
    /// <summary>
    /// Foreign key to Company (technical key)
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// Foreign key to User
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// User's message content
    /// </summary>
    public string UserMessage { get; private set; } = string.Empty;

    /// <summary>
    /// AI assistant's response
    /// </summary>
    public string AssistantResponse { get; private set; } = string.Empty;

    /// <summary>
    /// Detected intent type
    /// </summary>
    public IntentType Intent { get; private set; }

    /// <summary>
    /// Additional metadata as JSON
    /// Can include: model used, tokens, execution time, etc.
    /// </summary>
    public string? Metadata { get; private set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual User User { get; set; } = null!;

    // EF Core constructor
    private ChatMessage() { }

    /// <summary>
    /// Create a new chat message
    /// </summary>
    public ChatMessage(
        Guid companyId,
        Guid userId,
        string userMessage,
        string assistantResponse,
        IntentType intent,
        string? metadata = null)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID cannot be empty", nameof(companyId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty", nameof(userId));

        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("User message cannot be empty", nameof(userMessage));

        if (string.IsNullOrWhiteSpace(assistantResponse))
            throw new ArgumentException("Assistant response cannot be empty", nameof(assistantResponse));

        CompanyId = companyId;
        UserId = userId;
        UserMessage = userMessage.Trim();
        AssistantResponse = assistantResponse.Trim();
        Intent = intent;
        Metadata = metadata;
    }
}