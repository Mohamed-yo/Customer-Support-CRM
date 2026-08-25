import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';

export type Language = 'en' | 'ar';

interface AppState {
  appName: string;
  setAppName: (name: string) => void;
  language: Language;
  setLanguage: (lang: Language) => void;
}

export const useAppStore = create<AppState>()(
  persist(
    (set) => ({
      appName: 'Customer Support CRM',
      setAppName: (name) => set({ appName: name }),
      language: 'en',
      setLanguage: (language) => set({ language }),
    }),
    {
      name: 'cscrm-app-store',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ language: state.language }),
    },
  ),
);
