using Dapper;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiaXP.Infrastructure.Repositories;

public class MessageLogRepository : IMessageLogRepository
{
    private readonly string _connectionString;
    private readonly ILogger<MessageLogRepository> _logger;

    public MessageLogRepository(
        IConfiguration configuration,
        ILogger<MessageLogRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _logger = logger;
    }

    public async Task SaveAsync(
        MessageLog messageLog,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            INSERT INTO MessageLog (
                Id, CompanyId, Direction, PhoneFrom, PhoneTo, 
                Message, Provider, ExternalId, Status, SentAt, 
                ErrorMessage, CreatedAt
            )
            VALUES (
                @Id, @CompanyId, @Direction, @PhoneFrom, @PhoneTo,
                @Message, @Provider, @ExternalId, @Status, @SentAt,
                @ErrorMessage, @CreatedAt
            )";

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    messageLog.Id,
                    messageLog.CompanyId,
                    messageLog.Direction,
                    messageLog.PhoneFrom,
                    messageLog.PhoneTo,
                    messageLog.Message,
                    messageLog.Provider,
                    messageLog.ExternalId,
                    messageLog.Status,
                    messageLog.SentAt,
                    messageLog.ErrorMessage,
                    messageLog.CreatedAt
                },
                cancellationToken: cancellationToken
            )
        );

        _logger.LogDebug(
            "Message log saved | Direction: {Direction} | Phone: {Phone} | Status: {Status}",
            messageLog.Direction,
            messageLog.Direction == "Inbound" ? messageLog.PhoneFrom : messageLog.PhoneTo,
            messageLog.Status
        );
    }

    public async Task<IEnumerable<MessageLog>> GetByCompanyAsync(
        Guid companyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT TOP (@Limit) *
            FROM MessageLog
            WHERE CompanyId = @CompanyId
              AND IsDeleted = 0
              AND (@StartDate IS NULL OR SentAt >= @StartDate)
              AND (@EndDate IS NULL OR SentAt <= @EndDate)
            ORDER BY SentAt DESC";

        return await connection.QueryAsync<MessageLog>(
            new CommandDefinition(
                sql,
                new
                {
                    CompanyId = companyId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Limit = limit
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<IEnumerable<MessageLog>> GetByPhoneAsync(
        string phoneE164,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT TOP (@Limit) *
            FROM MessageLog
            WHERE (PhoneFrom = @Phone OR PhoneTo = @Phone)
              AND IsDeleted = 0
              AND (@StartDate IS NULL OR SentAt >= @StartDate)
              AND (@EndDate IS NULL OR SentAt <= @EndDate)
            ORDER BY SentAt DESC";

        return await connection.QueryAsync<MessageLog>(
            new CommandDefinition(
                sql,
                new
                {
                    Phone = phoneE164,
                    StartDate = startDate,
                    EndDate = endDate,
                    Limit = limit
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<IEnumerable<MessageLog>> GetFailedMessagesAsync(
        Guid companyId,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT *
            FROM MessageLog
            WHERE CompanyId = @CompanyId
              AND Status = 'Failed'
              AND Direction = 'Outbound'
              AND IsDeleted = 0
              AND (@Since IS NULL OR SentAt >= @Since)
            ORDER BY SentAt DESC";

        return await connection.QueryAsync<MessageLog>(
            new CommandDefinition(
                sql,
                new { CompanyId = companyId, Since = since },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task UpdateStatusAsync(
        Guid messageLogId,
        string status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            UPDATE MessageLog
            SET Status = @Status,
                ErrorMessage = @ErrorMessage,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id";

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    Id = messageLogId,
                    Status = status,
                    ErrorMessage = errorMessage
                },
                cancellationToken: cancellationToken
            )
        );

        _logger.LogDebug(
            "Message log status updated | Id: {MessageLogId} | Status: {Status}",
            messageLogId,
            status
        );
    }
}