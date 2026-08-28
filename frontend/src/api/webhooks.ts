import { httpClient } from './httpClient';

export interface WebhookSubscription {
  id: string;
  targetUrl: string;
  eventType: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface WebhookSubscriptionUpsert {
  targetUrl: string;
  eventType: string;
  isActive: boolean;
}

export async function listWebhookSubscriptions(): Promise<WebhookSubscription[]> {
  const { data } = await httpClient.get<WebhookSubscription[]>('/api/webhook-subscriptions');
  return data;
}

export async function createWebhookSubscription(body: WebhookSubscriptionUpsert): Promise<WebhookSubscription> {
  const { data } = await httpClient.post<WebhookSubscription>('/api/webhook-subscriptions', body);
  return data;
}

export async function updateWebhookSubscription(id: string, body: WebhookSubscriptionUpsert): Promise<void> {
  await httpClient.put<void>(`/api/webhook-subscriptions/${id}`, body);
}

export async function deleteWebhookSubscription(id: string): Promise<void> {
  await httpClient.delete<void>(`/api/webhook-subscriptions/${id}`);
}
