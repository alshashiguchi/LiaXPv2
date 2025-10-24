using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

public class Seller : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid StoreId { get; set; }
    public string SellerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PhoneE164 { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "Active";
    
    // Navigation
    public virtual Company Company { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
    public virtual ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
