namespace LiaXP.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; private set; }
    public string CompanyCode { get; private set; }
    public Guid UserId { get; private set; }
    public string UserMessage { get; private set; }
    public string AssistantResponse { get; private set; }
    public ChatIntent Intent { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; }

    private ChatMessage() { }

    public ChatMessage(
        string companyCode,
        Guid userId,
        string userMessage,
        string assistantResponse,
        ChatIntent intent,
        Dictionary<string, string>? metadata = null)
    {
        Id = Guid.NewGuid();
        CompanyCode = companyCode;
        UserId = userId;
        UserMessage = userMessage;
        AssistantResponse = assistantResponse;
        Intent = intent;
        CreatedAt = DateTime.UtcNow;
        Metadata = metadata ?? new Dictionary<string, string>();
    }
}

public enum ChatIntent
{
    Unknown = 0,
    GoalGap = 1,          // "quanto falta pra meta?"
    Tips = 2,             // "dica de abordagem"
    Ranking = 3,          // "ranking de vendas"
    SellerPerformance = 4, // "como está a Ana?"
    TeamMotivation = 5,    // "mensagem motivacional"
    ProductHelp = 6,       // "como vender skincare?"
    GeneralQuestion = 7    // perguntas gerais
}