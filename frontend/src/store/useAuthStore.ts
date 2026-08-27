import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  expiresAtUtc: string | null;
  setSession: (payload: { token: string; user: AuthUser; expiresAtUtc: string }) => void;
  clearSession: () => void;
  isAuthenticated: () => boolean;
  hasRole: (role: string) => boolean;
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
      // Optional chaining on `.roles` too: a session persisted before this story
      // lacks the field entirely (Zustand's shallow merge won't backfill it).
      hasRole: (role) => get().user?.roles?.includes(role) ?? false,
    }),
    {
      name: 'cscrm-auth-store',
      // v2: AuthUser gained `id` (Story 09, needed for the "My tickets" filter).
      // Older persisted sessions have no way to backfill it, so migrate() drops
      // them entirely rather than leaving a user object with a missing id.
      version: 2,
      migrate: () => ({ token: null, user: null, expiresAtUtc: null }),
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ token: state.token, user: state.user, expiresAtUtc: state.expiresAtUtc }),
    },
  ),
);
