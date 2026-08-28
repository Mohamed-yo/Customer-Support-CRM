import { httpClient } from './httpClient';

export interface ReportDateRange {
  fromUtc?: string | null;
  toUtc?: string | null;
}

export interface TicketCountsReport {
  total: number;
  byStatus: Record<string, number>;
  byCategory: Record<string, number>;
  byPriority: Record<string, number>;
  bySource: Record<string, number>;
}

export interface SlaPerformanceReport {
  totalConsidered: number;
  responseMet: number;
  responseBreached: number;
  responseMetPercent: number;
  resolutionMet: number;
  resolutionBreached: number;
  resolutionMetPercent: number;
  averageResponseMinutes: number;
  averageResolutionMinutes: number;
  escalatedCount: number;
}

export interface AgentPerformanceRow {
  userId: string;
  displayName: string;
  open: number;
  inProgress: number;
  closed: number;
  resolved: number;
  averageResolutionMinutes: number;
}

export interface AgentPerformanceReport {
  agents: AgentPerformanceRow[];
}

export interface RatingDistributionEntry {
  rating: number;
  count: number;
}

export interface SatisfactionReport {
  averageRating: number;
  feedbackCount: number;
  closedTicketCount: number;
  responseRatePercent: number;
  distribution: RatingDistributionEntry[];
  averageRatingByCategory: Record<string, number>;
  averageRatingByAgent: Record<string, number>;
}

export interface ManagementDashboardReport {
  tickets: TicketCountsReport;
  sla: SlaPerformanceReport;
  topAgents: AgentPerformanceRow[];
  satisfaction: SatisfactionReport;
}

export async function getTicketCounts(range?: ReportDateRange): Promise<TicketCountsReport> {
  const { data } = await httpClient.get<TicketCountsReport>('/api/reports/tickets', { params: range });
  return data;
}

export async function getSlaPerformance(range?: ReportDateRange): Promise<SlaPerformanceReport> {
  const { data } = await httpClient.get<SlaPerformanceReport>('/api/reports/sla', { params: range });
  return data;
}

export async function getAgentPerformance(range?: ReportDateRange): Promise<AgentPerformanceReport> {
  const { data } = await httpClient.get<AgentPerformanceReport>('/api/reports/agents', { params: range });
  return data;
}

export async function getSatisfaction(range?: ReportDateRange): Promise<SatisfactionReport> {
  const { data } = await httpClient.get<SatisfactionReport>('/api/reports/satisfaction', { params: range });
  return data;
}

export async function getDashboard(range?: ReportDateRange): Promise<ManagementDashboardReport> {
  const { data } = await httpClient.get<ManagementDashboardReport>('/api/reports/dashboard', { params: range });
  return data;
}
