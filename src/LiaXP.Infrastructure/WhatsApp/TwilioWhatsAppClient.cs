using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace LiaXP.Infrastructure.WhatsApp;

public class TwilioWhatsAppClient : IWhatsAppClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _from;

    public TwilioWhatsAppClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _accountSid = configuration["Twilio:AccountSid"] 
            ?? throw new InvalidOperationException("Twilio:AccountSid not configured");
        _authToken = configuration["Twilio:AuthToken"] 
            ?? throw new InvalidOperationException("Twilio:AuthToken not configured");
        _from = configuration["Twilio:From"] ?? "whatsapp:+14155238886";
        
        var authBytes = Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(authBytes));
    }

    public async Task<SendMessageResult> SendMessageAsync(string toPhoneE164, string message, Guid companyId)
    {
        try
        {
            var to = toPhoneE164.StartsWith("whatsapp:") ? toPhoneE164 : $"whatsapp:{toPhoneE164}";
            
            var values = new Dictionary<string, string>
            {
                { "From", _from },
                { "To", to },
                { "Body", message }
            };
            
            var content = new FormUrlEncodedContent(values);
            var url = $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json";
            
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                // TODO: Parse response and extract message SID
                return new SendMessageResult
                {
                    Success = true,
                    ExternalId = "twilio-message-sid"
                };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return new SendMessageResult
                {
                    Success = false,
                    ErrorMessage = $"Twilio error: {error}"
                };
            }
        }
        catch (Exception ex)
        {
            return new SendMessageResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> ValidateWebhookAsync(string signature, string payload)
    {
        // TODO: Implement Twilio signature validation
        await Task.CompletedTask;
        return true;
    }

    public string GetProviderName() => "Twilio";
}
