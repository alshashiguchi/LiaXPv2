namespace LiaXP.Domain.Entities;

public class SalesData
{
    public Guid Id { get; private set; }
    public string CompanyCode { get; private set; }
    public DateTime Date { get; private set; }
    public string Store { get; private set; }
    public string SellerCode { get; private set; }
    public string SellerName { get; private set; }
    public decimal TotalValue { get; private set; }
    public int ItemsQty { get; private set; }
    public decimal AvgTicket { get; private set; }
    public string Category { get; private set; }
    public DateTime ImportedAt { get; private set; }

    private SalesData() { }

    public SalesData(
        string companyCode,
        DateTime date,
        string store,
        string sellerCode,
        string sellerName,
        decimal totalValue,
        int itemsQty,
        decimal avgTicket,
        string category)
    {
        Id = Guid.NewGuid();
        CompanyCode = companyCode;
        Date = date;
        Store = store;
        SellerCode = sellerCode;
        SellerName = sellerName;
        TotalValue = totalValue;
        ItemsQty = itemsQty;
        AvgTicket = avgTicket;
        Category = category;
        ImportedAt = DateTime.UtcNow;
    }
}

public class GoalData
{
    public Guid Id { get; private set; }
    public string CompanyCode { get; private set; }
    public string Month { get; private set; }
    public string Store { get; private set; }
    public string SellerCode { get; private set; }
    public decimal TargetValue { get; private set; }
    public decimal TargetTicket { get; private set; }
    public string TargetConversion { get; private set; }
    public DateTime ImportedAt { get; private set; }

    private GoalData() { }

    public GoalData(
        string companyCode,
        string month,
        string store,
        string sellerCode,
        decimal targetValue,
        decimal targetTicket,
        string targetConversion)
    {
        Id = Guid.NewGuid();
        CompanyCode = companyCode;
        Month = month;
        Store = store;
        SellerCode = sellerCode;
        TargetValue = targetValue;
        TargetTicket = targetTicket;
        TargetConversion = targetConversion;
        ImportedAt = DateTime.UtcNow;
    }
}

public class TeamMember
{
    public Guid Id { get; private set; }
    public string CompanyCode { get; private set; }
    public string SellerCode { get; private set; }
    public string SellerName { get; private set; }
    public string Role { get; private set; }
    public string Store { get; private set; }
    public string PhoneE164 { get; private set; }
    public string Status { get; private set; }
    public DateTime ImportedAt { get; private set; }

    private TeamMember() { }

    public TeamMember(
        string companyCode,
        string sellerCode,
        string sellerName,
        string role,
        string store,
        string phoneE164,
        string status = "active")
    {
        Id = Guid.NewGuid();
        CompanyCode = companyCode;
        SellerCode = sellerCode;
        SellerName = sellerName;
        Role = role;
        Store = store;
        PhoneE164 = phoneE164;
        Status = status;
        ImportedAt = DateTime.UtcNow;
    }
}