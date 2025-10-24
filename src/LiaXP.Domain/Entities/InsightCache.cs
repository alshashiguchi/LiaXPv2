using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

public class InsightCache : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? StoreId { get; set; }
    public Guid? SellerId { get; set; }
    public DateTime InsightDate { get; set; }
    public string InsightType { get; set; } = string.Empty;
    public string DataJson { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public virtual Company Company { get; set; } = null!;
    public virtual Store? Store { get; set; }
    public virtual Seller? Seller { get; set; }
}
