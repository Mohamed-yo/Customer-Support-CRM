import { httpClient } from './httpClient';

export interface SlaTarget {
  responseHours: number;
  resolutionHours: number;
}

export type SlaTargets = Record<string, SlaTarget>;

export interface ReminderLeadTime {
  hours: number;
}

export async function getSlaTargets(): Promise<SlaTargets> {
  const { data } = await httpClient.get<{ value: SlaTargets }>('/api/runtime-settings/sla_targets');
  return data.value;
}

export async function updateSlaTargets(value: SlaTargets): Promise<void> {
  await httpClient.put<void>('/api/runtime-settings/sla_targets', value);
}

export async function getReminderLeadTime(): Promise<ReminderLeadTime> {
  const { data } = await httpClient.get<{ value: ReminderLeadTime }>('/api/runtime-settings/reminder_lead_hrs');
  return data.value;
}

export async function updateReminderLeadTime(value: ReminderLeadTime): Promise<void> {
  await httpClient.put<void>('/api/runtime-settings/reminder_lead_hrs', value);
}
