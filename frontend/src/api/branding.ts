import { httpClient } from './httpClient';

export interface BrandingSettings {
  appName: string;
  logoDataUrl: string | null;
  primaryColorHex: string | null;
}

// Anonymous endpoint - safe to call before any login (login page, portal pages).
export async function getBranding(): Promise<BrandingSettings> {
  const { data } = await httpClient.get<BrandingSettings>('/api/branding');
  return data;
}

export async function updateBranding(body: BrandingSettings): Promise<void> {
  await httpClient.put<void>('/api/branding', body);
}
