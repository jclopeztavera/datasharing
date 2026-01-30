using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeadlineApp.Infrastructure.Data.Repositories;

public class DeadlineRepository : IDeadlineRepository
{
    private readonly DeadlineDbContext _context;

    public DeadlineRepository(DeadlineDbContext context)
    {
        _context = context;
    }

    public async Task<Deadline?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Deadlines
            .Include(d => d.Matter)
            .Include(d => d.CourtRule)
            .Include(d => d.TriggerEvent)
            .Include(d => d.CalendarLink)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Deadline>> GetByMatterAsync(Guid matterId, CancellationToken cancellationToken = default)
    {
        return await _context.Deadlines
            .Include(d => d.CourtRule)
            .Include(d => d.CalendarLink)
            .Where(d => d.MatterId == matterId)
            .OrderBy(d => d.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deadline>> GetUpcomingAsync(Guid firmId, int days = 30, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var endDate = today.AddDays(days);

        return await _context.Deadlines
            .Include(d => d.Matter)
            .Include(d => d.CourtRule)
            .Where(d => d.Matter.FirmId == firmId &&
                        d.Matter.Status == MatterStatus.Active &&
                        d.Status == DeadlineStatus.Pending &&
                        d.DueDate >= today &&
                        d.DueDate <= endDate)
            .OrderBy(d => d.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deadline>> GetOverdueAsync(Guid firmId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await _context.Deadlines
            .Include(d => d.Matter)
            .Include(d => d.CourtRule)
            .Where(d => d.Matter.FirmId == firmId &&
                        d.Matter.Status == MatterStatus.Active &&
                        d.Status == DeadlineStatus.Pending &&
                        d.DueDate < today)
            .OrderBy(d => d.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deadline>> GetAtRiskBufferDeadlinesAsync(Guid firmId, int daysThreshold = 3, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var thresholdDate = today.AddDays(daysThreshold);

        return await _context.Deadlines
            .Include(d => d.Matter)
            .Include(d => d.CourtRule)
            .Where(d => d.Matter.FirmId == firmId &&
                        d.Matter.Status == MatterStatus.Active &&
                        d.Type == DeadlineType.Buffer &&
                        d.Status == DeadlineStatus.Pending &&
                        d.DueDate <= thresholdDate &&
                        d.DueDate >= today)
            .OrderBy(d => d.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Deadline>> GetByResponsibleAttorneyAsync(
        Guid attorneyId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Deadlines
            .Include(d => d.Matter)
            .Include(d => d.CourtRule)
            .Where(d => d.Matter.ResponsibleAttorneyId == attorneyId &&
                        d.Matter.Status == MatterStatus.Active);

        if (fromDate.HasValue)
        {
            query = query.Where(d => d.DueDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(d => d.DueDate <= toDate.Value);
        }

        return await query.OrderBy(d => d.DueDate).ToListAsync(cancellationToken);
    }

    public async Task<Deadline> CreateAsync(Deadline deadline, CancellationToken cancellationToken = default)
    {
        deadline.CreatedAt = DateTime.UtcNow;
        _context.Deadlines.Add(deadline);
        await _context.SaveChangesAsync(cancellationToken);
        return deadline;
    }

    public async Task<IEnumerable<Deadline>> CreateManyAsync(IEnumerable<Deadline> deadlines, CancellationToken cancellationToken = default)
    {
        var deadlineList = deadlines.ToList();
        foreach (var deadline in deadlineList)
        {
            deadline.CreatedAt = DateTime.UtcNow;
        }

        _context.Deadlines.AddRange(deadlineList);
        await _context.SaveChangesAsync(cancellationToken);
        return deadlineList;
    }

    public async Task<Deadline> UpdateAsync(Deadline deadline, CancellationToken cancellationToken = default)
    {
        deadline.UpdatedAt = DateTime.UtcNow;
        _context.Deadlines.Update(deadline);
        await _context.SaveChangesAsync(cancellationToken);
        return deadline;
    }

    public async Task<IEnumerable<Deadline>> GetPendingSyncAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Deadlines
            .Include(d => d.Matter)
            .Include(d => d.CalendarLink)
            .Where(d => d.CalendarLink == null ||
                        d.CalendarLink.SyncStatus == SyncStatus.Pending ||
                        d.CalendarLink.SyncStatus == SyncStatus.Failed)
            .ToListAsync(cancellationToken);
    }
}
