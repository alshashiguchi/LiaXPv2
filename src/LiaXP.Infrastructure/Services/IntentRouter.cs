using System.Diagnostics;
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
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var (intent, confidence) = ClassifyIntent(message);
            var seller = await _salesDataSource.GetSellerByPhoneAsync(companyId, phoneE164);

            if (seller == null)
            {
                stopwatch.Stop();
                return new IntentResult
                {
                    Intent = IntentType.Unknown,
                    Response = "Desculpe, não consegui identificar seu perfil. Entre em contato com o administrador.",
                    Success = true,
                    Confidence = 0.0,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
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

            stopwatch.Stop();

            return new IntentResult
            {
                Intent = intent,
                Response = response,
                Success = true,
                Confidence = confidence,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new IntentResult
            {
                Intent = IntentType.Unknown,
                Success = false,
                ErrorMessage = ex.Message,
                Confidence = 0.0,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Classifica a intenção da mensagem usando pattern matching
    /// </summary>
    /// <returns>Tupla com a intenção e nível de confiança (0.0 a 1.0)</returns>
    private (IntentType intent, double confidence) ClassifyIntent(string message)
    {
        var lowerMessage = message.ToLower();
        var words = lowerMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Dicionário de keywords por intenção
        var intentKeywords = new Dictionary<IntentType, string[]>
        {
            { IntentType.GoalGap, new[] { "meta", "falta", "objetivo", "quanto falta" } },
            { IntentType.PersonalizedTips, new[] { "dica", "ajuda", "melhorar", "como", "sugestão" } },
            { IntentType.Ranking, new[] { "ranking", "posição", "colocação", "lugar" } },
            { IntentType.Focus, new[] { "foco", "priorizar", "concentrar", "prioridade" } },
            { IntentType.AvgTicket, new[] { "ticket", "ticket médio", "valor médio" } }
        };

        // Calcula confiança baseado em matches de keywords
        var bestMatch = intentKeywords
            .Select(kvp => new
            {
                Intent = kvp.Key,
                Matches = kvp.Value.Count(keyword => lowerMessage.Contains(keyword)),
                TotalKeywords = kvp.Value.Length
            })
            .Where(x => x.Matches > 0)
            .OrderByDescending(x => x.Matches)
            .FirstOrDefault();

        if (bestMatch == null)
        {
            return (IntentType.Unknown, 0.0);
        }

        // Confiança baseada na proporção de keywords encontradas
        // Ajuste: se encontrou todas as keywords, confiança de 0.95
        // Se encontrou metade, confiança de 0.7, etc.
        var confidence = Math.Min(0.95, 0.5 + (bestMatch.Matches / (double)bestMatch.TotalKeywords * 0.45));

        return (bestMatch.Intent, confidence);
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