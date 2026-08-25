import { create } from 'zustand';

interface AppState {
  appName: string;
  setAppName: (name: string) => void;
}

export const useAppStore = create<AppState>((set) => ({
  appName: 'Customer Support CRM',
  setAppName: (name) => set({ appName: name }),
}));
