using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

public interface IReviewService
{
    Task<IEnumerable<ReviewQueue>> GetPendingReviewsAsync(Guid companyId);
    Task<ReviewQueue?> GetReviewByIdAsync(Guid id);
    Task<bool> ApproveAndSendAsync(Guid reviewId, string reviewedBy);
    Task<bool> EditAndApproveAsync(Guid reviewId, string editedMessage, string reviewedBy);
    Task<bool> RejectAsync(Guid reviewId, string reviewedBy, string? reason = null);
}
