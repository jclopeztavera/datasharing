namespace DeadlineApp.Core.Models;

/// <summary>
/// Represents a deadline grouped for notification purposes.
/// </summary>
public class DeadlineNotification
{
    public Guid DeadlineId { get; set; }
    public string MatterNumber { get; set; } = string.Empty;
    public string MatterTitle { get; set; } = string.Empty;
    public string DeadlineTitle { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysUntilDue { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsAtRisk { get; set; }
    public string? CourtRuleCitation { get; set; }
}

/// <summary>
/// A per-attorney daily summary ready to be sent.
/// </summary>
public class AttorneyDeadlineSummary
{
    public Guid AttorneyId { get; set; }
    public string AttorneyName { get; set; } = string.Empty;
    public string AttorneyEmail { get; set; } = string.Empty;
    public Guid FirmId { get; set; }
    public string FirmName { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public List<DeadlineNotification> OverdueDeadlines { get; set; } = new();
    public List<DeadlineNotification> AtRiskDeadlines { get; set; } = new();
    public List<DeadlineNotification> UpcomingDeadlines { get; set; } = new();
    public int TotalCount => OverdueDeadlines.Count + AtRiskDeadlines.Count + UpcomingDeadlines.Count;
}

/// <summary>
/// Result of a notification send attempt.
/// </summary>
public class NotificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }
    public string Channel { get; set; } = string.Empty;

    public static NotificationResult Succeeded(string channel, string? messageId = null) =>
        new() { Success = true, Channel = channel, MessageId = messageId };

    public static NotificationResult Failed(string channel, string errorMessage) =>
        new() { Success = false, Channel = channel, ErrorMessage = errorMessage };
}
