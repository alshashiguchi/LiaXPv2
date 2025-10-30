using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

public interface IReviewService
{
    /// <summary>
    /// Creates a new review queue entry for HITL workflow
    /// </summary>
    Task<ReviewQueue> CreateReviewAsync(ReviewQueue review, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending reviews for a company
    /// </summary>
    Task<IEnumerable<ReviewQueue>> GetPendingReviewsAsync(Guid companyId);

    /// <summary>
    /// Gets a specific review by ID
    /// </summary>
    Task<ReviewQueue?> GetReviewByIdAsync(Guid id);

    /// <summary>
    /// Approves a review and optionally sends the message immediately
    /// </summary>
    Task<bool> ApproveAndSendAsync(Guid reviewId, string reviewedBy);

    /// <summary>
    /// Edits the message and approves it
    /// </summary>
    Task<bool> EditAndApproveAsync(Guid reviewId, string editedMessage, string reviewedBy);

    /// <summary>
    /// Rejects a review with an optional reason
    /// </summary>
    Task<bool> RejectAsync(Guid reviewId, string reviewedBy, string? reason = null);
}
