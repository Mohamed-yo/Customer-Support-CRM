namespace CustomerSupportCrm.Api.Controllers;

// Story 13: bound as [FromQuery] on every ReportsController action - the same query-model
// type everywhere, not ad hoc loose fromUtc/toUtc parameters per action.
public record ReportDateRangeQuery(DateTime? FromUtc, DateTime? ToUtc);

public record TicketCountsReport(
    int Total,
    Dictionary<string, int> ByStatus,
    Dictionary<string, int> ByCategory,
    Dictionary<string, int> ByPriority,
    Dictionary<string, int> BySource);

public record SlaPerformanceReport(
    int TotalConsidered,
    int ResponseMet,
    int ResponseBreached,
    double ResponseMetPercent,
    int ResolutionMet,
    int ResolutionBreached,
    double ResolutionMetPercent,
    double AverageResponseMinutes,
    double AverageResolutionMinutes,
    int EscalatedCount);

public record AgentPerformanceRow(
    Guid UserId,
    string DisplayName,
    int Open,
    int InProgress,
    int Closed,
    int Resolved,
    double AverageResolutionMinutes);

public record AgentPerformanceReport(IReadOnlyList<AgentPerformanceRow> Agents);

public record RatingDistributionEntry(int Rating, int Count);

public record SatisfactionReport(
    double AverageRating,
    int FeedbackCount,
    int ClosedTicketCount,
    double ResponseRatePercent,
    IReadOnlyList<RatingDistributionEntry> Distribution,
    Dictionary<string, double> AverageRatingByCategory,
    Dictionary<string, double> AverageRatingByAgent);

public record ManagementDashboardReport(
    TicketCountsReport Tickets,
    SlaPerformanceReport Sla,
    IReadOnlyList<AgentPerformanceRow> TopAgents,
    SatisfactionReport Satisfaction);
