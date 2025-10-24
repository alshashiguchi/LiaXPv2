using LiaXP.Domain.Interfaces;
using LiaXP.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace LiaXP.Infrastructure.Cron;

public class CronService : ICronService
{
    private readonly ITemplateService _templateService;
    private readonly IReviewService _reviewService;
    private readonly IConfiguration _configuration;

    public CronService(
        ITemplateService templateService,
        IReviewService reviewService,
        IConfiguration configuration)
    {
        _templateService = templateService;
        _reviewService = reviewService;
        _configuration = configuration;
    }

    public async Task ExecuteScheduledJobAsync(MomentType moment, Guid companyId, bool sendImmediately = false)
    {
        // Generate message drafts for all sellers
        var drafts = await _templateService.GenerateAllMessagesAsync(moment, companyId);
        
        var reviewRequired = _configuration.GetValue<bool>("HITL:ReviewRequired", true);
        
        if (reviewRequired && !sendImmediately)
        {
            // Queue for review
            foreach (var draft in drafts)
            {
                // TODO: Add to ReviewQueue table
            }
        }
        else
        {
            // Send immediately (auto-approve)
            foreach (var draft in drafts)
            {
                // TODO: Send via WhatsAppClient and log
            }
        }
    }
}
