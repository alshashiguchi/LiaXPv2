namespace LiaXP.Domain.Interfaces;

public interface IWhatsAppClient
{
    Task<SendMessageResult> SendMessageAsync(string toPhoneE164, string message, Guid companyId);
    Task<bool> ValidateWebhookAsync(string signature, string payload);
    string GetProviderName();
}

public class SendMessageResult
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public string? ErrorMessage { get; set; }
}
