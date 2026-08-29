import { createContext, useContext, useEffect } from 'react';

export type BreadcrumbLabelSetter = (label: string | null) => void;
export const BreadcrumbLabelContext = createContext<BreadcrumbLabelSetter | null>(null);

/**
 * Detail pages call this once their entity has loaded to publish the current
 * (last) crumb's label. Pass null (or omit the call) while data is loading -
 * the breadcrumb will fall back to the parent's label, never showing the raw :id.
 */
export function useBreadcrumbLabel(label: string | null | undefined): void {
  const setter = useContext(BreadcrumbLabelContext);
  useEffect(() => {
    if (!setter) return;
    setter(label ?? null);
    return () => setter(null);
  }, [setter, label]);
}
