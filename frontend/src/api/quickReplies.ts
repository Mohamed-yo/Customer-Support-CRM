import { httpClient } from './httpClient';

export interface QuickReplyTemplate {
  id: string;
  title: string;
  body: string;
  createdByUserId: string;
  createdByDisplayName: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface QuickReplyTemplateUpsert {
  title: string;
  body: string;
}

export async function listQuickReplies(): Promise<QuickReplyTemplate[]> {
  const { data } = await httpClient.get<QuickReplyTemplate[]>('/api/quick-replies');
  return data;
}

export async function createQuickReply(body: QuickReplyTemplateUpsert): Promise<QuickReplyTemplate> {
  const { data } = await httpClient.post<QuickReplyTemplate>('/api/quick-replies', body);
  return data;
}

export async function updateQuickReply(id: string, body: QuickReplyTemplateUpsert): Promise<void> {
  await httpClient.put<void>(`/api/quick-replies/${id}`, body);
}

export async function deleteQuickReply(id: string): Promise<void> {
  await httpClient.delete<void>(`/api/quick-replies/${id}`);
}
