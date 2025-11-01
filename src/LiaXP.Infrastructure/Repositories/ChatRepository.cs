using Dapper;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Enums;
using LiaXP.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LiaXP.Infrastructure.Repositories;

/// <summary>
/// Repository for chat message persistence and retrieval
/// </summary>
public class ChatRepository : IChatRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ChatRepository> _logger;

    public ChatRepository(
        IConfiguration configuration,
        ILogger<ChatRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _logger = logger;
    }

    public async Task SaveMessageAsync(
        ChatMessage message,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            INSERT INTO ChatMessage (
                Id, CompanyId, UserId, UserMessage, AssistantResponse, 
                Intent, CreatedAt, Metadata
            )
            VALUES (
                @Id, @CompanyId, @UserId, @UserMessage, @AssistantResponse,
                @Intent, @CreatedAt, @Metadata
            )";

        var metadata = message.Metadata != null
            ? JsonSerializer.Serialize(message.Metadata)
            : null;

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    message.Id,
                    message.CompanyId,
                    message.UserId,
                    message.UserMessage,
                    message.AssistantResponse,
                    Intent = (int)message.Intent,
                    message.CreatedAt,
                    Metadata = metadata
                },
                cancellationToken: cancellationToken
            )
        );

        _logger.LogDebug(
            "Chat message saved | UserId: {UserId} | Intent: {Intent}",
            message.UserId,
            message.Intent
        );
    }

    public async Task<List<ChatMessage>> GetUserHistoryAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT TOP (@Limit)
                Id, CompanyId, UserId, UserMessage, AssistantResponse,
                Intent, CreatedAt, Metadata
            FROM ChatMessage
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC";

        var rows = await connection.QueryAsync<ChatMessageDto>(
            new CommandDefinition(
                sql,
                new { UserId = userId, Limit = limit },
                cancellationToken: cancellationToken
            )
        );

        return rows.Select(MapToEntity).ToList();
    }

    public async Task<List<ChatMessage>> GetByUserAndCompanyAsync(
    Guid userId,
    Guid companyId,
    int limit = 10,
    CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT TOP (@Limit)
                Id, CompanyId, UserId, UserMessage, AssistantResponse,
                Intent, CreatedAt, Metadata
            FROM ChatMessage
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC";

        var rows = await connection.QueryAsync<ChatMessageDto>(
            new CommandDefinition(
                sql,
                new { UserId = userId, CompanyId = companyId, Limit = limit },
                cancellationToken: cancellationToken
            )
        );

        return rows.Select(MapToEntity).ToList();
    }

    public async Task<List<ChatMessage>> GetCompanyHistoryAsync(
        string companyCode,
        DateTime? since = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT TOP (@Limit)
                Id, CompanyId, UserId, UserMessage, AssistantResponse,
                Intent, CreatedAt, Metadata
            FROM ChatMessage
            WHERE CompanyId = @CompanyId
              AND (@Since IS NULL OR CreatedAt >= @Since)
            ORDER BY CreatedAt DESC";

        var rows = await connection.QueryAsync<ChatMessageDto>(
            new CommandDefinition(
                sql,
                new { CompanyId = companyCode, Since = since, Limit = limit },
                cancellationToken: cancellationToken
            )
        );

        return rows.Select(MapToEntity).ToList();
    }

    public async Task<List<ChatMessage>> SearchMessagesAsync(
        string companyCode,
        string searchTerm,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT TOP (@Limit)
                Id, CompanyId, UserId, UserMessage, AssistantResponse,
                Intent, CreatedAt, Metadata
            FROM ChatMessage
            WHERE CompanyId = @CompanyId
              AND (
                UserMessage LIKE @SearchPattern
                OR AssistantResponse LIKE @SearchPattern
              )
            ORDER BY CreatedAt DESC";

        var searchPattern = $"%{searchTerm}%";

        var rows = await connection.QueryAsync<ChatMessageDto>(
            new CommandDefinition(
                sql,
                new { CompanyId = companyCode, SearchPattern = searchPattern, Limit = limit },
                cancellationToken: cancellationToken
            )
        );

        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Dictionary<IntentType, int>> GetIntentStatisticsAsync(
        string companyCode,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT 
                Intent,
                COUNT(*) as Count
            FROM ChatMessage
            WHERE CompanyId = @CompanyId
              AND (@StartDate IS NULL OR CreatedAt >= @StartDate)
              AND (@EndDate IS NULL OR CreatedAt <= @EndDate)
            GROUP BY Intent
            ORDER BY Count DESC";

        var rows = await connection.QueryAsync<(int Intent, int Count)>(
            new CommandDefinition(
                sql,
                new { CompanyId = companyCode, StartDate = startDate, EndDate = endDate },
                cancellationToken: cancellationToken
            )
        );

        return rows.ToDictionary(
            row => (IntentType)row.Intent,
            row => row.Count
        );
    }

    public async Task<int> GetTotalMessagesAsync(
        string companyCode,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT COUNT(*)
            FROM ChatMessage
            WHERE CompanyId = @CompanyId
              AND (@Since IS NULL OR CreatedAt >= @Since)";

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { CompanyId = companyCode, Since = since },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task DeleteOldMessagesAsync(
        DateTime olderThan,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            DELETE FROM ChatMessage
            WHERE CreatedAt < @OlderThan";

        var deleted = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { OlderThan = olderThan },
                cancellationToken: cancellationToken
            )
        );

        _logger.LogInformation(
            "Deleted {Count} old chat messages older than {Date}",
            deleted,
            olderThan
        );
    }

    // ============================================================
    // Private Helpers
    // ============================================================

    private static ChatMessage MapToEntity(ChatMessageDto dto)
    {
        var metadata = string.IsNullOrWhiteSpace(dto.Metadata)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(dto.Metadata)
              ?? new Dictionary<string, string>();

        // Use reflection to create instance with private constructor
        var message = (ChatMessage)Activator.CreateInstance(typeof(ChatMessage), true)!;

        typeof(ChatMessage).GetProperty(nameof(ChatMessage.Id))!
            .SetValue(message, dto.Id);
        typeof(ChatMessage).GetProperty(nameof(ChatMessage.CompanyId))!
            .SetValue(message, dto.CompanyId);
        typeof(ChatMessage).GetProperty(nameof(ChatMessage.UserId))!
            .SetValue(message, dto.UserId);
        typeof(ChatMessage).GetProperty(nameof(ChatMessage.UserMessage))!
            .SetValue(message, dto.UserMessage);
        typeof(ChatMessage).GetProperty(nameof(ChatMessage.AssistantResponse))!
            .SetValue(message, dto.AssistantResponse);
        typeof(ChatMessage).GetProperty(nameof(ChatMessage.Intent))!
            .SetValue(message, (IntentType)dto.Intent);
        typeof(ChatMessage).GetProperty(nameof(ChatMessage.CreatedAt))!
            .SetValue(message, dto.CreatedAt);
        typeof(ChatMessage).GetProperty(nameof(ChatMessage.Metadata))!
            .SetValue(message, metadata);

        return message;
    }

    // ============================================================
    // DTOs for Dapper
    // ============================================================

    private class ChatMessageDto
    {
        public Guid Id { get; set; }
        public string CompanyId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string AssistantResponse { get; set; } = string.Empty;
        public int Intent { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Metadata { get; set; }
    }
}
