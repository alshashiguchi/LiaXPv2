namespace LiaXP.Domain.Interfaces.Excel;

public interface IExcelImportService
{
    Task<ImportResult> ImportAsync(
        Stream fileStream,
        string companyCode,
        CancellationToken cancellationToken = default);
}

public class ImportResult
{
    public bool Success { get; set; }
    public int SalesCount { get; set; }
    public int GoalsCount { get; set; }
    public int TeamCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }
}