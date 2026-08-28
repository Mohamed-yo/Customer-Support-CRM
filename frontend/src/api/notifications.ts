import { httpClient } from './httpClient';

export type NotificationType = 'Assigned' | 'Escalated';

export interface NotificationItem {
  id: string;
  type: NotificationType;
  message: string;
  ticketId: string | null;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc: string | null;
}

export async function listNotifications(): Promise<NotificationItem[]> {
  const { data } = await httpClient.get<NotificationItem[]>('/api/notifications');
  return data;
}

export async function getUnreadNotificationCount(): Promise<number> {
  const { data } = await httpClient.get<{ count: number }>('/api/notifications/unread-count');
  return data.count;
}

export async function markNotificationRead(id: string): Promise<void> {
  await httpClient.post<void>(`/api/notifications/${id}/read`);
}
