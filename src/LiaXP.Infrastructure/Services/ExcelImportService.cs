using ClosedXML.Excel;
using Dapper;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiaXP.Infrastructure.Services;

public class ExcelImportService : IExcelImportService
{
    private readonly string _connectionString;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(IConfiguration configuration, ILogger<ExcelImportService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _logger = logger;
    }

    public async Task<ImportResult> ImportAsync(
        Stream fileStream,
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        var result = new ImportResult { Success = false };

        try
        {
            using var workbook = new XLWorkbook(fileStream);

            // Importar vendas
            if (workbook.TryGetWorksheet("Vendas", out var salesSheet))
            {
                result.SalesCount = await ImportSalesAsync(salesSheet, companyCode, cancellationToken);
            }

            // Importar metas
            if (workbook.TryGetWorksheet("Metas", out var goalsSheet))
            {
                result.GoalsCount = await ImportGoalsAsync(goalsSheet, companyCode, cancellationToken);
            }

            // Importar equipe
            if (workbook.TryGetWorksheet("Equipe", out var teamSheet))
            {
                result.TeamCount = await ImportTeamAsync(teamSheet, companyCode, cancellationToken);
            }

            result.Success = true;
            result.Message = "Importação concluída com sucesso";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante importação");
            result.Errors.Add(ex.Message);
            result.Message = "Erro durante importação";
        }

        return result;
    }

    private async Task<int> ImportSalesAsync(
        IXLWorksheet sheet,
        string companyCode,
        CancellationToken cancellationToken)
    {
        var rows = sheet.RowsUsed().Skip(1); // Pular cabeçalho
        var salesData = new List<SalesData>();

        foreach (var row in rows)
        {
            try
            {
                var sale = new SalesData(
                    companyCode,
                    row.Cell(1).GetValue<DateTime>(),  // data
                    row.Cell(2).GetValue<string>(),    // loja
                    row.Cell(3).GetValue<string>(),    // vendedora (código)
                    row.Cell(3).GetValue<string>(),    // vendedora (nome)
                    row.Cell(4).GetValue<decimal>(),   // valor_total
                    row.Cell(5).GetValue<int>(),       // qtd_itens
                    row.Cell(6).GetValue<decimal>(),   // ticket_medio
                    row.Cell(7).GetValue<string>()     // categoria
                );

                salesData.Add(sale);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao processar linha {Row} da planilha Vendas", row.RowNumber());
            }
        }

        // Bulk insert
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO SalesData (Id, CompanyCode, Date, Store, SellerCode, SellerName, 
                                    TotalValue, ItemsQty, AvgTicket, Category, ImportedAt)
            VALUES (@Id, @CompanyCode, @Date, @Store, @SellerCode, @SellerName, 
                    @TotalValue, @ItemsQty, @AvgTicket, @Category, @ImportedAt)";

        foreach (var sale in salesData)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new
                {
                    sale.Id,
                    sale.CompanyCode,
                    sale.Date,
                    sale.Store,
                    sale.SellerCode,
                    sale.SellerName,
                    sale.TotalValue,
                    sale.ItemsQty,
                    sale.AvgTicket,
                    sale.Category,
                    sale.ImportedAt
                }, cancellationToken: cancellationToken));
        }

        return salesData.Count;
    }

    private async Task<int> ImportGoalsAsync(
        IXLWorksheet sheet,
        string companyCode,
        CancellationToken cancellationToken)
    {
        // Implementação similar ao ImportSalesAsync
        // ...
        return 0;
    }

    private async Task<int> ImportTeamAsync(
        IXLWorksheet sheet,
        string companyCode,
        CancellationToken cancellationToken)
    {
        // Implementação similar ao ImportSalesAsync
        // ...
        return 0;
    }
}