using LiaXP.Domain.Interfaces;
using LiaXP.Domain.Enums;

namespace LiaXP.Infrastructure.Services;

public class IntentRouter : IIntentRouter
{
    private readonly IInsightsService _insightsService;
    private readonly ISalesDataSource _salesDataSource;

    public IntentRouter(IInsightsService insightsService, ISalesDataSource salesDataSource)
    {
        _insightsService = insightsService;
        _salesDataSource = salesDataSource;
    }

    public async Task<IntentResult> RouteMessageAsync(string message, Guid companyId, string phoneE164)
    {
        try
        {
            var intent = ClassifyIntent(message);
            var seller = await _salesDataSource.GetSellerByPhoneAsync(companyId, phoneE164);
            
            if (seller == null)
            {
                return new IntentResult
                {
                    Intent = IntentType.Unknown,
                    Response = "Desculpe, não consegui identificar seu perfil. Entre em contato com o administrador.",
                    Success = true
                };
            }
            
            var response = intent switch
            {
                IntentType.GoalGap => await HandleGoalGapIntent(companyId, seller.Id),
                IntentType.PersonalizedTips => await HandleTipsIntent(companyId, seller.Id),
                IntentType.Ranking => await HandleRankingIntent(companyId, seller.Id),
                IntentType.Focus => await HandleFocusIntent(companyId, seller.Id),
                IntentType.AvgTicket => await HandleAvgTicketIntent(companyId, seller.Id),
                _ => "Desculpe, não entendi sua mensagem. Tente perguntar sobre metas, ranking ou dicas."
            };
            
            return new IntentResult
            {
                Intent = intent,
                Response = response,
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new IntentResult
            {
                Intent = IntentType.Unknown,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private IntentType ClassifyIntent(string message)
    {
        var lowerMessage = message.ToLower();
        
        if (lowerMessage.Contains("meta") || lowerMessage.Contains("falta"))
            return IntentType.GoalGap;
        
        if (lowerMessage.Contains("dica") || lowerMessage.Contains("ajuda") || lowerMessage.Contains("melhorar"))
            return IntentType.PersonalizedTips;
        
        if (lowerMessage.Contains("ranking") || lowerMessage.Contains("posição"))
            return IntentType.Ranking;
        
        if (lowerMessage.Contains("foco") || lowerMessage.Contains("priorizar"))
            return IntentType.Focus;
        
        if (lowerMessage.Contains("ticket") || lowerMessage.Contains("ticket médio"))
            return IntentType.AvgTicket;
        
        return IntentType.Unknown;
    }

    private async Task<string> HandleGoalGapIntent(Guid companyId, Guid sellerId)
    {
        var insights = await _insightsService.CalculateInsightsAsync(companyId, null, sellerId);
        return $"Você atingiu {insights.GoalProgress:F1}% da sua meta! Falta R$ {insights.GoalGap:F2} para completar. Continue assim! 💪";
    }

    private async Task<string> HandleTipsIntent(Guid companyId, Guid sellerId)
    {
        var insights = await _insightsService.CalculateInsightsAsync(companyId, null, sellerId);
        var tips = string.Join("\n", insights.Suggestions);
        return $"Aqui estão algumas dicas personalizadas para você:\n\n{tips}";
    }

    private async Task<string> HandleRankingIntent(Guid companyId, Guid sellerId)
    {
        var insights = await _insightsService.CalculateInsightsAsync(companyId);
        var sellerRanking = insights.Rankings.FirstOrDefault(r => r.SellerCode == sellerId.ToString());
        
        if (sellerRanking != null)
        {
            return $"Você está em {sellerRanking.Rank}º lugar com R$ {sellerRanking.TotalSales:F2} em vendas! 🎯";
        }
        
        return "Não consegui encontrar seu ranking no momento.";
    }

    private async Task<string> HandleFocusIntent(Guid companyId, Guid sellerId)
    {
        var insights = await _insightsService.CalculateInsightsAsync(companyId, null, sellerId);
        var focus = string.Join("\n", insights.FocusAreas);
        return $"Áreas de foco para você:\n\n{focus}";
    }

    private async Task<string> HandleAvgTicketIntent(Guid companyId, Guid sellerId)
    {
        var insights = await _insightsService.CalculateInsightsAsync(companyId, null, sellerId);
        return $"Seu ticket médio atual é R$ {insights.AvgTicket:F2}. " +
               $"Sugestões para aumentar: ofereça produtos complementares, faça upsell, e destaque itens premium.";
    }
}
