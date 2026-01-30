using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeadlineApp.Infrastructure.Data.Repositories;

public class MatterRepository : IMatterRepository
{
    private readonly DeadlineDbContext _context;

    public MatterRepository(DeadlineDbContext context)
    {
        _context = context;
    }

    public async Task<Matter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Matters
            .Include(m => m.ResponsibleAttorney)
            .Include(m => m.Assignments)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Matter?> GetByIdWithDeadlinesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Matters
            .Include(m => m.ResponsibleAttorney)
            .Include(m => m.Deadlines)
                .ThenInclude(d => d.CalendarLink)
            .Include(m => m.TriggerEvents)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Matter>> GetByFirmAsync(Guid firmId, MatterStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Matters
            .Include(m => m.ResponsibleAttorney)
            .Where(m => m.FirmId == firmId);

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Matter>> GetByResponsibleAttorneyAsync(Guid attorneyId, MatterStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Matters
            .Include(m => m.ResponsibleAttorney)
            .Where(m => m.ResponsibleAttorneyId == attorneyId);

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Matter>> GetByUserAssignmentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Matters
            .Include(m => m.ResponsibleAttorney)
            .Where(m => m.Assignments.Any(a => a.UserId == userId && a.IsActive))
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Matter> CreateAsync(Matter matter, CancellationToken cancellationToken = default)
    {
        matter.CreatedAt = DateTime.UtcNow;
        _context.Matters.Add(matter);
        await _context.SaveChangesAsync(cancellationToken);
        return matter;
    }

    public async Task<Matter> UpdateAsync(Matter matter, CancellationToken cancellationToken = default)
    {
        matter.UpdatedAt = DateTime.UtcNow;
        _context.Matters.Update(matter);
        await _context.SaveChangesAsync(cancellationToken);
        return matter;
    }

    public async Task<bool> UserHasAccessAsync(Guid matterId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Matters
            .AnyAsync(m => m.Id == matterId &&
                (m.ResponsibleAttorneyId == userId ||
                 m.Assignments.Any(a => a.UserId == userId && a.IsActive)),
                cancellationToken);
    }
}
