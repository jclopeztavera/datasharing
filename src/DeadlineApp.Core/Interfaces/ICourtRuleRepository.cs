using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;

namespace DeadlineApp.Core.Interfaces;

public interface ICourtRuleRepository
{
    Task<IEnumerable<CourtRule>> GetRulesForMatterAsync(string state, string? county, string caseType, CancellationToken cancellationToken = default);
    Task<IEnumerable<CourtRule>> GetRulesByTriggerTypeAsync(string state, string? county, string caseType, TriggerEventType triggerType, CancellationToken cancellationToken = default);
    Task<CourtRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CourtRule> CreateAsync(CourtRule rule, CancellationToken cancellationToken = default);
    Task<CourtRule> UpdateAsync(CourtRule rule, CancellationToken cancellationToken = default);
}
