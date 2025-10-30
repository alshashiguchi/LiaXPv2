using LiaXP.Domain.Entities;
using LiaXP.Domain.Enums;

namespace LiaXP.Domain.Interfaces;

/// <summary>
/// Repository for chat message persistence and analytics
/// </summary>
public interface IChatRepository
{
    /// <summary>
    /// Save a chat message to the database
    /// </summary>
    Task SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chat history for a specific user (most recent first)
    /// </summary>
    Task<List<ChatMessage>> GetUserHistoryAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chat history for an entire company
    /// </summary>
    Task<List<ChatMessage>> GetCompanyHistoryAsync(
        string companyCode,
        DateTime? since = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search messages by content
    /// </summary>
    Task<List<ChatMessage>> SearchMessagesAsync(
        string companyCode,
        string searchTerm,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get intent statistics for analytics
    /// </summary>
    Task<Dictionary<IntentType, int>> GetIntentStatisticsAsync(
        string companyCode,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total message count for a company
    /// </summary>
    Task<int> GetTotalMessagesAsync(
        string companyCode,
        DateTime? since = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete messages older than specified date (for GDPR/cleanup)
    /// </summary>
    Task DeleteOldMessagesAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default);
}
