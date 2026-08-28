import { Navigate, Outlet } from 'react-router-dom';
import { useCustomerAuthStore } from '../store/useCustomerAuthStore';

export default function RequirePortalAuth() {
  const isAuthenticated = useCustomerAuthStore((s) => s.isAuthenticated());

  if (!isAuthenticated) {
    return <Navigate to="/portal/login" replace />;
  }

  return <Outlet />;
}
