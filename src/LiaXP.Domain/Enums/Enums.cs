namespace LiaXP.Domain.Enums;

public enum MessageDirection
{
    Inbound,
    Outbound
}

public enum MessageStatus
{
    Pending,
    Sent,
    Delivered,
    Read,
    Failed
}

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected,
    Sent
}

public enum MomentType
{
    Morning,
    Midday,
    Evening
}

public enum IntentType
{
    GoalGap,
    PersonalizedTips,
    Ranking,
    Focus,
    AvgTicket,
    Admin,
    Unknown
}
