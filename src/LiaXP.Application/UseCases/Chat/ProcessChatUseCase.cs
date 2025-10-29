// src/LiaXP.Application/UseCases/Chat/ProcessChatMessageUseCase.cs

using LiaXP.Application.DTOs.Chat;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Enums;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases.Chat;

/// <summary>
/// Use Case para processar mensagens de chat com IA
/// </summary>
public interface IProcessChatMessageUseCase
{
    Task<Result<ChatResponse>> ExecuteAsync(
        ChatRequest request,
        Guid userId,
        string companyCode,
        CancellationToken cancellationToken = default);
}

public class ProcessChatMessageUseCase : IProcessChatMessageUseCase
{
    private readonly IIntentRouter _intentRouter;
    private readonly IChatRepository _chatRepository;
    private readonly ISalesDataSource _salesDataSource;
    private readonly ILogger<ProcessChatMessageUseCase> _logger;

    public ProcessChatMessageUseCase(
        IIntentRouter intentRouter,
        IChatRepository chatRepository,
        ISalesDataSource salesDataSource,
        ILogger<ProcessChatMessageUseCase> logger)
    {
        _intentRouter = intentRouter;
        _chatRepository = chatRepository;
        _salesDataSource = salesDataSource;
        _logger = logger;
    }

    public async Task<Result<ChatResponse>> ExecuteAsync(
        ChatRequest request,
        Guid companyCode,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validar entrada
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Result<ChatResponse>.Failure("Mensagem não pode estar vazia");
            }

            _logger.LogInformation(
                "Processando mensagem de chat. UserId: {UserId}, Company: {CompanyCode}",
                userId,
                companyCode);

            // 1. Detectar intent e rotear mensagem
            var intentResult = await _intentRouter.RouteMessageAsync(
                request.Message,
                companyCode,
                userId);

            if (!intentResult.Success)
            {
                _logger.LogWarning(
                    "Falha ao detectar intent: {Error}",
                    intentResult.ErrorMessage);

                return Result<ChatResponse>.Failure(
                    intentResult.ErrorMessage ?? "Não foi possível processar a mensagem");
            }

            // 2. Salvar mensagem no histórico
            var chatMessage = new ChatMessage(
                companyCode: userId,
                userId: companyCode,
                userMessage: request.Message,
                assistantResponse: intentResult.Response,
                intent: ParseIntent(intentResult.Intent.ToString()),
                metadata: new Dictionary<string, string>
                {
                    { "intent_confidence", intentResult.Confidence?.ToString() ?? "unknown" },
                    { "processing_time_ms", intentResult.ProcessingTimeMs.ToString() ?? "0" }
                });

            await _chatRepository.SaveMessageAsync(chatMessage, cancellationToken);

            // 3. Criar resposta
            var response = new ChatResponse
            {
                Message = intentResult.Response,
                Intent = intentResult.Intent.ToString(),
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Mensagem processada com sucesso. Intent: {Intent}",
                intentResult.Intent);

            return Result<ChatResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar mensagem de chat. UserId: {UserId}",
                userId);

            return Result<ChatResponse>.Failure(
                "Ocorreu um erro ao processar sua mensagem. Tente novamente.");
        }
    }

    private IntentType ParseIntent(string? intentString)
    {
        if (string.IsNullOrWhiteSpace(intentString))
            return IntentType.Unknown;

        if (Enum.TryParse<IntentType>(intentString, ignoreCase: true, out var intent))
            return intent;

        return IntentType.Unknown;
    }
}

// ========================================
// Result Helper Class
// ========================================

/// <summary>
/// Classe para encapsular resultados de operações
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? data, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}