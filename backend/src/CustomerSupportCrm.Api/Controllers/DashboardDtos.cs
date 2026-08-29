namespace CustomerSupportCrm.Api.Controllers;

public sealed record DashboardResponseDto(
    DashboardKpisDto Kpis,
    DashboardMyWorkDto MyWork,
    // Populated only when the caller is in the "Admin" role; null for an Agent caller.
    DashboardAdminSummaryDto? AdminSummary);

public sealed record DashboardKpisDto(
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ClosedTickets,
    // Computed via TicketsController.ComputeIsEscalated - never a parallel implementation.
    int EscalatedTickets);

public sealed record DashboardMyWorkDto(
    int MyAssignedOpenCount,
    IReadOnlyList<DashboardMyTicketDto> MyRecentAssignedTickets,
    int MyUnreadNotificationCount,
    IReadOnlyList<DashboardMyTaskDto> MyOutstandingTasks);

public sealed record DashboardMyTicketDto(
    Guid Id, string Subject, string Status, string Priority, bool IsEscalated, DateTime CreatedAtUtc);

public sealed record DashboardMyTaskDto(Guid Id, Guid TicketId, string Title, DateTime? DueAtUtc);

public sealed record DashboardAdminSummaryDto(
    int UnassignedOpenCount,
    int EscalatedOpenCount,
    IReadOnlyList<DashboardAdminAgentRowDto> TopAgents);

public sealed record DashboardAdminAgentRowDto(
    Guid UserId,
    string DisplayName,
    int OpenAssignedCount,
    int ResolvedCount,
    // double, not decimal - matches every average-rating field in Controllers/ReportsDtos.cs.
    double? AverageSatisfaction);
