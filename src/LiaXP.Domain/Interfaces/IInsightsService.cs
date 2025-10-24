using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

public interface IInsightsService
{
    Task<InsightsResult> CalculateInsightsAsync(Guid companyId, Guid? storeId = null, Guid? sellerId = null);
    Task<InsightsResult?> GetCachedInsightsAsync(Guid companyId, Guid? storeId = null, Guid? sellerId = null);
    Task CacheInsightsAsync(Guid companyId, InsightsResult insights, Guid? storeId = null, Guid? sellerId = null);
}

public class InsightsResult
{
    public decimal TotalSales { get; set; }
    public decimal AvgTicket { get; set; }
    public decimal GoalGap { get; set; }
    public decimal GoalProgress { get; set; }
    public decimal ProjectedMonthly { get; set; }
    public List<SellerRanking> Rankings { get; set; } = new();
    public List<string> FocusAreas { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public class SellerRanking
{
    public string SellerCode { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int Rank { get; set; }
}
