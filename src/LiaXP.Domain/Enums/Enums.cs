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
    Unknown = 0,
    GoalGap = 1,
    PersonalizedTips = 2,  // Era "Tips"
    Ranking = 3,
    SellerPerformance = 4, 
    TeamMotivation = 5,    
    ProductHelp = 6,       
    Focus = 7,             
    AvgTicket = 8,         
    Admin = 9,             
    GeneralQuestion = 10   
}