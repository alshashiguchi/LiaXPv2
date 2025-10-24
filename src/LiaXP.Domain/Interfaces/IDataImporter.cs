namespace LiaXP.Domain.Interfaces;

public interface IDataImporter
{
    Task<ImportResult> ImportFromExcelAsync(Stream fileStream, Guid companyId, bool retrain = false);
    string ComputeFileHash(Stream fileStream);
}

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public int CompaniesImported { get; set; }
    public int StoresImported { get; set; }
    public int SellersImported { get; set; }
    public int GoalsImported { get; set; }
    public int SalesImported { get; set; }
    public List<string> Errors { get; set; } = new();
}
