using LiaXP.Application.UseCases.Auth;
using LiaXP.Domain.Interfaces.Excel;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases.Data;

public interface IImportExcelUseCase
{
    Task<Result<ImportResult>> ExecuteAsync(
        Stream fileStream,
        string companyCode,
        bool retrain = false,
        CancellationToken cancellationToken = default);
}

public class ImportExcelUseCase : IImportExcelUseCase
{
    private readonly IExcelImportService _importService;
    private readonly ILogger<ImportExcelUseCase> _logger;

    public ImportExcelUseCase(
        IExcelImportService importService,
        ILogger<ImportExcelUseCase> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    public async Task<Result<ImportResult>> ExecuteAsync(
        Stream fileStream,
        string companyCode,
        bool retrain = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando importação de dados para company: {CompanyCode}", companyCode);

            var result = await _importService.ImportAsync(fileStream, companyCode, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Importação concluída. Vendas: {Sales}, Metas: {Goals}, Equipe: {Team}",
                    result.SalesCount,
                    result.GoalsCount,
                    result.TeamCount);

                // Se retrain=true, disparar retreinamento de insights aqui
                if (retrain)
                {
                    _logger.LogInformation("Retreinamento solicitado após importação");
                    // TODO: Implementar retreinamento
                }
            }

            return Result<ImportResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao importar dados para company: {CompanyCode}", companyCode);
            return Result<ImportResult>.Failure("Erro ao importar dados");
        }
    }
}