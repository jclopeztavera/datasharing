using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using DeadlineApp.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DeadlineApp.Functions;

public class DeadlineNotificationFunction
{
    private readonly IDeadlineRepository _deadlineRepository;
    private readonly IEnumerable<INotificationService> _notificationServices;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<DeadlineNotificationFunction> _logger;

    public DeadlineNotificationFunction(
        IDeadlineRepository deadlineRepository,
        IEnumerable<INotificationService> notificationServices,
        IAuditLogger auditLogger,
        ILogger<DeadlineNotificationFunction> logger)
    {
        _deadlineRepository = deadlineRepository;
        _notificationServices = notificationServices;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    /// <summary>
    /// Check for overdue deadlines, update their status, and send alerts — daily at 6 AM.
    /// </summary>
    [Function("CheckOverdueDeadlines")]
    public async Task CheckOverdueDeadlines(
        [TimerTrigger("0 0 6 * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("CheckOverdueDeadlines started at {Time}", DateTime.UtcNow);

        try
        {
            var overdueDeadlines = (await _deadlineRepository.GetAllOverdueAsync(cancellationToken)).ToList();

            if (overdueDeadlines.Count == 0)
            {
                _logger.LogInformation("No overdue deadlines found");
                return;
            }

            _logger.LogInformation("Found {Count} overdue deadlines", overdueDeadlines.Count);

            int markedCount = 0;
            int notifiedCount = 0;
            int failedCount = 0;

            foreach (var deadline in overdueDeadlines)
            {
                // Mark as overdue if still Pending
                if (deadline.Status == DeadlineStatus.Pending)
                {
                    deadline.Status = DeadlineStatus.Overdue;
                    deadline.UpdatedAt = DateTime.UtcNow;
                    deadline.UpdatedBy = "system";
                    await _deadlineRepository.UpdateAsync(deadline, cancellationToken);
                    markedCount++;

                    await _auditLogger.LogAsync(
                        AuditAction.DeadlineMarkedOverdue,
                        "Deadline", deadline.Id, deadline.MatterId,
                        newValues: new { deadline.Status, deadline.DueDate },
                        cancellationToken: cancellationToken);
                }

                // Send notification to responsible attorney
                var attorney = deadline.Matter.ResponsibleAttorney;
                if (attorney == null || !attorney.IsActive)
                    continue;

                var notification = MapToNotification(deadline);

                foreach (var service in _notificationServices)
                {
                    var result = await service.SendOverdueAlertAsync(
                        notification, attorney.Email, attorney.DisplayName, cancellationToken);

                    var auditAction = result.Success
                        ? AuditAction.NotificationSent
                        : AuditAction.NotificationFailed;

                    await _auditLogger.LogAsync(
                        auditAction, "Deadline", deadline.Id, deadline.MatterId,
                        newValues: new
                        {
                            Channel = result.Channel,
                            RecipientEmail = attorney.Email,
                            result.Success,
                            result.ErrorMessage
                        },
                        cancellationToken: cancellationToken);

                    if (result.Success)
                        notifiedCount++;
                    else
                        failedCount++;
                }
            }

            _logger.LogInformation(
                "CheckOverdueDeadlines completed: {Marked} marked overdue, {Notified} notifications sent, {Failed} failed",
                markedCount, notifiedCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckOverdueDeadlines");
            throw;
        }
    }

    /// <summary>
    /// Send personalized daily deadline summaries per attorney — daily at 7 AM.
    /// Includes overdue, at-risk buffer, and upcoming (7-day) deadlines.
    /// </summary>
    [Function("SendDailyDeadlineSummary")]
    public async Task SendDailyDeadlineSummary(
        [TimerTrigger("0 0 7 * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("SendDailyDeadlineSummary started at {Time}", DateTime.UtcNow);

        try
        {
            // Gather all three deadline categories across all firms
            var overdueTask = _deadlineRepository.GetAllOverdueAsync(cancellationToken);
            var atRiskTask = _deadlineRepository.GetAllAtRiskBufferAsync(3, cancellationToken);
            var upcomingTask = _deadlineRepository.GetAllUpcomingGroupedByAttorneyAsync(7, cancellationToken);

            await Task.WhenAll(overdueTask, atRiskTask, upcomingTask);

            var overdue = overdueTask.Result.ToList();
            var atRisk = atRiskTask.Result.ToList();
            var upcoming = upcomingTask.Result.ToList();

            // Group all deadlines by responsible attorney
            var allDeadlines = overdue.Concat(atRisk).Concat(upcoming).ToList();
            var overdueIds = new HashSet<Guid>(overdue.Select(d => d.Id));
            var atRiskIds = new HashSet<Guid>(atRisk.Select(d => d.Id));

            var byAttorney = allDeadlines
                .Where(d => d.Matter.ResponsibleAttorney != null && d.Matter.ResponsibleAttorney.IsActive)
                .GroupBy(d => d.Matter.ResponsibleAttorneyId);

            int summariesSent = 0;
            int summariesFailed = 0;

            foreach (var group in byAttorney)
            {
                var attorney = group.First().Matter.ResponsibleAttorney!;
                var firm = group.First().Matter.Firm;

                // De-duplicate deadlines that might appear in multiple categories
                var uniqueDeadlines = group.DistinctBy(d => d.Id).ToList();

                var summary = new AttorneyDeadlineSummary
                {
                    AttorneyId = attorney.Id,
                    AttorneyName = attorney.DisplayName,
                    AttorneyEmail = attorney.Email,
                    FirmId = attorney.FirmId,
                    FirmName = firm?.Name ?? "Unknown Firm",
                    GeneratedAtUtc = DateTime.UtcNow,
                    OverdueDeadlines = uniqueDeadlines
                        .Where(d => overdueIds.Contains(d.Id))
                        .Select(MapToNotification)
                        .ToList(),
                    AtRiskDeadlines = uniqueDeadlines
                        .Where(d => atRiskIds.Contains(d.Id) && !overdueIds.Contains(d.Id))
                        .Select(d => MapToNotification(d, isAtRisk: true))
                        .ToList(),
                    UpcomingDeadlines = uniqueDeadlines
                        .Where(d => !overdueIds.Contains(d.Id) && !atRiskIds.Contains(d.Id))
                        .Select(MapToNotification)
                        .ToList(),
                };

                // Skip if nothing to report
                if (summary.TotalCount == 0)
                    continue;

                foreach (var service in _notificationServices)
                {
                    var result = await service.SendDailySummaryAsync(summary, cancellationToken);

                    await _auditLogger.LogAsync(
                        result.Success ? AuditAction.DailySummarySent : AuditAction.NotificationFailed,
                        "User", attorney.Id, null,
                        newValues: new
                        {
                            Channel = result.Channel,
                            summary.AttorneyEmail,
                            summary.OverdueDeadlines.Count,
                            AtRiskCount = summary.AtRiskDeadlines.Count,
                            UpcomingCount = summary.UpcomingDeadlines.Count,
                            result.Success,
                            result.ErrorMessage
                        },
                        cancellationToken: cancellationToken);

                    if (result.Success)
                        summariesSent++;
                    else
                        summariesFailed++;
                }
            }

            _logger.LogInformation(
                "SendDailyDeadlineSummary completed: {Sent} sent, {Failed} failed across {Overdue} overdue, {AtRisk} at-risk, {Upcoming} upcoming deadlines",
                summariesSent, summariesFailed, overdue.Count, atRisk.Count, upcoming.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SendDailyDeadlineSummary");
            throw;
        }
    }

    private static DeadlineNotification MapToNotification(
        Deadline d, bool isAtRisk = false)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntil = (int)(d.DueDate.Date - today).TotalDays;

        return new DeadlineNotification
        {
            DeadlineId = d.Id,
            MatterNumber = d.Matter.MatterNumber,
            MatterTitle = d.Matter.Title,
            DeadlineTitle = d.Title,
            DueDate = d.DueDate,
            DaysUntilDue = daysUntil,
            IsOverdue = daysUntil < 0,
            IsAtRisk = isAtRisk,
            CourtRuleCitation = d.CourtRule?.RuleCitation,
        };
    }
}
