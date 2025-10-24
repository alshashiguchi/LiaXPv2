using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiaXP.Application.UseCases;

namespace LiaXP.Api.Controllers;

[ApiController]
[Route("data")]
[Authorize(Policy = "RequireAdminOrManager")]
public class DataController : ControllerBase
{
    private readonly ImportDataUseCase _importDataUseCase;
    private readonly ILogger<DataController> _logger;

    public DataController(ImportDataUseCase importDataUseCase, ILogger<DataController> logger)
    {
        _importDataUseCase = importDataUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Import data from Excel file
    /// </summary>
    [HttpPost("import/xlsx")]
    public async Task<IActionResult> ImportExcel(
        [FromForm] IFormFile file,
        [FromQuery] bool retrain = false)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "File is required" });

        if (!file.FileName.EndsWith(".xlsx"))
            return BadRequest(new { error = "Only .xlsx files are supported" });

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
            return BadRequest(new { error = "File size exceeds 10MB limit" });

        var companyCode = HttpContext.Items["CompanyCode"]?.ToString();
        if (string.IsNullOrEmpty(companyCode))
            return Unauthorized(new { error = "Company code not found" });

        try
        {
            using var stream = file.OpenReadStream();
            
            // TODO: Get actual company ID from code
            var companyId = Guid.NewGuid(); // Placeholder
            
            var result = await _importDataUseCase.ExecuteAsync(stream, companyId, retrain);
            
            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = new
                    {
                        fileHash = result.FileHash,
                        stores = result.StoresImported,
                        sellers = result.SellersImported,
                        goals = result.GoalsImported,
                        sales = result.SalesImported
                    }
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Excel file");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get import/training status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        // TODO: Implement status retrieval
        return Ok(new
        {
            lastImport = DateTime.UtcNow.AddHours(-2),
            lastTrain = DateTime.UtcNow.AddHours(-1),
            isStale = false
        });
    }
}
