using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

public class MessageLog : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Direction { get; set; } = string.Empty; // Inbound, Outbound
    public string PhoneFrom { get; set; } = string.Empty;
    public string PhoneTo { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // Twilio, Meta
    public string? ExternalId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    
    // Navigation
    public virtual Company Company { get; set; } = null!;
}
