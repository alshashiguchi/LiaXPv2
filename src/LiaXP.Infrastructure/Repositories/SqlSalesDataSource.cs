using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Dapper;
using Microsoft.Data.SqlClient;

namespace LiaXP.Infrastructure.Repositories;

public class SqlSalesDataSource : ISalesDataSource
{
    private readonly string _connectionString;

    public SqlSalesDataSource(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<IEnumerable<Sale>> GetSalesByCompanyAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM Sale 
            WHERE CompanyId = @CompanyId 
            AND IsDeleted = 0
            AND (@StartDate IS NULL OR SaleDate >= @StartDate)
            AND (@EndDate IS NULL OR SaleDate <= @EndDate)";
        
        return await connection.QueryAsync<Sale>(sql, new { CompanyId = companyId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<IEnumerable<Sale>> GetSalesBySellerAsync(Guid sellerId, DateTime? startDate = null, DateTime? endDate = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM Sale 
            WHERE SellerId = @SellerId 
            AND IsDeleted = 0
            AND (@StartDate IS NULL OR SaleDate >= @StartDate)
            AND (@EndDate IS NULL OR SaleDate <= @EndDate)";
        
        return await connection.QueryAsync<Sale>(sql, new { SellerId = sellerId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<IEnumerable<Sale>> GetSalesByStoreAsync(Guid storeId, DateTime? startDate = null, DateTime? endDate = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM Sale 
            WHERE StoreId = @StoreId 
            AND IsDeleted = 0
            AND (@StartDate IS NULL OR SaleDate >= @StartDate)
            AND (@EndDate IS NULL OR SaleDate <= @EndDate)";
        
        return await connection.QueryAsync<Sale>(sql, new { StoreId = storeId, StartDate = startDate, EndDate = endDate });
    }

    public async Task UpsertSalesAsync(IEnumerable<Sale> sales)
    {
        using var connection = new SqlConnection(_connectionString);
        
        foreach (var sale in sales)
        {
            var sql = @"
                MERGE Sale AS target
                USING (SELECT @Id AS Id) AS source
                ON target.Id = source.Id
                WHEN MATCHED THEN
                    UPDATE SET 
                        CompanyId = @CompanyId,
                        StoreId = @StoreId,
                        SellerId = @SellerId,
                        SaleDate = @SaleDate,
                        TotalValue = @TotalValue,
                        ItemsQty = @ItemsQty,
                        AvgTicket = @AvgTicket,
                        Category = @Category,
                        UpdatedAt = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (Id, CompanyId, StoreId, SellerId, SaleDate, TotalValue, ItemsQty, AvgTicket, Category, CreatedAt)
                    VALUES (@Id, @CompanyId, @StoreId, @SellerId, @SaleDate, @TotalValue, @ItemsQty, @AvgTicket, @Category, GETUTCDATE());";
            
            await connection.ExecuteAsync(sql, sale);
        }
    }

    public async Task<IEnumerable<Goal>> GetGoalsByCompanyAsync(Guid companyId, DateTime? month = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM Goal 
            WHERE CompanyId = @CompanyId 
            AND IsDeleted = 0
            AND (@Month IS NULL OR Month = @Month)";
        
        return await connection.QueryAsync<Goal>(sql, new { CompanyId = companyId, Month = month });
    }

    public async Task<IEnumerable<Goal>> GetGoalsBySellerAsync(Guid sellerId, DateTime? month = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM Goal 
            WHERE SellerId = @SellerId 
            AND IsDeleted = 0
            AND (@Month IS NULL OR Month = @Month)";
        
        return await connection.QueryAsync<Goal>(sql, new { SellerId = sellerId, Month = month });
    }

    public async Task UpsertGoalsAsync(IEnumerable<Goal> goals)
    {
        using var connection = new SqlConnection(_connectionString);
        
        foreach (var goal in goals)
        {
            var sql = @"
                MERGE Goal AS target
                USING (SELECT @CompanyId AS CompanyId, @SellerId AS SellerId, @Month AS Month) AS source
                ON target.CompanyId = source.CompanyId AND target.SellerId = source.SellerId AND target.Month = source.Month
                WHEN MATCHED THEN
                    UPDATE SET 
                        StoreId = @StoreId,
                        TargetValue = @TargetValue,
                        TargetTicket = @TargetTicket,
                        TargetConversion = @TargetConversion,
                        UpdatedAt = GETUTCDATE()
                WHEN NOT MATCHED THEN
                    INSERT (Id, CompanyId, StoreId, SellerId, Month, TargetValue, TargetTicket, TargetConversion, CreatedAt)
                    VALUES (@Id, @CompanyId, @StoreId, @SellerId, @Month, @TargetValue, @TargetTicket, @TargetConversion, GETUTCDATE());";
            
            await connection.ExecuteAsync(sql, goal);
        }
    }

    public async Task<Company?> GetCompanyByCodeAsync(string code)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Company WHERE Code = @Code AND IsDeleted = 0";
        return await connection.QueryFirstOrDefaultAsync<Company>(sql, new { Code = code });
    }

    public async Task<Store?> GetStoreByNameAsync(Guid companyId, string name)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Store WHERE CompanyId = @CompanyId AND Name = @Name AND IsDeleted = 0";
        return await connection.QueryFirstOrDefaultAsync<Store>(sql, new { CompanyId = companyId, Name = name });
    }

    public async Task<Seller?> GetSellerByCodeAsync(Guid companyId, string sellerCode)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Seller WHERE CompanyId = @CompanyId AND SellerCode = @SellerCode AND IsDeleted = 0";
        return await connection.QueryFirstOrDefaultAsync<Seller>(sql, new { CompanyId = companyId, SellerCode = sellerCode });
    }

    public async Task<Seller?> GetSellerByPhoneAsync(Guid companyId, string phoneE164)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM Seller WHERE CompanyId = @CompanyId AND PhoneE164 = @PhoneE164 AND IsDeleted = 0";
        return await connection.QueryFirstOrDefaultAsync<Seller>(sql, new { CompanyId = companyId, PhoneE164 = phoneE164 });
    }

    public async Task UpsertCompanyAsync(Company company)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            MERGE Company AS target
            USING (SELECT @Code AS Code) AS source
            ON target.Code = source.Code
            WHEN MATCHED THEN
                UPDATE SET Name = @Name, Description = @Description, IsActive = @IsActive, UpdatedAt = GETUTCDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, Code, Name, Description, IsActive, CreatedAt)
                VALUES (@Id, @Code, @Name, @Description, @IsActive, GETUTCDATE());";
        
        await connection.ExecuteAsync(sql, company);
    }

