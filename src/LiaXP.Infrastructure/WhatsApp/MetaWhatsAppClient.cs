using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace LiaXP.Infrastructure.WhatsApp;

public class MetaWhatsAppClient : IWhatsAppClient
{
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly string _phoneId;
    private readonly string _appSecret;

    public MetaWhatsAppClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _token = configuration["Meta:Token"] 
            ?? throw new InvalidOperationException("Meta:Token not configured");
        _phoneId = configuration["Meta:PhoneId"] 
            ?? throw new InvalidOperationException("Meta:PhoneId not configured");
        _appSecret = configuration["Meta:AppSecret"] 
            ?? throw new InvalidOperationException("Meta:AppSecret not configured");
        
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", _token);
    }

    public async Task<SendMessageResult> SendMessageAsync(string toPhoneE164, string message, Guid companyId)
    {
        try
        {
            var to = toPhoneE164.Replace("whatsapp:", "").Replace("+", "");
            
            var payload = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "text",
                text = new { body = message }
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var url = $"https://graph.facebook.com/v18.0/{_phoneId}/messages";
            var response = await _httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                // TODO: Parse response and extract message ID
                return new SendMessageResult
                {
                    Success = true,
                    ExternalId = "meta-message-id"
                };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return new SendMessageResult
                {
                    Success = false,
                    ErrorMessage = $"Meta error: {error}"
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
        // TODO: Implement Meta webhook signature validation (HMAC SHA256)
        await Task.CompletedTask;
        return true;
    }

    public string GetProviderName() => "Meta";
}
