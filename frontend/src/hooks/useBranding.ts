import { useEffect, useState } from 'react';
import { getBranding, type BrandingSettings } from '../api/branding';

const DEFAULT_BRANDING: BrandingSettings = { appName: 'Customer Support CRM', logoDataUrl: null, primaryColorHex: null };

// Branding is cosmetic - a failed fetch keeps the default rather than blocking or erroring
// the shell that renders it.
export function useBranding(): BrandingSettings {
  const [branding, setBranding] = useState<BrandingSettings>(DEFAULT_BRANDING);

  useEffect(() => {
    let cancelled = false;
    getBranding()
      .then((data) => {
        if (!cancelled) setBranding(data);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, []);

  return branding;
}
