using DeadlineApp.Core.Entities;
using DeadlineApp.Core.Enums;
using DeadlineApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DeadlineApp.Infrastructure.Data.Repositories;

public class CourtRuleRepository : ICourtRuleRepository
{
    private readonly DeadlineDbContext _context;

    public CourtRuleRepository(DeadlineDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CourtRule>> GetRulesForMatterAsync(
        string state,
        string? county,
        string caseType,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await _context.CourtRules
            .Where(r => r.State == state &&
                        (r.County == null || r.County == county) &&
                        r.CaseType == caseType &&
                        r.IsActive &&
                        r.EffectiveDate <= today &&
                        (r.ExpirationDate == null || r.ExpirationDate > today))
            .OrderBy(r => r.County == null ? 1 : 0) // County-specific rules first
            .ThenBy(r => r.RuleName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CourtRule>> GetRulesByTriggerTypeAsync(
        string state,
        string? county,
        string caseType,
        TriggerEventType triggerType,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        return await _context.CourtRules
            .Where(r => r.State == state &&
                        (r.County == null || r.County == county) &&
                        r.CaseType == caseType &&
                        r.TriggerEventType == triggerType &&
                        r.IsActive &&
                        r.EffectiveDate <= today &&
                        (r.ExpirationDate == null || r.ExpirationDate > today))
            .OrderBy(r => r.County == null ? 1 : 0)
            .ThenBy(r => r.RuleName)
            .ToListAsync(cancellationToken);
    }

    public async Task<CourtRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CourtRules.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<CourtRule> CreateAsync(CourtRule rule, CancellationToken cancellationToken = default)
    {
        rule.CreatedAt = DateTime.UtcNow;
        _context.CourtRules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }

    public async Task<CourtRule> UpdateAsync(CourtRule rule, CancellationToken cancellationToken = default)
    {
        rule.UpdatedAt = DateTime.UtcNow;
        _context.CourtRules.Update(rule);
        await _context.SaveChangesAsync(cancellationToken);
        return rule;
    }
}
