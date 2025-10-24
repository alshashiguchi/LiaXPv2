using LiaXP.Domain.Enums;

namespace LiaXP.Domain.Interfaces;

public interface ITemplateService
{
    Task<string> GenerateMessageAsync(MomentType moment, Guid companyId, Guid? storeId = null, Guid? sellerId = null);
    Task<List<MessageDraft>> GenerateAllMessagesAsync(MomentType moment, Guid companyId);
}

public class MessageDraft
{
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string PhoneE164 { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MomentType Moment { get; set; }
}
