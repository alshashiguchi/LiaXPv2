using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

public class ImportStatus : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public string? LastTrainedHash { get; set; }
    public DateTime? LastTrainedAt { get; set; }
    public bool IsStale { get; set; }
    
    // Navigation
    public virtual Company Company { get; set; } = null!;
}
