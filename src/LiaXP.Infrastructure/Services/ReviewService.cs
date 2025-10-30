using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;

namespace LiaXP.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly string _connectionString;
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        IConfiguration configuration,
        IWhatsAppClient whatsAppClient,
        ILogger<ReviewService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _whatsAppClient = whatsAppClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new review queue entry for HITL workflow
    /// </summary>
    public async Task<ReviewQueue> CreateReviewAsync(
        ReviewQueue review,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            INSERT INTO ReviewQueue (
                Id, CompanyId, Moment, RecipientPhone, RecipientName,
                DraftMessage, Status, CreatedAt
            )
            VALUES (
                @Id, @CompanyId, @Moment, @RecipientPhone, @RecipientName,
                @DraftMessage, @Status, @CreatedAt
            )";

        try
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        review.Id,
                        review.CompanyId,
                        review.Moment,
                        review.RecipientPhone,
                        review.RecipientName,
                        review.DraftMessage,
                        review.Status,
                        review.CreatedAt
                    },
                    cancellationToken: cancellationToken
                )
            );

            _logger.LogInformation(
                "Review queue entry created | Id: {ReviewId} | Recipient: {RecipientName} | Phone: {Phone}",
                review.Id,
                review.RecipientName,
                review.RecipientPhone
            );

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating review queue entry | CompanyId: {CompanyId} | Recipient: {Phone}",
                review.CompanyId,
                review.RecipientPhone
            );
            throw;
        }
    }

    public async Task<IEnumerable<ReviewQueue>> GetPendingReviewsAsync(Guid companyId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT * FROM ReviewQueue 
            WHERE CompanyId = @CompanyId AND Status = 'Pending' AND IsDeleted = 0
            ORDER BY CreatedAt";

        return await connection.QueryAsync<ReviewQueue>(sql, new { CompanyId = companyId });
    }

    public async Task<ReviewQueue?> GetReviewByIdAsync(Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = "SELECT * FROM ReviewQueue WHERE Id = @Id AND IsDeleted = 0";
        return await connection.QueryFirstOrDefaultAsync<ReviewQueue>(sql, new { Id = id });
    }

    public async Task<bool> ApproveAndSendAsync(Guid reviewId, string reviewedBy)
    {
        var review = await GetReviewByIdAsync(reviewId);
        if (review == null || review.Status != "Pending")
            return false;

        var sendOnApprove = _configuration.GetValue<bool>("HITL:SendOnApprove", true);

        if (sendOnApprove)
        {
            var result = await _whatsAppClient.SendMessageAsync(
                review.RecipientPhone,
                review.DraftMessage,
                review.CompanyId);

            if (!result.Success)
            {
                await UpdateReviewStatusAsync(reviewId, "Failed", reviewedBy, result.ErrorMessage);
                return false;
            }

            await UpdateReviewStatusAsync(reviewId, "Sent", reviewedBy);
            return true;
        }
        else
        {
            await UpdateReviewStatusAsync(reviewId, "Approved", reviewedBy);
            return true;
        }
    }

    public async Task<bool> EditAndApproveAsync(Guid reviewId, string editedMessage, string reviewedBy)
    {
        var review = await GetReviewByIdAsync(reviewId);
        if (review == null || review.Status != "Pending")
            return false;

        // Update with edited message
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            UPDATE ReviewQueue 
            SET EditedMessage = @EditedMessage, 
                Status = 'Approved',
                ReviewedAt = GETUTCDATE(),
                ReviewedBy = @ReviewedBy,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            Id = reviewId,
            EditedMessage = editedMessage,
            ReviewedBy = reviewedBy
        });

        var sendOnApprove = _configuration.GetValue<bool>("HITL:SendOnApprove", true);

        if (sendOnApprove)
        {
            var result = await _whatsAppClient.SendMessageAsync(
                review.RecipientPhone,
                editedMessage,
                review.CompanyId);

            if (!result.Success)
            {
                await UpdateReviewStatusAsync(reviewId, "Failed", reviewedBy, result.ErrorMessage);
                return false;
            }

            await UpdateReviewStatusAsync(reviewId, "Sent", reviewedBy);
        }

        return true;
    }

    public async Task<bool> RejectAsync(Guid reviewId, string reviewedBy, string? reason = null)
    {
        await UpdateReviewStatusAsync(reviewId, "Rejected", reviewedBy, reason);
        return true;
    }

    private async Task UpdateReviewStatusAsync(Guid reviewId, string status, string reviewedBy, string? errorMessage = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            UPDATE ReviewQueue 
            SET Status = @Status,
                ReviewedAt = GETUTCDATE(),
                ReviewedBy = @ReviewedBy,
                SentAt = CASE WHEN @Status = 'Sent' THEN GETUTCDATE() ELSE SentAt END,
                ErrorMessage = @ErrorMessage,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            Id = reviewId,
            Status = status,
            ReviewedBy = reviewedBy,
            ErrorMessage = errorMessage
        });
    }
}
