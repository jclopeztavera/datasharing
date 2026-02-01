using DeadlineApp.Core.Models;

namespace DeadlineApp.Core.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Send an overdue deadline alert for a single deadline to the responsible attorney.
    /// </summary>
    Task<NotificationResult> SendOverdueAlertAsync(
        DeadlineNotification deadline,
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a daily deadline summary to an attorney.
    /// </summary>
    Task<NotificationResult> SendDailySummaryAsync(
        AttorneyDeadlineSummary summary,
        CancellationToken cancellationToken = default);
}
