using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

public class ReviewQueue : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Moment { get; set; } = string.Empty; // morning, midday, evening
    public string RecipientPhone { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string DraftMessage { get; set; } = string.Empty;
    public string? EditedMessage { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Sent
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Navigation
    public virtual Company Company { get; set; } = null!;
}
