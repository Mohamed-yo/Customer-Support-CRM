import { BrowserRouter, Route, Routes } from 'react-router-dom';
import HomePage from '../pages/HomePage';
import LoginPage from '../pages/LoginPage';
import CustomersPage from '../pages/CustomersPage';
import TicketsPage from '../pages/TicketsPage';
import RequireAuth from './RequireAuth';
import AppShell from '../components/layout/AppShell';

export default function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<RequireAuth />}>
          <Route element={<AppShell />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/customers" element={<CustomersPage />} />
            <Route path="/tickets" element={<TicketsPage />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
