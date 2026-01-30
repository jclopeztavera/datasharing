using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Interfaces;

public interface IDeadlineRepository
{
    Task<Deadline?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deadline>> GetByMatterAsync(Guid matterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deadline>> GetUpcomingAsync(Guid firmId, int days = 30, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deadline>> GetOverdueAsync(Guid firmId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deadline>> GetAtRiskBufferDeadlinesAsync(Guid firmId, int daysThreshold = 3, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deadline>> GetByResponsibleAttorneyAsync(Guid attorneyId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Deadline> CreateAsync(Deadline deadline, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deadline>> CreateManyAsync(IEnumerable<Deadline> deadlines, CancellationToken cancellationToken = default);
    Task<Deadline> UpdateAsync(Deadline deadline, CancellationToken cancellationToken = default);
    Task<IEnumerable<Deadline>> GetPendingSyncAsync(CancellationToken cancellationToken = default);
}
