using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases;

public class ImportDataUseCase
{
    private readonly IDataImporter _dataImporter;
    private readonly ILogger<ImportDataUseCase> _logger;

    public ImportDataUseCase(IDataImporter dataImporter, ILogger<ImportDataUseCase> logger)
    {
        _dataImporter = dataImporter;
        _logger = logger;
    }

    public async Task<ImportResult> ExecuteAsync(Stream fileStream, Guid companyId, bool retrain = false)
    {
        try
        {
            _logger.LogInformation("Starting data import for company {CompanyId}", companyId);
            
            var result = await _dataImporter.ImportFromExcelAsync(fileStream, companyId, retrain);
            
            if (result.Success)
            {
                _logger.LogInformation("Data import completed successfully for company {CompanyId}. " +
                    "Imported: {Stores} stores, {Sellers} sellers, {Goals} goals, {Sales} sales",
                    companyId, result.StoresImported, result.SellersImported, 
                    result.GoalsImported, result.SalesImported);
            }
            else
            {
                _logger.LogWarning("Data import completed with errors for company {CompanyId}: {Message}",
                    companyId, result.Message);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing data for company {CompanyId}", companyId);
            return new ImportResult
            {
                Success = false,
                Message = $"Error importing data: {ex.Message}",
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
