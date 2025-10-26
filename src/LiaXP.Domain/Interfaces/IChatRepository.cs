using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

public interface IChatRepository
{
    Task SaveMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<List<ChatMessage>> GetUserHistoryAsync(
        Guid userId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}