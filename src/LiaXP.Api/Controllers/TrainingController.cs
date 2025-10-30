using LiaXP.Application.DTOs.Training;
using LiaXP.Application.UseCases.Training;
using LiaXP.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiaXP.Api.Controllers;

/// <summary>
/// Training controller for model training operations
/// </summary>
[Authorize(Roles = "Admin,Manager")]
[ApiController]
[Route("training")]
[Produces("application/json")]
public class TrainingController : ControllerBase
{
    private readonly ITrainModelUseCase _trainModelUseCase;
    private readonly IModelTrainingService _trainingService;
    private readonly ILogger<TrainingController> _logger;

    public TrainingController(
        ITrainModelUseCase trainModelUseCase,
        IModelTrainingService trainingService,
        ILogger<TrainingController> logger)
    {
        _trainModelUseCase = trainModelUseCase;
        _trainingService = trainingService;
        _logger = logger;
    }

    /// <summary>
    /// Train the insights model for the company
    /// </summary>
    /// <param name="request">Training configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Training result</returns>
    [HttpPost("train")]
    [ProducesResponseType(typeof(TrainModelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Train(
        [FromBody] TrainModelRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var companyId = GetCompanyIdFromClaims();
            request ??= new TrainModelRequest();

            _logger.LogInformation(
                "🎯 Training request received | CompanyId: {CompanyId} | Force: {Force}",
                companyId,
                request.Force
            );

            var result = await _trainModelUseCase.ExecuteAsync(
                companyId,
                request.Force,
                cancellationToken
            );

            if (!result.IsSuccess || result.Data == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Training failed",
                    Detail = result.ErrorMessage
                });
            }

            var response = new TrainModelResponse
            {
                Success = result.Data.Success,
                Message = result.Data.Message,
                CompanyId = result.Data.CompanyId,
                FileHash = result.Data.FileHash,
                TrainedAt = result.Data.TrainedAt,
                SellersProcessed = result.Data.SellersProcessed,
                InsightsGenerated = result.Data.InsightsGenerated,
                CacheEntriesCreated = result.Data.CacheEntriesCreated,
                DurationSeconds = result.Data.Duration.TotalSeconds,
                Errors = result.Data.Errors
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing training request");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail = "An error occurred while training the model"
            });
        }
    }

    /// <summary>
    /// Get training status for the company
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TrainingStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        try
        {
            var companyId = GetCompanyIdFromClaims();

            var status = await _trainingService.GetTrainingStatusAsync(
                companyId,
                cancellationToken
            );

            if (status == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Training status not found",
                    Detail = "No import data found for this company"
                });
            }

            var response = new TrainingStatusResponse
            {
                CompanyId = status.CompanyId,
                CurrentFileHash = status.CurrentFileHash,
                LastTrainedHash = status.LastTrainedHash,
                LastTrainedAt = status.LastTrainedAt,
                IsStale = status.IsStale,
                TrainingNeeded = status.TrainingNeeded,
                Status = status.TrainingNeeded ? "Training needed" : "Up to date"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving training status");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail = "An error occurred while retrieving training status"
            });
        }
    }

    /// <summary>
    /// Check if training is needed
    /// </summary>
    [HttpGet("check")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckTrainingNeeded(CancellationToken cancellationToken)
    {
        try
        {
            var companyId = GetCompanyIdFromClaims();

            var needed = await _trainingService.IsTrainingNeededAsync(
                companyId,
                cancellationToken
            );

            return Ok(new
            {
                companyId,
                trainingNeeded = needed,
                message = needed
                    ? "Training is needed - data has changed or is stale"
                    : "Training is up to date"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error checking training status");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal server error",
                Detail = "An error occurred while checking training status"
            });
        }
    }

    /// <summary>
    /// Trigger training manually (convenience endpoint)
    /// </summary>
    [HttpPost("retrain")]
    [ProducesResponseType(typeof(TrainModelResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Retrain(
        [FromQuery] bool force = true,
        CancellationToken cancellationToken = default)
    {
        return await Train(
            new TrainModelRequest { Force = force },
            cancellationToken
        );
    }

    private Guid GetCompanyIdFromClaims()
    {
        var companyCodeClaim = User.FindFirst("company_code")?.Value;
        if (string.IsNullOrEmpty(companyCodeClaim))
        {
            throw new UnauthorizedAccessException("Company code not found in token");
        }

        // TODO: In production, lookup company ID by code or add to JWT claims
        var companyIdFromContext = HttpContext.Items["CompanyId"] as Guid?;
        if (companyIdFromContext.HasValue)
        {
            return companyIdFromContext.Value;
        }

        throw new UnauthorizedAccessException("Company ID not found");
    }
}