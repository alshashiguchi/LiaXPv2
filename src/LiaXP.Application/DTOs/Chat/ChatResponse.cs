namespace LiaXP.Application.DTOs.Chat;

//public class ChatResponse
//{
//    public string Message { get; set; } = string.Empty;
//    public string Intent { get; set; } = string.Empty;
//    public double Confidence { get; set; }
//    public long ProcessingTimeMs { get; set; }
//    public DateTime Timestamp { get; set; }
//}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
