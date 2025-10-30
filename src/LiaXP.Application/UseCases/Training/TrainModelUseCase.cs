using LiaXP.Application.DTOs.Auth;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases.Training;

/// <summary>
/// Use Case: Train/Retrain the sales insights model
/// </summary>
public interface ITrainModelUseCase
{
    Task<Result<ModelTrainingResult>> ExecuteAsync(
        Guid companyId,
        bool force = false,
        CancellationToken cancellationToken = default);
}

public class TrainModelUseCase : ITrainModelUseCase
{
    private readonly IModelTrainingService _trainingService;
    private readonly ILogger<TrainModelUseCase> _logger;

    public TrainModelUseCase(
        IModelTrainingService trainingService,
        ILogger<TrainModelUseCase> logger)
    {
        _trainingService = trainingService;
        _logger = logger;
    }

    public async Task<Result<ModelTrainingResult>> ExecuteAsync(
        Guid companyId,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "🎯 Executing model training use case | CompanyId: {CompanyId} | Force: {Force}",
                companyId,
                force
            );

            // Execute training
            var result = await _trainingService.TrainAsync(
                companyId,
                force,
                cancellationToken
            );

            if (result.Success)
            {
                _logger.LogInformation(
                    "✅ Training completed successfully | CompanyId: {CompanyId} | " +
                    "Sellers: {Sellers} | Insights: {Insights} | Duration: {Duration:F2}s",
                    companyId,
                    result.SellersProcessed,
                    result.InsightsGenerated,
                    result.Duration.TotalSeconds
                );

                return Result<ModelTrainingResult>.Success(result);
            }
            else
            {
                _logger.LogWarning(
                    "⚠️  Training completed with errors | CompanyId: {CompanyId} | Errors: {ErrorCount}",
                    companyId,
                    result.Errors.Count
                );

                return Result<ModelTrainingResult>.Success(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error in training use case | CompanyId: {CompanyId}",
                companyId
            );

            return Result<ModelTrainingResult>.Failure(
                $"Error training model: {ex.Message}"
            );
        }
    }
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? data, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}