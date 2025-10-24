using LiaXP.Domain.Interfaces;
using LiaXP.Domain.Entities;
using ClosedXML.Excel;
using System.Security.Cryptography;
using System.Text;

namespace LiaXP.Infrastructure.Services;

public class ExcelDataImporter : IDataImporter
{
    private readonly ISalesDataSource _salesDataSource;

    public ExcelDataImporter(ISalesDataSource salesDataSource)
    {
        _salesDataSource = salesDataSource;
    }

    public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream, Guid companyId, bool retrain = false)
    {
        var result = new ImportResult { Success = true };
        
        try
        {
            // Compute file hash
            fileStream.Position = 0;
            result.FileHash = ComputeFileHash(fileStream);
            fileStream.Position = 0;
            
            using var workbook = new XLWorkbook(fileStream);
            
            // Import Sales
            if (workbook.Worksheets.Contains("Sales"))
            {
                var salesSheet = workbook.Worksheet("Sales");
                var sales = ParseSalesSheet(salesSheet, companyId);
                await _salesDataSource.UpsertSalesAsync(sales);
                result.SalesImported = sales.Count();
            }
            
            // Import Goals
            if (workbook.Worksheets.Contains("Goals"))
            {
                var goalsSheet = workbook.Worksheet("Goals");
                var goals = ParseGoalsSheet(goalsSheet, companyId);
                await _salesDataSource.UpsertGoalsAsync(goals);
                result.GoalsImported = goals.Count();
            }
            
            // Import Team
            if (workbook.Worksheets.Contains("Team"))
            {
                var teamSheet = workbook.Worksheet("Team");
                var sellers = ParseTeamSheet(teamSheet, companyId);
                foreach (var seller in sellers)
                {
                    await _salesDataSource.UpsertSellerAsync(seller);
                }
                result.SellersImported = sellers.Count();
            }
            
            result.Message = "Import completed successfully";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            result.Errors.Add(ex.Message);
        }
        
        return result;
    }

    public string ComputeFileHash(Stream fileStream)
    {
        using var sha256 = SHA256.Create();
        fileStream.Position = 0;
        var hash = sha256.ComputeHash(fileStream);
        fileStream.Position = 0;
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private IEnumerable<Sale> ParseSalesSheet(IXLWorksheet sheet, Guid companyId)
    {
        var sales = new List<Sale>();
        var firstRow = sheet.FirstRowUsed().RowNumber();
        var lastRow = sheet.LastRowUsed().RowNumber();
        
        for (int row = firstRow + 1; row <= lastRow; row++)
        {
            try
            {
                var sale = new Sale
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    StoreId = Guid.NewGuid(), // TODO: Lookup store
                    SellerId = Guid.NewGuid(), // TODO: Lookup seller
                    SaleDate = sheet.Cell(row, 1).GetDateTime(),
                    TotalValue = sheet.Cell(row, 5).GetValue<decimal>(),
                    ItemsQty = sheet.Cell(row, 6).GetValue<int>(),
                    AvgTicket = sheet.Cell(row, 7).GetValue<decimal>(),
                    Category = sheet.Cell(row, 8).GetString()
                };
                sales.Add(sale);
            }
            catch
            {
                // Skip invalid rows
            }
        }
        
        return sales;
    }

    private IEnumerable<Goal> ParseGoalsSheet(IXLWorksheet sheet, Guid companyId)
    {
        var goals = new List<Goal>();
        var firstRow = sheet.FirstRowUsed().RowNumber();
        var lastRow = sheet.LastRowUsed().RowNumber();
        
        for (int row = firstRow + 1; row <= lastRow; row++)
        {
            try
            {
                var goal = new Goal
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    StoreId = Guid.NewGuid(), // TODO: Lookup store
                    SellerId = Guid.NewGuid(), // TODO: Lookup seller
                    Month = sheet.Cell(row, 1).GetDateTime(),
                    TargetValue = sheet.Cell(row, 4).GetValue<decimal>(),
                    TargetTicket = sheet.Cell(row, 5).GetValue<decimal?>(),
                    TargetConversion = sheet.Cell(row, 6).GetValue<decimal?>()
                };
                goals.Add(goal);
            }
            catch
            {
                // Skip invalid rows
            }
        }
        
        return goals;
    }

    private IEnumerable<Seller> ParseTeamSheet(IXLWorksheet sheet, Guid companyId)
    {
        var sellers = new List<Seller>();
        var firstRow = sheet.FirstRowUsed().RowNumber();
        var lastRow = sheet.LastRowUsed().RowNumber();
        
        for (int row = firstRow + 1; row <= lastRow; row++)
        {
            try
            {
                var seller = new Seller
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    StoreId = Guid.NewGuid(), // TODO: Lookup store
                    SellerCode = sheet.Cell(row, 1).GetString(),
                    Name = sheet.Cell(row, 2).GetString(),
                    PhoneE164 = sheet.Cell(row, 4).GetString(),
                    Status = sheet.Cell(row, 5).GetString() ?? "Active"
                };
                sellers.Add(seller);
            }
            catch
            {
                // Skip invalid rows
            }
        }
        
        return sellers;
    }
}
