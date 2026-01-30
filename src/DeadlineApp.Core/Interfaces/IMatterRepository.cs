using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Interfaces;

public interface IMatterRepository
{
    Task<Matter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Matter?> GetByIdWithDeadlinesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Matter>> GetByFirmAsync(Guid firmId, MatterStatus? status = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Matter>> GetByResponsibleAttorneyAsync(Guid attorneyId, MatterStatus? status = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Matter>> GetByUserAssignmentAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Matter> CreateAsync(Matter matter, CancellationToken cancellationToken = default);
    Task<Matter> UpdateAsync(Matter matter, CancellationToken cancellationToken = default);
    Task<bool> UserHasAccessAsync(Guid matterId, Guid userId, CancellationToken cancellationToken = default);
}
