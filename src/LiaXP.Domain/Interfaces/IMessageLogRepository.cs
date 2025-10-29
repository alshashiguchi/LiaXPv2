using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

/// <summary>
/// Repository for message logs (WhatsApp inbound/outbound)
/// </summary>
public interface IMessageLogRepository
{
    /// <summary>
    /// Save a message log entry
    /// </summary>
    Task SaveAsync(MessageLog messageLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get message logs for a specific company
    /// </summary>
    Task<IEnumerable<MessageLog>> GetByCompanyAsync(
        Guid companyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get message logs for a specific phone number
    /// </summary>
    Task<IEnumerable<MessageLog>> GetByPhoneAsync(
        string phoneE164,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed messages for retry
    /// </summary>
    Task<IEnumerable<MessageLog>> GetFailedMessagesAsync(
        Guid companyId,
        DateTime? since = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update message status (for delivery confirmations)
    /// </summary>
    Task UpdateStatusAsync(
        Guid messageLogId,
        string status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}