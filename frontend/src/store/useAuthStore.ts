import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

export interface AuthUser {
  email: string;
  displayName: string;
}

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  expiresAtUtc: string | null;
  setSession: (payload: { token: string; user: AuthUser; expiresAtUtc: string }) => void;
  clearSession: () => void;
  isAuthenticated: () => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      token: null,
      user: null,
      expiresAtUtc: null,
      setSession: ({ token, user, expiresAtUtc }) => set({ token, user, expiresAtUtc }),
      clearSession: () => set({ token: null, user: null, expiresAtUtc: null }),
      isAuthenticated: () => {
        const { token, expiresAtUtc } = get();
        if (!token || !expiresAtUtc) return false;
        return new Date(expiresAtUtc).getTime() > Date.now();
      },
    }),
    {
      name: 'cscrm-auth-store',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ token: state.token, user: state.user, expiresAtUtc: state.expiresAtUtc }),
    },
  ),
);