    public async Task UpsertStoreAsync(Store store)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            MERGE Store AS target
            USING (SELECT @CompanyId AS CompanyId, @Name AS Name) AS source
            ON target.CompanyId = source.CompanyId AND target.Name = source.Name
            WHEN MATCHED THEN
                UPDATE SET Address = @Address, Phone = @Phone, IsActive = @IsActive, UpdatedAt = GETUTCDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, CompanyId, Name, Address, Phone, IsActive, CreatedAt)
                VALUES (@Id, @CompanyId, @Name, @Address, @Phone, @IsActive, GETUTCDATE());";
        
        await connection.ExecuteAsync(sql, store);
    }

    public async Task UpsertSellerAsync(Seller seller)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            MERGE Seller AS target
            USING (SELECT @CompanyId AS CompanyId, @SellerCode AS SellerCode) AS source
            ON target.CompanyId = source.CompanyId AND target.SellerCode = source.SellerCode
            WHEN MATCHED THEN
                UPDATE SET 
                    StoreId = @StoreId,
                    Name = @Name,
                    PhoneE164 = @PhoneE164,
                    Email = @Email,
                    Status = @Status,
                    UpdatedAt = GETUTCDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, CompanyId, StoreId, SellerCode, Name, PhoneE164, Email, Status, CreatedAt)
                VALUES (@Id, @CompanyId, @StoreId, @SellerCode, @Name, @PhoneE164, @Email, @Status, GETUTCDATE());";
        
        await connection.ExecuteAsync(sql, seller);
    }

    public async Task<Seller?> GetSellerByPhoneAsync(string phoneE164)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
        SELECT s.*, c.Code as CompanyCode, c.Name as CompanyName
        FROM Seller s
        INNER JOIN Company c ON s.CompanyId = c.Id
        WHERE s.PhoneE164 = @PhoneE164 
          AND s.IsDeleted = 0
          AND s.Status = 'Active'
          AND c.IsActive = 1";

        return await connection.QueryFirstOrDefaultAsync<Seller>(
            sql,
            new { PhoneE164 = phoneE164 }
        );
    }
}
