using System.Diagnostics;
using Dapper;
using LiaXP.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiaXP.Infrastructure.Services;

/// <summary>
/// Service responsible for training the sales insights model.
/// Training involves recalculating insights, invalidating cache, and updating training status.
/// </summary>
public class ModelTrainingService : IModelTrainingService
{
    private readonly string _connectionString;
    private readonly IInsightsService _insightsService;
    private readonly ISalesDataSource _salesDataSource;
    private readonly ILogger<ModelTrainingService> _logger;

    public ModelTrainingService(
        IConfiguration configuration,
        IInsightsService insightsService,
        ISalesDataSource salesDataSource,
        ILogger<ModelTrainingService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _insightsService = insightsService;
        _salesDataSource = salesDataSource;
        _logger = logger;
    }

    public async Task<ModelTrainingResult> TrainAsync(
        Guid companyId,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ModelTrainingResult
        {
            CompanyId = companyId,
            TrainedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation(
                "🧠 Starting model training | CompanyId: {CompanyId} | Force: {Force}",
                companyId,
                force
            );

            // 1. Check if training is needed
            if (!force)
            {
                var isNeeded = await IsTrainingNeededAsync(companyId, cancellationToken);
                if (!isNeeded)
                {
                    stopwatch.Stop();
                    result.Duration = stopwatch.Elapsed;
                    result.Success = true;
                    result.Message = "Training skipped - data hasn't changed since last training";

                    _logger.LogInformation(
                        "⏭️  Training skipped (no changes) | CompanyId: {CompanyId}",
                        companyId
                    );

                    return result;
                }
            }

            // 2. Get current import status
            var importStatus = await GetImportStatusAsync(companyId, cancellationToken);
            if (importStatus == null)
            {
                result.Success = false;
                result.Message = "No import data found for company";
                result.Errors.Add("Company has not imported any data yet");
                return result;
            }

            result.FileHash = importStatus.FileHash;

            // 3. Get all active sellers for the company
            var sales = await _salesDataSource.GetSalesByCompanyAsync(
                companyId,
                DateTime.UtcNow.AddMonths(-3), // Last 3 months
                DateTime.UtcNow
            );

            var sellerIds = sales
                .Select(s => s.SellerId)
                .Distinct()
                .ToList();

            _logger.LogInformation(
                "📊 Found {SellerCount} sellers to process | CompanyId: {CompanyId}",
                sellerIds.Count,
                companyId
            );

            // 4. Calculate insights for each seller
            foreach (var sellerId in sellerIds)
            {
                try
                {
                    var insights = await _insightsService.CalculateInsightsAsync(
                        companyId,
                        null,
                        sellerId
                    );

                    // Cache the insights
                    await _insightsService.CacheInsightsAsync(
                        companyId,
                        insights,
                        null,
                        sellerId
                    );

                    result.SellersProcessed++;
                    result.InsightsGenerated++;
                    result.CacheEntriesCreated++;

                    _logger.LogDebug(
                        "✅ Insights calculated | SellerId: {SellerId} | Sales: {Sales:F2} | Progress: {Progress:F1}%",
                        sellerId,
                        insights.TotalSales,
                        insights.GoalProgress
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Error calculating insights for seller | SellerId: {SellerId}",
                        sellerId
                    );
                    result.Errors.Add($"Failed to process seller {sellerId}: {ex.Message}");
                }

                // Prevent overwhelming the system
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("⚠️  Training cancelled by user");
                    result.Success = false;
                    result.Message = "Training cancelled";
                    return result;
                }
            }

            // 5. Calculate company-level insights
            try
            {
                var companyInsights = await _insightsService.CalculateInsightsAsync(
                    companyId
                );

                await _insightsService.CacheInsightsAsync(
                    companyId,
                    companyInsights
                );

                result.InsightsGenerated++;
                result.CacheEntriesCreated++;

                _logger.LogInformation(
                    "📈 Company insights | Total Sales: {Sales:F2} | Progress: {Progress:F1}%",
                    companyInsights.TotalSales,
                    companyInsights.GoalProgress
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error calculating company insights | CompanyId: {CompanyId}",
                    companyId
                );
                result.Errors.Add($"Failed to process company insights: {ex.Message}");
            }

            // 6. Update training status
            await UpdateTrainingStatusAsync(
                companyId,
                importStatus.FileHash,
                cancellationToken
            );

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = result.Errors.Count == 0;
            result.Message = result.Success
                ? $"Training completed successfully in {result.Duration.TotalSeconds:F2}s"
                : $"Training completed with {result.Errors.Count} errors";

            _logger.LogInformation(
                "✅ Training completed | CompanyId: {CompanyId} | " +
                "Sellers: {Sellers} | Insights: {Insights} | Duration: {Duration:F2}s | Errors: {Errors}",
                companyId,
                result.SellersProcessed,
                result.InsightsGenerated,
                result.Duration.TotalSeconds,
                result.Errors.Count
            );

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = false;
            result.Message = $"Training failed: {ex.Message}";
            result.Errors.Add(ex.Message);

            _logger.LogError(
                ex,
                "❌ Critical error during training | CompanyId: {CompanyId}",
                companyId
            );

            return result;
        }
    }

    public async Task<bool> IsTrainingNeededAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var status = await GetTrainingStatusAsync(companyId, cancellationToken);

        if (status == null)
        {
            _logger.LogDebug("Training needed - no import status found");
            return false; // No data imported yet
        }

        var needed = status.TrainingNeeded;

        _logger.LogDebug(
            "Training needed check | CompanyId: {CompanyId} | Needed: {Needed} | " +
            "Stale: {Stale} | CurrentHash: {CurrentHash} | LastHash: {LastHash}",
            companyId,
            needed,
            status.IsStale,
            status.CurrentFileHash,
            status.LastTrainedHash
        );

        return needed;
    }

    public async Task<TrainingStatus?> GetTrainingStatusAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT 
                CompanyId,
                FileHash as CurrentFileHash,
                LastTrainedHash,
                LastTrainedAt,
                IsStale
            FROM ImportStatus
            WHERE CompanyId = @CompanyId
              AND IsDeleted = 0
            ORDER BY ImportedAt DESC";

        var status = await connection.QueryFirstOrDefaultAsync<TrainingStatus>(
            new CommandDefinition(
                sql,
                new { CompanyId = companyId },
                cancellationToken: cancellationToken
            )
        );

        return status;
    }

    // ============================================================
    // Private Helper Methods
    // ============================================================

    private async Task<ImportStatusDto?> GetImportStatusAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT TOP 1
                Id, CompanyId, FileHash, ImportedAt,
                LastTrainedHash, LastTrainedAt, IsStale
            FROM ImportStatus
            WHERE CompanyId = @CompanyId
              AND IsDeleted = 0
            ORDER BY ImportedAt DESC";

        return await connection.QueryFirstOrDefaultAsync<ImportStatusDto>(
            new CommandDefinition(
                sql,
                new { CompanyId = companyId },
                cancellationToken: cancellationToken
            )
        );
    }

    private async Task UpdateTrainingStatusAsync(
        Guid companyId,
        string fileHash,
        CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            UPDATE ImportStatus
            SET LastTrainedHash = @FileHash,
                LastTrainedAt = GETUTCDATE(),
                IsStale = 0,
                UpdatedAt = GETUTCDATE()
            WHERE CompanyId = @CompanyId
              AND IsDeleted = 0";

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { CompanyId = companyId, FileHash = fileHash },
                cancellationToken: cancellationToken
            )
        );

        _logger.LogDebug(
            "Training status updated | CompanyId: {CompanyId} | Hash: {Hash} | Rows: {Rows}",
            companyId,
            fileHash,
            affected
        );
    }

    // ============================================================
    // DTOs
    // ============================================================

    private class ImportStatusDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; }
        public string? LastTrainedHash { get; set; }
        public DateTime? LastTrainedAt { get; set; }
        public bool IsStale { get; set; }
    }
}