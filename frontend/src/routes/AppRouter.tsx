import { BrowserRouter, Route, Routes } from 'react-router-dom';
import HomePage from '../pages/HomePage';
import LoginPage from '../pages/LoginPage';
import CustomersPage from '../pages/CustomersPage';
import CustomerDetailPage from '../pages/CustomerDetailPage';
import TicketsPage from '../pages/TicketsPage';
import TicketDetailPage from '../pages/TicketDetailPage';
import QuickRepliesPage from '../pages/QuickRepliesPage';
import KnowledgeBasePage from '../pages/KnowledgeBasePage';
import WebFormPage from '../pages/WebFormPage';
import WebhooksPage from '../pages/WebhooksPage';
import ReportsDashboardPage from '../pages/reports/ReportsDashboardPage';
import TicketReportsPage from '../pages/reports/TicketReportsPage';
import SlaReportPage from '../pages/reports/SlaReportPage';
import AgentReportPage from '../pages/reports/AgentReportPage';
import SatisfactionReportPage from '../pages/reports/SatisfactionReportPage';
import PortalLoginPage from '../pages/portal/PortalLoginPage';
import PortalRegisterPage from '../pages/portal/PortalRegisterPage';
import SubmitTicketPage from '../pages/portal/SubmitTicketPage';
import MyRequestsPage from '../pages/portal/MyRequestsPage';
import MyRequestDetailPage from '../pages/portal/MyRequestDetailPage';
import PortalKnowledgeBasePage from '../pages/portal/PortalKnowledgeBasePage';
import RequireAuth from './RequireAuth';
import RequirePortalAuth from './RequirePortalAuth';
import AppShell from '../components/layout/AppShell';
import PortalShell from '../components/layout/PortalShell';

export default function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/web-form" element={<WebFormPage />} />
        <Route element={<RequireAuth />}>
          <Route element={<AppShell />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/customers" element={<CustomersPage />} />
            <Route path="/customers/:id" element={<CustomerDetailPage />} />
            <Route path="/tickets" element={<TicketsPage />} />
            <Route path="/tickets/:id" element={<TicketDetailPage />} />
            <Route path="/quick-replies" element={<QuickRepliesPage />} />
            <Route path="/knowledge-base" element={<KnowledgeBasePage />} />
            <Route path="/webhooks" element={<WebhooksPage />} />
            <Route path="/reports" element={<ReportsDashboardPage />} />
            <Route path="/reports/tickets" element={<TicketReportsPage />} />
            <Route path="/reports/sla" element={<SlaReportPage />} />
            <Route path="/reports/agents" element={<AgentReportPage />} />
            <Route path="/reports/satisfaction" element={<SatisfactionReportPage />} />
          </Route>
        </Route>

        <Route path="/portal/login" element={<PortalLoginPage />} />
        <Route path="/portal/register" element={<PortalRegisterPage />} />
        <Route element={<RequirePortalAuth />}>
          <Route element={<PortalShell />}>
            <Route path="/portal/submit-ticket" element={<SubmitTicketPage />} />
            <Route path="/portal/my-requests" element={<MyRequestsPage />} />
            <Route path="/portal/my-requests/:id" element={<MyRequestDetailPage />} />
            <Route path="/portal/knowledge-base" element={<PortalKnowledgeBasePage />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
