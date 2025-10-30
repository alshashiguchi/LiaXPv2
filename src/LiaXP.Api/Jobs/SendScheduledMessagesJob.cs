using LiaXP.Application.UseCases.Messages;
using LiaXP.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace LiaXP.Api.Jobs;

/// <summary>
/// Job responsible for processing and sending scheduled messages at specific times.
/// Integrates with LiaXP's HITL (Human-in-the-Loop) workflow and multi-company architecture.
/// </summary>
/// <remarks>
/// This job follows LiaXP's Clean Architecture principles:
/// - Uses IServiceProvider for scoped dependency resolution
/// - Supports multi-company isolation via companyCode
/// - Integrates with HITL review workflow
/// - Logs structured data for observability
/// - Handles cancellation gracefully
/// </remarks>
[DisallowConcurrentExecution]
public class SendScheduledMessagesJob : IJob
{
    private readonly ILogger<SendScheduledMessagesJob> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SendScheduledMessagesJob(
        ILogger<SendScheduledMessagesJob> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var jobKey = context.JobDetail.Key;
        var startTime = DateTime.UtcNow;

        try
        {
            // Extract parameters from JobDataMap
            var moment = ExtractParameter<string>(context, "moment");
            var companyId = ExtractParameter<Guid>(context, "companyId");

            _logger.LogInformation(
                "🚀 [Job: {JobKey}] Starting scheduled message generation | Moment: {Moment} | CompanyId: {CompanyId}",
                jobKey,
                moment,
                companyId
            );

            // Create a new scope for this job execution (following DI best practices)
            using var scope = _serviceProvider.CreateScope();

            // Execute business logic
            var result = await ExecuteBusinessLogicAsync(
                scope.ServiceProvider,
                moment,
                companyId,
                context.CancellationToken
            );

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "✅ [Job: {JobKey}] Completed successfully | Messages generated: {MessagesGenerated} | Messages sent: {MessagesSent} | Duration: {DurationMs}ms",
                jobKey,
                result.MessagesGenerated,
                result.MessagesSent,
                duration.TotalMilliseconds
            );

            // Store result in context for monitoring/observability
            context.Result = new JobExecutionResult
            {
                Success = true,
                MessagesGenerated = result.MessagesGenerated,
                MessagesSent = result.MessagesSent,
                Duration = duration,
                ExecutedAt = startTime,
                CompanyId = companyId,
                Moment = moment
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ [Job: {JobKey}] Execution cancelled", jobKey);
            throw;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            _logger.LogError(
                ex,
                "❌ [Job: {JobKey}] Execution failed | Duration: {DurationMs}ms | Error: {ErrorMessage}",
                jobKey,
                duration.TotalMilliseconds,
                ex.Message
            );

            // Re-throw as JobExecutionException so Quartz can handle retry logic
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }

    /// <summary>
    /// Executes the core business logic for scheduled message generation.
    /// Integrates with LiaXP's HITL workflow and multi-company architecture.
    /// </summary>
    private async Task<ExecutionResult> ExecuteBusinessLogicAsync(
        IServiceProvider scopedProvider,
        string moment,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var messagesGenerated = 0;
        var messagesSent = 0;

        try
        {
            // Parse moment type
            if (!Enum.TryParse<MomentType>(moment, ignoreCase: true, out var momentType))
            {
                throw new InvalidOperationException($"Invalid moment type: {moment}");
            }

            // ============================================================
            // STEP 1: Generate messages (creates pending reviews in HITL)
            // ============================================================
            _logger.LogDebug("📝 Generating messages for moment: {Moment}", moment);

            var generateUseCase = scopedProvider.GetRequiredService<IGenerateScheduledMessagesUseCase>();
            var generationResult = await generateUseCase.ExecuteAsync(momentType, companyId, cancellationToken);

            if (!generationResult.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to generate messages: {generationResult.ErrorMessage}");
            }

            messagesGenerated = generationResult.MessagesQueued;

            _logger.LogInformation(
                "📊 Generated {Count} messages for review | Moment: {Moment} | CompanyId: {CompanyId}",
                messagesGenerated,
                moment,
                companyId
            );

            // ============================================================
            // STEP 2: Send pre-approved messages (if auto-send is enabled)
            // ============================================================
            // Note: In LiaXP, messages go through HITL review first
            // Only auto-approved messages (if configured) are sent immediately

            if (generationResult.AutoApproved)
            {
                _logger.LogDebug("📤 Auto-approve enabled, sending messages immediately");

                var sendUseCase = scopedProvider.GetRequiredService<ISendApprovedMessagesUseCase>();
                var sendResult = await sendUseCase.ExecuteAsync(companyId, moment, cancellationToken);

                if (!sendResult.Success)
                {
                    _logger.LogWarning(
                        "⚠️ Some messages failed to send | Error: {Error}",
                        sendResult.ErrorMessage
                    );
                }

                messagesSent = sendResult.MessagesSent;

                _logger.LogInformation(
                    "📨 Sent {Sent} messages | Failed: {Failed} | CompanyId: {CompanyId}",
                    sendResult.MessagesSent,
                    sendResult.MessagesFailed,
                    companyId
                );
            }
            else
            {
                _logger.LogInformation(
                    "⏸️ Messages queued for manual review (HITL enabled) | Count: {Count}",
                    messagesGenerated
                );
            }

            return new ExecutionResult
            {
                MessagesGenerated = messagesGenerated,
                MessagesSent = messagesSent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error in business logic | Moment: {Moment} | CompanyId: {CompanyId}",
                moment,
                companyId
            );
            throw;
        }
    }

    /// <summary>
    /// Extracts and validates a typed parameter from the JobDataMap.
    /// </summary>
    private T ExtractParameter<T>(IJobExecutionContext context, string key)
    {
        var dataMap = context.JobDetail.JobDataMap;

        if (!dataMap.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"Required parameter '{key}' not found in JobDataMap"
            );
        }

        var value = dataMap.Get(key);

        try
        {
            if (typeof(T) == typeof(Guid))
            {
                return (T)(object)Guid.Parse(
                    value?.ToString() ?? throw new InvalidOperationException($"Parameter '{key}' is null")
                );
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)(value?.ToString() ?? throw new InvalidOperationException($"Parameter '{key}' is null"));
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to convert parameter '{key}' to type {typeof(T).Name}",
                ex
            );
        }
    }

    /// <summary>
    /// Internal result object for tracking execution metrics.
    /// </summary>
    private sealed class ExecutionResult
    {
        public int MessagesGenerated { get; init; }
        public int MessagesSent { get; init; }
    }
}

/// <summary>
/// Represents the execution result for monitoring, observability, and metrics.
/// Can be accessed via IJobExecutionContext.Result after job completion.
/// </summary>
public sealed class JobExecutionResult
{
    public bool Success { get; init; }
    public int MessagesGenerated { get; init; }
    public int MessagesSent { get; init; }
    public TimeSpan Duration { get; init; }
    public DateTime ExecutedAt { get; init; }
    public Guid CompanyId { get; init; }
    public string Moment { get; init; } = string.Empty;
}
