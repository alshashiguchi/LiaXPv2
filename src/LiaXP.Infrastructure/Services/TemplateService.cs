using LiaXP.Domain.Interfaces;
using LiaXP.Domain.Enums;

namespace LiaXP.Infrastructure.Services;

public class TemplateService : ITemplateService
{
    private readonly IInsightsService _insightsService;
    private readonly ISalesDataSource _salesDataSource;

    public TemplateService(IInsightsService insightsService, ISalesDataSource salesDataSource)
    {
        _insightsService = insightsService;
        _salesDataSource = salesDataSource;
    }

    public async Task<string> GenerateMessageAsync(MomentType moment, Guid companyId, Guid? storeId = null, Guid? sellerId = null)
    {
        var insights = await _insightsService.CalculateInsightsAsync(companyId, storeId, sellerId);
        
        return moment switch
        {
            MomentType.Morning => GenerateMorningMessage(insights),
            MomentType.Midday => GenerateMiddayMessage(insights),
            MomentType.Evening => GenerateEveningMessage(insights),
            _ => "Boa sorte hoje!"
        };
    }

    public async Task<List<MessageDraft>> GenerateAllMessagesAsync(MomentType moment, Guid companyId)
    {
        var drafts = new List<MessageDraft>();
        
        // Get all sellers for the company
        var sales = await _salesDataSource.GetSalesByCompanyAsync(companyId);
        var sellerIds = sales.Select(s => s.SellerId).Distinct();
        
        foreach (var sellerId in sellerIds)
        {
            var seller = await _salesDataSource.GetSellerByCodeAsync(companyId, sellerId.ToString());
            if (seller != null && !string.IsNullOrEmpty(seller.PhoneE164))
            {
                var message = await GenerateMessageAsync(moment, companyId, null, sellerId);
                
                drafts.Add(new MessageDraft
                {
                    SellerId = sellerId,
                    SellerName = seller.Name,
                    PhoneE164 = seller.PhoneE164,
                    Message = message,
                    Moment = moment
                });
            }
        }
        
        return drafts;
    }

    private string GenerateMorningMessage(InsightsResult insights)
    {
        return $"Bom dia! 🌅\n\n" +
               $"Você está em {insights.GoalProgress:F0}% da sua meta mensal.\n" +
               $"Faltam R$ {insights.GoalGap:F2} para completar o objetivo.\n\n" +
               $"Dica do dia: {insights.Suggestions.FirstOrDefault() ?? "Mantenha o foco!"}\n\n" +
               $"Vamos com tudo! 💪";
    }

    private string GenerateMiddayMessage(InsightsResult insights)
    {
        return $"Hora do almoço! ⏰\n\n" +
               $"Como está indo? Ticket médio hoje: R$ {insights.AvgTicket:F2}\n\n" +
               $"Lembre-se: cada venda conta! Continue firme. 🎯";
    }

    private string GenerateEveningMessage(InsightsResult insights)
    {
        return $"Fim de expediente! 🌙\n\n" +
               $"Vendas hoje: R$ {insights.TotalSales:F2}\n" +
               $"Progresso da meta: {insights.GoalProgress:F0}%\n\n" +
               $"Ótimo trabalho! Descanse bem e até amanhã! 👏";
    }
}
