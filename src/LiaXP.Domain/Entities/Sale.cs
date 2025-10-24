using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

public class Sale : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid StoreId { get; set; }
    public Guid SellerId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalValue { get; set; }
    public int ItemsQty { get; set; }
    public decimal AvgTicket { get; set; }
    public string? Category { get; set; }
    
    // Navigation
    public virtual Company Company { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
    public virtual Seller Seller { get; set; } = null!;
}
