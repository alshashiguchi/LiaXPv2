using LiaXP.Domain.Entities;
using LiaXP.Domain.Enums;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases.Messages;

/// <summary>
/// Use Case: Generate scheduled messages and queue them for review (HITL workflow)
/// </summary>
public interface IGenerateScheduledMessagesUseCase
{
    Task<GenerateMessagesResult> ExecuteAsync(
        MomentType moment,
        Guid companyId,
        CancellationToken cancellationToken = default);
}

public class GenerateScheduledMessagesUseCase : IGenerateScheduledMessagesUseCase
{
    private readonly ITemplateService _templateService;
    private readonly IReviewService _reviewService;
    private readonly ISalesDataSource _salesDataSource;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GenerateScheduledMessagesUseCase> _logger;

    public GenerateScheduledMessagesUseCase(
        ITemplateService templateService,
        IReviewService reviewService,
        ISalesDataSource salesDataSource,
        IConfiguration configuration,
        ILogger<GenerateScheduledMessagesUseCase> logger)
    {
        _templateService = templateService;
        _reviewService = reviewService;
        _salesDataSource = salesDataSource;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GenerateMessagesResult> ExecuteAsync(
        MomentType moment,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var result = new GenerateMessagesResult { CompanyId = companyId, Moment = moment };

        try
        {
            _logger.LogInformation(
                "Generating scheduled messages | Moment: {Moment} | CompanyId: {CompanyId}",
                moment,
                companyId
            );

            // 1. Generate message drafts for all active sellers
            var drafts = await _templateService.GenerateAllMessagesAsync(moment, companyId);

            if (drafts == null || !drafts.Any())
            {
                _logger.LogWarning(
                    "No message drafts generated | CompanyId: {CompanyId}",
                    companyId
                );
                return result;
            }

            _logger.LogInformation(
                "Generated {Count} message drafts | CompanyId: {CompanyId}",
                drafts.Count,
                companyId
            );

            // 2. Check HITL configuration
            var reviewRequired = _configuration.GetValue<bool>("HITL:ReviewRequired", true);

            if (reviewRequired)
            {
                // 3a. Queue messages for review (HITL workflow)
                foreach (var draft in drafts)
                {
                    var reviewQueue = new ReviewQueue
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        Moment = moment.ToString(),
                        RecipientPhone = draft.PhoneE164,
                        RecipientName = draft.SellerName,
                        DraftMessage = draft.Message,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    // ✅ Save to review queue using the service
                    try
                    {
                        await _reviewService.CreateReviewAsync(reviewQueue, cancellationToken);
                        result.MessagesQueued++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to queue message for review | Recipient: {RecipientName} | Phone: {Phone}",
                            draft.SellerName,
                            draft.PhoneE164
                        );
                        result.FailedMessages++;
                    }
                }

                _logger.LogInformation(
                    "Queued {Queued} messages for review ({Failed} failed) | CompanyId: {CompanyId}",
                    result.MessagesQueued,
                    result.FailedMessages,
                    companyId
                );
            }
            else
            {
                // 3b. Auto-approve (skip HITL)
                _logger.LogInformation(
                    "HITL disabled, messages will be auto-approved | CompanyId: {CompanyId}",
                    companyId
                );

                result.MessagesQueued = drafts.Count;
                result.AutoApproved = true;
            }

            result.Success = result.MessagesQueued > 0;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generating scheduled messages | Moment: {Moment} | CompanyId: {CompanyId}",
                moment,
                companyId
            );

            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
}

/// <summary>
/// Result object for message generation
/// </summary>
public class GenerateMessagesResult
{
    public bool Success { get; set; }
    public Guid CompanyId { get; set; }
    public MomentType Moment { get; set; }
    public int MessagesQueued { get; set; }
    public int FailedMessages { get; set; }
    public bool AutoApproved { get; set; }
    public string? ErrorMessage { get; set; }
}
