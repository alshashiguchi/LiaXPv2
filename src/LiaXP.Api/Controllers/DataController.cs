using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiaXP.Application.UseCases;
using LiaXP.Domain.Interfaces;
using LiaXP.Application.UseCases.Data;

namespace LiaXP.Api.Controllers;

[ApiController]
[Route("data")]
[Authorize(Policy = "RequireAdminOrManager")]
public class DataController : ControllerBase
{
    private readonly ImportDataUseCase _importDataUseCase;
    private readonly IImportExcelUseCase _importExcelUseCase;
    private readonly ILogger<DataController> _logger;

    public DataController(ImportDataUseCase importDataUseCase, ILogger<DataController> logger, IImportExcelUseCase importExcelUseCase)
    {
        _importDataUseCase = importDataUseCase;
        _logger = logger;
        _importExcelUseCase = importExcelUseCase;
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

    /// <summary>
    /// Importa dados de vendas, metas e equipe a partir de arquivo Excel
    /// </summary>
    /// <param name="file">Arquivo Excel (.xlsx)</param>
    /// <param name="retrain">Se deve retreinar insights após importação</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da importação</returns>
    /// <response code="200">Importação realizada com sucesso</response>
    /// <response code="400">Arquivo inválido</response>
    /// <response code="401">Não autenticado</response>
    /// <response code="403">Sem permissão (requer Admin ou Manager)</response>
    [HttpPost("import/xlsx")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportExcel(
        IFormFile file,
        [FromQuery] bool retrain = false,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Arquivo inválido",
                Detail = "Nenhum arquivo foi enviado"
            });
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Formato inválido",
                Detail = "Apenas arquivos .xlsx são suportados"
            });
        }

        var companyCode = User.FindFirst("company_code")?.Value
            ?? throw new UnauthorizedAccessException("Company code não encontrado");

        using var stream = file.OpenReadStream();
        var result = await _importExcelUseCase.ExecuteAsync(stream, companyCode, retrain, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Erro na importação",
                Detail = result.ErrorMessage
            });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Obtém o status da última importação
    /// </summary>
    /// <returns>Status da importação</returns>
    [HttpGet("statusImport")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatusImport()
    {
        // TODO: Implementar busca de status da última importação
        return Ok(new
        {
            LastImport = DateTime.UtcNow.AddHours(-2),
            Status = "completed",
            RecordsProcessed = 150
        });
    }
}
