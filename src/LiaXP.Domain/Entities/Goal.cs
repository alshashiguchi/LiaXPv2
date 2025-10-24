using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

public class Goal : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid StoreId { get; set; }
    public Guid SellerId { get; set; }
    public DateTime Month { get; set; }
    public decimal TargetValue { get; set; }
    public decimal? TargetTicket { get; set; }
    public decimal? TargetConversion { get; set; }
    
    // Navigation
    public virtual Company Company { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
    public virtual Seller Seller { get; set; } = null!;
}
