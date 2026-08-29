import { httpClient } from './httpClient';

export interface DashboardKpis {
  totalTickets: number;
  openTickets: number;
  inProgressTickets: number;
  closedTickets: number;
  escalatedTickets: number;
}

export interface DashboardMyTicket {
  id: string;
  subject: string;
  status: string;
  priority: string;
  isEscalated: boolean;
  createdAtUtc: string;
}

export interface DashboardMyTask {
  id: string;
  ticketId: string;
  title: string;
  dueAtUtc: string | null;
}

export interface DashboardMyWork {
  myAssignedOpenCount: number;
  myRecentAssignedTickets: DashboardMyTicket[];
  myUnreadNotificationCount: number;
  myOutstandingTasks: DashboardMyTask[];
}

export interface DashboardAdminAgentRow {
  userId: string;
  displayName: string;
  openAssignedCount: number;
  resolvedCount: number;
  averageSatisfaction: number | null;
}

export interface DashboardAdminSummary {
  unassignedOpenCount: number;
  escalatedOpenCount: number;
  topAgents: DashboardAdminAgentRow[];
}

export interface DashboardResponse {
  kpis: DashboardKpis;
  myWork: DashboardMyWork;
  // Populated only when the caller is an Admin; null for an Agent - never present for both.
  adminSummary: DashboardAdminSummary | null;
}

export async function fetchDashboard(): Promise<DashboardResponse> {
  const { data } = await httpClient.get<DashboardResponse>('/api/dashboard');
  return data;
}
