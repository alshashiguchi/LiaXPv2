namespace LiaXP.Application.DTOs.Training;

/// <summary>
/// Request to train the model
/// </summary>
public record TrainModelRequest
{
    /// <summary>
    /// Force retraining even if data hasn't changed
    /// </summary>
    public bool Force { get; init; } = false;
}

/// <summary>
/// Response from training operation
/// </summary>
public record TrainModelResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid CompanyId { get; init; }
    public string FileHash { get; init; } = string.Empty;
    public DateTime TrainedAt { get; init; }
    public int SellersProcessed { get; init; }
    public int InsightsGenerated { get; init; }
    public int CacheEntriesCreated { get; init; }
    public double DurationSeconds { get; init; }
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Training status response
/// </summary>
public record TrainingStatusResponse
{
    public Guid CompanyId { get; init; }
    public string? CurrentFileHash { get; init; }
    public string? LastTrainedHash { get; init; }
    public DateTime? LastTrainedAt { get; init; }
    public bool IsStale { get; init; }
    public bool TrainingNeeded { get; init; }
    public string Status { get; init; } = string.Empty;
}