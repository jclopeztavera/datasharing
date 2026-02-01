namespace DeadlineApp.Core.Enums;

public enum AuditAction
{
    DeadlineCreated = 1,
    DeadlineRecalculated = 2,
    DeadlineOverridden = 3,
    DeadlineCompleted = 4,
    DeadlineCancelled = 5,
    CourtRuleCreated = 6,
    CourtRuleUpdated = 7,
    CalendarSyncStarted = 8,
    CalendarSyncCompleted = 9,
    CalendarSyncFailed = 10,
    CalendarEventCreated = 11,
    CalendarEventUpdated = 12,
    CalendarEventDeleted = 13,
    MatterCreated = 14,
    MatterUpdated = 15,
    MatterAccessed = 16,
    MatterClosed = 17,
    UserLogin = 18,
    UserLogout = 19,
    NotificationSent = 20,
    NotificationFailed = 21,
    DeadlineMarkedOverdue = 22,
    DailySummarySent = 23
}
