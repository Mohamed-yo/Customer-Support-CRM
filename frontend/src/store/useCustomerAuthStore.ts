import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

export interface PortalCustomer {
  id: string;
  email: string;
  fullName: string;
}

interface CustomerAuthState {
  token: string | null;
  customer: PortalCustomer | null;
  expiresAtUtc: string | null;
  setSession: (payload: { token: string; customer: PortalCustomer; expiresAtUtc: string }) => void;
  clearSession: () => void;
  isAuthenticated: () => boolean;
}

export const useCustomerAuthStore = create<CustomerAuthState>()(
  persist(
    (set, get) => ({
      token: null,
      customer: null,
      expiresAtUtc: null,
      setSession: ({ token, customer, expiresAtUtc }) => set({ token, customer, expiresAtUtc }),
      clearSession: () => set({ token: null, customer: null, expiresAtUtc: null }),
      isAuthenticated: () => {
        const { token, expiresAtUtc } = get();
        if (!token || !expiresAtUtc) return false;
        return new Date(expiresAtUtc).getTime() > Date.now();
      },
    }),
    {
      // Distinct storage key from the staff store so a staff and a customer session can
      // coexist in the same browser without overwriting each other.
      name: 'cscrm-customer-auth-store',
      version: 1,
      migrate: () => ({ token: null, customer: null, expiresAtUtc: null }),
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ token: state.token, customer: state.customer, expiresAtUtc: state.expiresAtUtc }),
    },
  ),
);
