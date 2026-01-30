namespace DeadlineApp.Api.DTOs;

public record DashboardResponse(
    IEnumerable<DeadlineResponse> UpcomingDeadlines,
    IEnumerable<DeadlineResponse> OverdueDeadlines,
    IEnumerable<DeadlineResponse> AtRiskBufferDeadlines,
    DashboardSummary Summary);

public record DashboardSummary(
    int TotalUpcoming,
    int TotalOverdue,
    int TotalAtRisk,
    int TotalMatters);

public record MatterDeadlineGroup(
    Guid MatterId,
    string MatterNumber,
    string MatterTitle,
    IEnumerable<DeadlineResponse> Deadlines);
