// src/LiaXP.Application/UseCases/Chat/ProcessChatMessageUseCase.cs

using LiaXP.Application.DTOs.Chat;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Enums;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases.Chat;

/// <summary>
/// Use case for processing chat messages with AI assistant
/// UPDATED: Now uses CompanyId (GUID) instead of CompanyCode (string)
/// </summary>
public interface IProcessChatMessageUseCase
{
    /// <summary>
    /// Process a chat message
    /// </summary>
    /// <param name="request">Chat message request</param>
    /// <param name="userId">User ID from JWT</param>
    /// <param name="companyId">Company ID (GUID) from JWT - NOT CompanyCode!</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat response result</returns>
    Task<Result<ChatResponse>> ExecuteAsync(
        ChatRequest request,
        Guid userId,
        Guid companyId,  // ✅ CHANGED: Was string companyCode, now Guid companyId
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chat history for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="companyId">Company ID (GUID)</param>
    /// <param name="limit">Maximum number of messages to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of chat history items</returns>
    Task<List<ChatHistoryItem>> GetHistoryAsync(
        Guid userId,
        Guid companyId,
        int limit = 50,
        CancellationToken cancellationToken = default);
}

public class ProcessChatMessageUseCase : IProcessChatMessageUseCase
{
    private readonly IAIService _aiService;
    private readonly IChatRepository _chatMessageRepository;
    private readonly ICompanyResolver _companyResolver;
    private readonly ILogger<ProcessChatMessageUseCase> _logger;

    public ProcessChatMessageUseCase(
        IAIService aiService,
        IChatRepository chatMessageRepository,
        ICompanyResolver companyResolver,
        ILogger<ProcessChatMessageUseCase> logger)
    {
        _aiService = aiService;
        _chatMessageRepository = chatMessageRepository;
        _companyResolver = companyResolver;
        _logger = logger;
    }

    public async Task<Result<ChatResponse>> ExecuteAsync(
        ChatRequest request,
        Guid userId,
        Guid companyId,  // ✅ Now using CompanyId (GUID)
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ OPTIONAL: Get CompanyCode for logging/context (if needed by AI)
            var companyCode = await _companyResolver.GetCompanyCodeAsync(
                companyId,
                cancellationToken);

            if (string.IsNullOrEmpty(companyCode))
            {
                _logger.LogWarning(
                    "Company not found | CompanyId: {CompanyId}",
                    companyId);

                return Result<ChatResponse>.Failure("Company not found");
            }

            _logger.LogInformation(
                "Processing chat message | UserId: {UserId} | CompanyId: {CompanyId} | CompanyCode: {CompanyCode}",
                userId,
                companyId,
                companyCode);

            // Build context for AI (may need companyCode for display in prompts)
            var context = new Dictionary<string, object>
            {
                { "userId", userId },
                { "companyId", companyId },
                { "companyCode", companyCode }
            };

            // Process message with AI
            var aiResponse = await _aiService.ProcessMessageAsync(
                request.Message,
                companyCode,  // AI service may still use companyCode for context
                context,
                cancellationToken);

            // ✅ Save chat message using CompanyId (GUID)
            var chatMessage = new ChatMessage(
                companyId: companyId,  // ✅ Using CompanyId (GUID)
                userId: userId,
                userMessage: request.Message,
                assistantResponse: aiResponse.Message,
                intent: aiResponse.Intent,
                metadata: System.Text.Json.JsonSerializer.Serialize(aiResponse.Metadata)
            );

            await _chatMessageRepository.SaveMessageAsync(chatMessage, cancellationToken);

            _logger.LogInformation(
                "Chat message saved | MessageId: {MessageId} | Intent: {Intent}",
                chatMessage.Id,
                aiResponse.Intent);

            // Return response
            var response = new ChatResponse
            {
                Message = aiResponse.Message,
                Intent = aiResponse.Intent.ToString(),
                Metadata = aiResponse.Metadata,
                Timestamp = DateTime.UtcNow
            };

            return Result<ChatResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing chat message | UserId: {UserId} | CompanyId: {CompanyId}",
                userId,
                companyId);

            return Result<ChatResponse>.Failure(
                "Erro ao processar mensagem. Tente novamente.");
        }
    }

    public async Task<List<ChatHistoryItem>> GetHistoryAsync(
        Guid userId,
        Guid companyId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving chat history | UserId: {UserId} | CompanyId: {CompanyId} | Limit: {Limit}",
                userId,
                companyId,
                limit);

            // ✅ Query using CompanyId (GUID)
            var messages = await _chatMessageRepository.GetByUserAndCompanyAsync(
                userId,
                companyId,
                limit,
                cancellationToken);

            var history = messages.Select(m => new ChatHistoryItem
            {
                Id = m.Id,
                UserMessage = m.UserMessage,
                AssistantResponse = m.AssistantResponse,
                Intent = m.Intent.ToString(),
                CreatedAt = m.CreatedAt
            }).ToList();

            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving chat history | UserId: {UserId} | CompanyId: {CompanyId}",
                userId,
                companyId);

            return new List<ChatHistoryItem>();
        }
    }
}
