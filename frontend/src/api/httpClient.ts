import axios from 'axios';
import { useAuthStore } from '../store/useAuthStore';
import { useCustomerAuthStore } from '../store/useCustomerAuthStore';

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

export const httpClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

httpClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

httpClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().clearSession();
    }
    if (error.response?.status === 403) {
      // Authenticated but not authorized for this resource — distinct from 401.
      // Do NOT clear the session or redirect; the caller decides how to handle it.
    }
    return Promise.reject(error);
  },
);

// A second, independent instance so a customer (portal) session's token is never sent
// on a staff request and vice versa — each reads its own store, never the other's.
export const portalHttpClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

portalHttpClient.interceptors.request.use((config) => {
  const token = useCustomerAuthStore.getState().token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

portalHttpClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useCustomerAuthStore.getState().clearSession();
    }
    return Promise.reject(error);
  },
);
