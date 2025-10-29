using Dapper;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiaXP.Infrastructure.Repositories;

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
            INSERT INTO ChatHistory (
                Id, CompanyCode, UserId, UserMessage, 
                AssistantResponse, Intent, CreatedAt, MetadataJson
            )
            VALUES (
                @Id, @CompanyCode, @UserId, @UserMessage,
                @AssistantResponse, @Intent, @CreatedAt, @MetadataJson
            )";

        var metadataJson = System.Text.Json.JsonSerializer.Serialize(message.Metadata);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    message.Id,
                    message.CompanyCode,
                    message.UserId,
                    message.UserMessage,
                    message.AssistantResponse,
                    Intent = message.Intent.ToString(),
                    message.CreatedAt,
                    MetadataJson = metadataJson
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
                Id, CompanyCode, UserId, UserMessage,
                AssistantResponse, Intent, CreatedAt, MetadataJson
            FROM ChatHistory
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC";

        var results = await connection.QueryAsync<ChatHistoryDto>(
            new CommandDefinition(
                sql,
                new { UserId = userId, Limit = limit },
                cancellationToken: cancellationToken
            )
        );

        return results.Select(dto => dto.ToEntity()).ToList();
    }

    private class ChatHistoryDto
    {
        public Guid Id { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string AssistantResponse { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string MetadataJson { get; set; } = string.Empty;

        public ChatMessage ToEntity()
        {
            var metadata = string.IsNullOrWhiteSpace(MetadataJson)
                ? new Dictionary<string, string>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(MetadataJson)
                  ?? new Dictionary<string, string>();

            var intentEnum = Enum.TryParse<LiaXP.Domain.Enums.IntentType>(Intent, out var parsed)
                ? parsed
                : LiaXP.Domain.Enums.IntentType.Unknown;

            return new ChatMessage(
                CompanyCode,
                UserId,
                UserMessage,
                AssistantResponse,
                intentEnum,
                metadata
            );
        }
    }
}
