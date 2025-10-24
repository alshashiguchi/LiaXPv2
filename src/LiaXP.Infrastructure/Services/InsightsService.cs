using LiaXP.Domain.Interfaces;

namespace LiaXP.Infrastructure.Services;

public class InsightsService : IInsightsService
{
    private readonly ISalesDataSource _salesDataSource;

    public InsightsService(ISalesDataSource salesDataSource)
    {
        _salesDataSource = salesDataSource;
    }

    public async Task<InsightsResult> CalculateInsightsAsync(Guid companyId, Guid? storeId = null, Guid? sellerId = null)
    {
        var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var endDate = DateTime.Now;
        
        IEnumerable<Domain.Entities.Sale> sales;
        IEnumerable<Domain.Entities.Goal> goals;
        
        if (sellerId.HasValue)
        {
            sales = await _salesDataSource.GetSalesBySellerAsync(sellerId.Value, startDate, endDate);
            goals = await _salesDataSource.GetGoalsBySellerAsync(sellerId.Value, startDate);
        }
        else if (storeId.HasValue)
        {
            sales = await _salesDataSource.GetSalesByStoreAsync(storeId.Value, startDate, endDate);
            goals = await _salesDataSource.GetGoalsByCompanyAsync(companyId, startDate);
        }
        else
        {
            sales = await _salesDataSource.GetSalesByCompanyAsync(companyId, startDate, endDate);
            goals = await _salesDataSource.GetGoalsByCompanyAsync(companyId, startDate);
        }
        
        var salesList = sales.ToList();
        var goalsList = goals.ToList();
        
        var totalSales = salesList.Sum(s => s.TotalValue);
        var avgTicket = salesList.Any() ? salesList.Average(s => s.AvgTicket) : 0;
        var targetValue = goalsList.Sum(g => g.TargetValue);
        var goalGap = targetValue - totalSales;
        var goalProgress = targetValue > 0 ? (totalSales / targetValue) * 100 : 0;
        
        // Simple projection based on current pace
        var daysInMonth = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
        var daysPassed = DateTime.Now.Day;
        var projectedMonthly = daysPassed > 0 ? (totalSales / daysPassed) * daysInMonth : 0;
        
        var result = new InsightsResult
        {
            TotalSales = totalSales,
            AvgTicket = avgTicket,
            GoalGap = goalGap,
            GoalProgress = goalProgress,
            ProjectedMonthly = projectedMonthly,
            Rankings = GenerateRankings(salesList),
            FocusAreas = GenerateFocusAreas(totalSales, targetValue, avgTicket),
            Suggestions = GenerateSuggestions((double)goalProgress, avgTicket),
            CalculatedAt = DateTime.UtcNow
        };
        
        return result;
    }

    public async Task<InsightsResult?> GetCachedInsightsAsync(Guid companyId, Guid? storeId = null, Guid? sellerId = null)
    {
        // TODO: Implement cache retrieval from InsightCache table
        await Task.CompletedTask;
        return null;
    }

    public async Task CacheInsightsAsync(Guid companyId, InsightsResult insights, Guid? storeId = null, Guid? sellerId = null)
    {
        // TODO: Implement cache storage to InsightCache table
        await Task.CompletedTask;
    }

    private List<SellerRanking> GenerateRankings(List<Domain.Entities.Sale> sales)
    {
        return sales
            .GroupBy(s => s.SellerId)
            .Select(g => new SellerRanking
            {
                SellerCode = g.Key.ToString(),
                SellerName = "Seller", // TODO: Lookup seller name
                TotalSales = g.Sum(s => s.TotalValue)
            })
            .OrderByDescending(r => r.TotalSales)
            .Select((r, index) => { r.Rank = index + 1; return r; })
            .ToList();
    }

    private List<string> GenerateFocusAreas(decimal totalSales, decimal targetValue, decimal avgTicket)
    {
        var areas = new List<string>();
        
        if (totalSales < targetValue * 0.7m)
            areas.Add("Aumentar volume de vendas");
        
        if (avgTicket < 100)
            areas.Add("Trabalhar ticket médio");
        
        areas.Add("Produtos complementares");
        
        return areas;
    }

    private List<string> GenerateSuggestions(double goalProgress, decimal avgTicket)
    {
        var suggestions = new List<string>();
        
        if (goalProgress < 70)
            suggestions.Add("Foque em conversão - cada cliente conta!");
        
        if (avgTicket < 150)
            suggestions.Add("Ofereça combos e produtos premium");
        
        suggestions.Add("Mantenha contato próximo com clientes frequentes");
        
        return suggestions;
    }
}
