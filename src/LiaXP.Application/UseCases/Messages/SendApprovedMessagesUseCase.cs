using LiaXP.Domain.Entities;
using LiaXP.Domain.Enums;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases.Messages;

/// <summary>
/// Use Case: Send approved messages via WhatsApp
/// </summary>
public interface ISendApprovedMessagesUseCase
{
    Task<SendMessagesResult> ExecuteAsync(
        Guid companyId,
        string? moment = null,
        CancellationToken cancellationToken = default);
}

public class SendApprovedMessagesUseCase : ISendApprovedMessagesUseCase
{
    private readonly IReviewService _reviewService;
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly IMessageLogRepository _messageLogRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendApprovedMessagesUseCase> _logger;

    public SendApprovedMessagesUseCase(
        IReviewService reviewService,
        IWhatsAppClient whatsAppClient,
        IMessageLogRepository messageLogRepository,
        IConfiguration configuration,
        ILogger<SendApprovedMessagesUseCase> logger)
    {
        _reviewService = reviewService;
        _whatsAppClient = whatsAppClient;
        _messageLogRepository = messageLogRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SendMessagesResult> ExecuteAsync(
        Guid companyId,
        string? moment = null,
        CancellationToken cancellationToken = default)
    {
        var result = new SendMessagesResult { CompanyId = companyId };

        try
        {
            _logger.LogInformation(
                "Sending approved messages | CompanyId: {CompanyId} | Moment: {Moment}",
                companyId,
                moment ?? "all"
            );

            // 1. Get approved messages that haven't been sent yet
            var approvedMessages = await _reviewService.GetPendingReviewsAsync(companyId);

            // Filter by moment if specified
            var messagesToSend = approvedMessages
                .Where(m => m.Status == "Approved" && m.SentAt == null)
                .Where(m => moment == null || m.Moment.Equals(moment, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!messagesToSend.Any())
            {
                _logger.LogInformation(
                    "No approved messages to send | CompanyId: {CompanyId}",
                    companyId
                );
                result.Success = true;
                return result;
            }

            _logger.LogInformation(
                "Found {Count} approved messages to send | CompanyId: {CompanyId}",
                messagesToSend.Count,
                companyId
            );

            // 2. Send each message via WhatsApp
            foreach (var review in messagesToSend)
            {
                try
                {
                    // Determine which message to send (edited or original)
                    var messageToSend = !string.IsNullOrWhiteSpace(review.EditedMessage)
                        ? review.EditedMessage
                        : review.DraftMessage;

                    // Send via WhatsApp
                    var sendResult = await _whatsAppClient.SendMessageAsync(
                        review.RecipientPhone,
                        messageToSend,
                        companyId
                    );

                    if (sendResult.Success)
                    {
                        result.MessagesSent++;

                        // Log successful send
                        await _messageLogRepository.SaveAsync(new MessageLog
                        {
                            Id = Guid.NewGuid(),
                            CompanyId = companyId,
                            Direction = "Outbound",
                            PhoneFrom = "system",
                            PhoneTo = review.RecipientPhone,
                            Message = messageToSend,
                            Provider = _whatsAppClient.GetProviderName(),
                            ExternalId = sendResult.ExternalId,
                            Status = "Sent",
                            SentAt = DateTime.UtcNow
                        }, cancellationToken);

                        // Update review status
                        // Note: This should be done through ReviewService
                        // await _reviewService.MarkAsSentAsync(review.Id, sendResult.ExternalId);

                        _logger.LogDebug(
                            "Message sent successfully | ReviewId: {ReviewId} | Phone: {Phone}",
                            review.Id,
                            review.RecipientPhone
                        );
                    }
                    else
                    {
                        result.MessagesFailed++;

                        // Log failed send
                        await _messageLogRepository.SaveAsync(new MessageLog
                        {
                            Id = Guid.NewGuid(),
                            CompanyId = companyId,
                            Direction = "Outbound",
                            PhoneFrom = "system",
                            PhoneTo = review.RecipientPhone,
                            Message = messageToSend,
                            Provider = _whatsAppClient.GetProviderName(),
                            Status = "Failed",
                            ErrorMessage = sendResult.ErrorMessage,
                            SentAt = DateTime.UtcNow
                        }, cancellationToken);

                        _logger.LogWarning(
                            "Failed to send message | ReviewId: {ReviewId} | Error: {Error}",
                            review.Id,
                            sendResult.ErrorMessage
                        );
                    }
                }
                catch (Exception ex)
                {
                    result.MessagesFailed++;

                    _logger.LogError(
                        ex,
                        "Exception while sending message | ReviewId: {ReviewId}",
                        review.Id
                    );
                }

                // Add small delay to avoid rate limiting
                await Task.Delay(200, cancellationToken);
            }

            result.Success = true;

            _logger.LogInformation(
                "Message sending completed | Sent: {Sent} | Failed: {Failed} | CompanyId: {CompanyId}",
                result.MessagesSent,
                result.MessagesFailed,
                companyId
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending approved messages | CompanyId: {CompanyId}",
                companyId
            );

            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
}

/// <summary>
/// Result object for message sending
/// </summary>
public class SendMessagesResult
{
    public bool Success { get; set; }
    public Guid CompanyId { get; set; }
    public int MessagesSent { get; set; }
    public int MessagesFailed { get; set; }
    public string? ErrorMessage { get; set; }
}
