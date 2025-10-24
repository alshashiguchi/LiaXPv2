using LiaXP.Domain.Enums;

namespace LiaXP.Domain.Interfaces;

public interface ICronService
{
    Task ExecuteScheduledJobAsync(MomentType moment, Guid companyId, bool sendImmediately = false);
}
