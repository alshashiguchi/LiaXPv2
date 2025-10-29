using LiaXP.Domain.Entities;
using LiaXP.Domain.Enums;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using static LiaXP.Domain.Interfaces.IAIService;

namespace LiaXP.Infrastructure.Services;

public class OpenAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI API Key não configurada");
        _model = configuration["OpenAI:Model"] ?? "gpt-4";
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<AIResponse> ProcessMessageAsync(
        string message,
        string companyCode,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var intent = DetectIntent(message);
            var systemPrompt = BuildSystemPrompt(intent, context);

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = message }
                },
                temperature = 0.7,
                max_tokens = 500
            };

            var response = await _httpClient.PostAsJsonAsync(
                "chat/completions",
                requestBody,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIResponse>(
                cancellationToken: cancellationToken);

            var assistantMessage = result?.Choices?.FirstOrDefault()?.Message?.Content
                ?? "Desculpe, não consegui processar sua mensagem.";

            return new AIResponse
            {
                Message = assistantMessage,
                Intent = intent,
                Metadata = new Dictionary<string, string>
                {
                    { "model", _model },
                    { "company_code", companyCode }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem com OpenAI");
            return new AIResponse
            {
                Message = "Desculpe, ocorreu um erro ao processar sua mensagem. Tente novamente.",
                Intent = IntentType.Unknown,
                Metadata = new Dictionary<string, string>()
            };
        }
    }

    public IntentType DetectIntent(string message) 
    {
        var lowerMessage = message.ToLowerInvariant();

        if (lowerMessage.Contains("meta") && (lowerMessage.Contains("falta") || lowerMessage.Contains("faltam")))
            return IntentType.GoalGap; 

        if (lowerMessage.Contains("dica") || lowerMessage.Contains("como vender"))
            return IntentType.PersonalizedTips; 

        if (lowerMessage.Contains("ranking") || lowerMessage.Contains("top"))
            return IntentType.Ranking; 

        if (lowerMessage.Contains("como está") && !lowerMessage.Contains("equipe"))
            return IntentType.SellerPerformance; 

        if (lowerMessage.Contains("motivação") || lowerMessage.Contains("motivacional"))
            return IntentType.TeamMotivation; 

        if (lowerMessage.Contains("produto") || lowerMessage.Contains("categoria"))
            return IntentType.ProductHelp; 

        return IntentType.GeneralQuestion; 
    }

    private string BuildSystemPrompt(IntentType intent, Dictionary<string, object>? context)
    {
        var basePrompt = @"Você é a LIA, uma assistente virtual especializada em vendas de cosméticos. 
Seu objetivo é ajudar vendedores e gerentes a atingir suas metas através de insights, 
dicas práticas e motivação. Seja objetiva, positiva e empática.";

        return intent switch
        {
            IntentType.GoalGap => basePrompt + @"
O usuário está perguntando sobre o gap para a meta. 
Use os dados fornecidos para calcular quanto falta e dê insights sobre como alcançar.",

            IntentType.PersonalizedTips => basePrompt + @"
O usuário precisa de dicas de abordagem de vendas. 
Forneça técnicas consultivas, focadas em benefícios e experiência do cliente.",

            IntentType.Ranking => basePrompt + @"
O usuário quer ver o ranking de vendas. 
Apresente os dados de forma motivacional, celebrando os destaques.",

            IntentType.TeamMotivation => basePrompt + @"
Crie uma mensagem motivacional curta e impactante para a equipe.",

            _ => basePrompt
        };
    }

    private class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}