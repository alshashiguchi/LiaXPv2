using Microsoft.AspNetCore.Mvc;
using LiaXP.Application.UseCases;
using LiaXP.Application.DTOs.Webhook;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;

namespace LiaXP.Api.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IIntentRouter _intentRouter;
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly ISalesDataSource _salesDataSource;
    private readonly IMessageLogRepository _messageLogRepository;

    public WebhookController(ILogger<WebhookController> logger, IConfiguration configuration, IIntentRouter intentRouter, IWhatsAppClient whatsAppClient, ISalesDataSource salesDataSource, IMessageLogRepository messageLogRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _intentRouter = intentRouter;
        _whatsAppClient = whatsAppClient;
        _salesDataSource = salesDataSource;
        _messageLogRepository = messageLogRepository;
    }

    /// <summary>
    /// Verificação do webhook do WhatsApp (Meta)
    /// </summary>
    [HttpGet("whatsapp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string verifyToken)
    {
        var expectedToken = _configuration["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && verifyToken == expectedToken)
        {
            _logger.LogInformation("Webhook verificado com sucesso");
            return Ok(challenge);
        }

        _logger.LogWarning("Falha na verificação do webhook");
        return StatusCode(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Recebe mensagens do WhatsApp
    /// </summary>
    [HttpPost("whatsapp")]
    public async Task<IActionResult> ReceiveMessage(
       [FromBody] WhatsAppWebhookRequest payload)
    {
        try
        {
            // 1. Extrair dados da mensagem
            var message = payload.Entry?[0]?.Changes?[0]?.Value?.Messages?[0];
            if (message == null)
            {
                _logger.LogWarning("Webhook recebido sem mensagem válida");
                return Ok(); // WhatsApp espera 200 mesmo sem processar
            }

            var fromPhone = NormalizePhone(message.From);
            var messageText = message.Text?.Body;

            if (string.IsNullOrWhiteSpace(messageText))
            {
                _logger.LogWarning("Mensagem vazia recebida de {Phone}", fromPhone);
                return Ok();
            }

            _logger.LogInformation(
                "📩 Mensagem recebida | Phone: {Phone} | Text: {Text}",
                fromPhone,
                messageText
            );

            // 2. ✅ BUSCAR VENDEDOR PELO TELEFONE (SEM COMPANY)
            var seller = await _salesDataSource.GetSellerByPhoneAsync(fromPhone);

            if (seller == null)
            {
                _logger.LogWarning(
                    "⚠️ Vendedor não encontrado para phone: {Phone}",
                    fromPhone
                );

                await _whatsAppClient.SendMessageAsync(
                    fromPhone,
                    "Desculpe, não encontrei seu cadastro no sistema. " +
                    "Entre em contato com seu administrador para verificar seu número cadastrado.",
                    Guid.Empty // Sem company para mensagens de erro
                );

                return Ok();
            }

            // 3. ✅ AGORA TEMOS O COMPANY ID!
            var companyId = seller.CompanyId;

            _logger.LogInformation(
                "✅ Vendedor identificado | Seller: {SellerName} | Company: {CompanyId}",
                seller.Name,
                companyId
            );

            // 4. Processar intent e gerar resposta
            var result = await _intentRouter.RouteMessageAsync(
                messageText,
                companyId,
                fromPhone
            );

            if (!result.Success)
            {
                _logger.LogError(
                    "❌ Erro ao processar intent | Error: {Error}",
                    result.ErrorMessage
                );

                await _whatsAppClient.SendMessageAsync(
                    fromPhone,
                    "Desculpe, ocorreu um erro ao processar sua mensagem. Tente novamente.",
                    companyId
                );

                return Ok();
            }

            // 5. Enviar resposta via WhatsApp
            var sendResult = await _whatsAppClient.SendMessageAsync(
                fromPhone,
                result.Response,
                companyId
            );

            if (!sendResult.Success)
            {
                _logger.LogError(
                    "❌ Erro ao enviar mensagem | Error: {Error}",
                    sendResult.ErrorMessage
                );
            }

            // 6. Registrar log da conversa (Inbound + Outbound)
            await _messageLogRepository.SaveAsync(new MessageLog
            {
                CompanyId = companyId,
                Direction = "Inbound",
                PhoneFrom = fromPhone,
                PhoneTo = sendResult.ExternalId ?? "system",
                Message = messageText,
                Provider = _whatsAppClient.GetProviderName(),
                Status = "Received",
                SentAt = DateTime.UtcNow
            });

            await _messageLogRepository.SaveAsync(new MessageLog
            {
                CompanyId = companyId,
                Direction = "Outbound",
                PhoneFrom = "system",
                PhoneTo = fromPhone,
                Message = result.Response,
                Provider = _whatsAppClient.GetProviderName(),
                ExternalId = sendResult.ExternalId,
                Status = sendResult.Success ? "Sent" : "Failed",
                ErrorMessage = sendResult.ErrorMessage,
                SentAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "✅ Mensagem processada com sucesso | Intent: {Intent} | Confidence: {Confidence}",
                result.Intent,
                result.Confidence
            );

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro crítico ao processar webhook WhatsApp");

            // WhatsApp exige 200 mesmo em caso de erro
            // para não reenviar a mensagem
            return Ok();
        }
    }

    /// <summary>
    /// Normaliza telefone para formato E.164
    /// </summary>
    private static string NormalizePhone(string phone)
    {
        // Remove tudo exceto números
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

        // Se não começa com +, adiciona + e código do país (assumindo Brasil)
        if (!phone.StartsWith("+"))
        {
            return $"+{digitsOnly}";
        }

        return phone;
    }
}
