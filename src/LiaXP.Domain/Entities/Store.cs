using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

public class Store : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<Seller> Sellers { get; set; } = new List<Seller>();
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
