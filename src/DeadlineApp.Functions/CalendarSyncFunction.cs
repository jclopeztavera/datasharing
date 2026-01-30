using DeadlineApp.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DeadlineApp.Functions;

public class CalendarSyncFunction
{
    private readonly ICalendarSyncService _calendarSyncService;
    private readonly ILogger<CalendarSyncFunction> _logger;

    public CalendarSyncFunction(
        ICalendarSyncService calendarSyncService,
        ILogger<CalendarSyncFunction> logger)
    {
        _calendarSyncService = calendarSyncService;
        _logger = logger;
    }

    /// <summary>
    /// Process pending calendar syncs every 2 minutes
    /// </summary>
    [Function("ProcessPendingSyncs")]
    public async Task ProcessPendingSyncs(
        [TimerTrigger("0 */2 * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("ProcessPendingSyncs function started at: {time}", DateTime.UtcNow);

        try
        {
            await _calendarSyncService.ProcessPendingSyncsAsync(cancellationToken);
            _logger.LogInformation("ProcessPendingSyncs completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending calendar syncs");
            throw;
        }
    }

    /// <summary>
    /// Detect and resolve calendar conflicts every 5 minutes
    /// </summary>
    [Function("DetectCalendarConflicts")]
    public async Task DetectCalendarConflicts(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("DetectCalendarConflicts function started at: {time}", DateTime.UtcNow);

        try
        {
            await _calendarSyncService.DetectAndResolveConflictsAsync(cancellationToken);
            _logger.LogInformation("DetectCalendarConflicts completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting calendar conflicts");
            throw;
        }
    }
}
