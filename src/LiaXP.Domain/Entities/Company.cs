using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

public class Company : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
    public virtual ICollection<Seller> Sellers { get; set; } = new List<Seller>();
}
