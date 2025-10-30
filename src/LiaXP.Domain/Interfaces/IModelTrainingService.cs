using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

/// <summary>
/// Service responsible for training/retraining the sales insights model.
/// Training involves:
/// 1. Recalculating insights for all sellers
/// 2. Invalidating stale cache
/// 3. Updating ImportStatus with training metadata
/// </summary>
public interface IModelTrainingService
{
    /// <summary>
    /// Train the model for a specific company
    /// </summary>
    /// <param name="companyId">Company to train</param>
    /// <param name="force">Force retraining even if data hasn't changed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training result with statistics</returns>
    Task<ModelTrainingResult> TrainAsync(
        Guid companyId,
        bool force = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if training is needed (data has changed since last training)
    /// </summary>
    Task<bool> IsTrainingNeededAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get training status for a company
    /// </summary>
    Task<TrainingStatus?> GetTrainingStatusAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of model training operation
/// </summary>
public class ModelTrainingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public DateTime TrainedAt { get; set; }
    public int SellersProcessed { get; set; }
    public int InsightsGenerated { get; set; }
    public int CacheEntriesCreated { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Training status information
/// </summary>
public class TrainingStatus
{
    public Guid CompanyId { get; set; }
    public string? CurrentFileHash { get; set; }
    public string? LastTrainedHash { get; set; }
    public DateTime? LastTrainedAt { get; set; }
    public bool IsStale { get; set; }
    public bool TrainingNeeded => CurrentFileHash != LastTrainedHash || IsStale;
}