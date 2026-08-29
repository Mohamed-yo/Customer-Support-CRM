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

// Only ever present in the response to create/rotateSigningSecret - never returned again by
// listWebhookSubscriptions.
export interface WebhookSubscriptionCreated extends WebhookSubscription {
  signingSecret: string;
}

export async function listWebhookSubscriptions(): Promise<WebhookSubscription[]> {
  const { data } = await httpClient.get<WebhookSubscription[]>('/api/webhook-subscriptions');
  return data;
}

export async function createWebhookSubscription(body: WebhookSubscriptionUpsert): Promise<WebhookSubscriptionCreated> {
  const { data } = await httpClient.post<WebhookSubscriptionCreated>('/api/webhook-subscriptions', body);
  return data;
}

export async function rotateWebhookSigningSecret(id: string): Promise<string> {
  const { data } = await httpClient.post<{ signingSecret: string }>(`/api/webhook-subscriptions/${id}/rotate-secret`);
  return data.signingSecret;
}

export async function updateWebhookSubscription(id: string, body: WebhookSubscriptionUpsert): Promise<void> {
  await httpClient.put<void>(`/api/webhook-subscriptions/${id}`, body);
}

export async function deleteWebhookSubscription(id: string): Promise<void> {
  await httpClient.delete<void>(`/api/webhook-subscriptions/${id}`);
}
