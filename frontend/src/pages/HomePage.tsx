import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { type DashboardResponse, fetchDashboard } from '../api/dashboard';
import { useAuthStore } from '../store/useAuthStore';

function StatTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded border border-slate-200 bg-white p-4">
      <p className="text-sm text-slate-500">{label}</p>
      <p className="text-2xl font-semibold text-slate-800">{value}</p>
    </div>
  );
}

export default function HomePage() {
  const { t } = useTranslation();
  const isAdmin = useAuthStore((s) => s.hasRole('Admin'));

  const [data, setData] = useState<DashboardResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchDashboard()
      .then(setData)
      .catch(() => setError(t('home.states.error')))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (loading) return null;

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('home.title')}</h1>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {data && (
        <>
          {/* KPIs */}
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
            <StatTile label={t('home.kpis.total')} value={String(data.kpis.totalTickets)} />
            <StatTile label={t('home.kpis.open')} value={String(data.kpis.openTickets)} />
            <StatTile label={t('home.kpis.inProgress')} value={String(data.kpis.inProgressTickets)} />
            <StatTile label={t('home.kpis.closed')} value={String(data.kpis.closedTickets)} />
            <StatTile label={t('home.kpis.escalated')} value={String(data.kpis.escalatedTickets)} />
          </div>

          {/* My Work */}
          <section className="rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">{t('home.myWork.title')}</h2>
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
              <div className="flex flex-col gap-2">
                <h3 className="text-sm font-medium text-slate-700">{t('home.myWork.myTickets')}</h3>
                {data.myWork.myRecentAssignedTickets.length === 0 ? (
                  <p className="text-sm text-slate-500">{t('home.myWork.myTicketsEmpty')}</p>
                ) : (
                  <ul className="flex flex-col gap-1">
                    {data.myWork.myRecentAssignedTickets.map((ticket) => (
                      <li key={ticket.id}>
                        <Link
                          to={`/tickets/${ticket.id}`}
                          className="flex items-center justify-between gap-2 rounded px-2 py-1.5 text-sm text-slate-700 hover:bg-slate-50"
                        >
                          <span className="truncate">{ticket.subject}</span>
                          {ticket.isEscalated && (
                            <span className="shrink-0 rounded bg-red-600 px-1.5 py-0.5 text-xs font-medium text-white">
                              {t('tickets.escalatedBadge')}
                            </span>
                          )}
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
              </div>

              <div className="flex flex-col gap-2">
                <h3 className="text-sm font-medium text-slate-700">{t('home.myWork.unreadNotifications')}</h3>
                <p className="text-2xl font-semibold text-slate-800">{data.myWork.myUnreadNotificationCount}</p>
              </div>

              <div className="flex flex-col gap-2">
                <h3 className="text-sm font-medium text-slate-700">{t('home.myWork.myTasks')}</h3>
                {data.myWork.myOutstandingTasks.length === 0 ? (
                  <p className="text-sm text-slate-500">{t('home.myWork.myTasksEmpty')}</p>
                ) : (
                  <ul className="flex flex-col gap-1">
                    {data.myWork.myOutstandingTasks.map((task) => (
                      <li key={task.id}>
                        <Link
                          to={`/tickets/${task.ticketId}`}
                          className="flex items-center justify-between gap-2 rounded px-2 py-1.5 text-sm text-slate-700 hover:bg-slate-50"
                        >
                          <span className="truncate">{task.title}</span>
                          <span className="shrink-0 text-xs text-slate-500">
                            {task.dueAtUtc
                              ? `${t('home.myWork.dueAt')}: ${new Date(task.dueAtUtc).toLocaleDateString()}`
                              : t('home.myWork.noDueDate')}
                          </span>
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          </section>

          {/* Admin summary - deliberately smaller than /reports/dashboard: 2 tiles + a
              max-5-row table, no charts, no date range control. */}
          {isAdmin && data.adminSummary && (
            <section className="rounded border border-slate-200 bg-white p-4">
              <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">{t('home.admin.title')}</h2>
              <div className="mb-4 grid grid-cols-2 gap-4 sm:grid-cols-2">
                <StatTile label={t('home.admin.unassignedOpen')} value={String(data.adminSummary.unassignedOpenCount)} />
                <StatTile label={t('home.admin.escalatedOpen')} value={String(data.adminSummary.escalatedOpenCount)} />
              </div>

              <h3 className="mb-2 text-sm font-medium text-slate-700">{t('home.admin.topAgents')}</h3>
              {data.adminSummary.topAgents.length === 0 ? (
                <p className="text-sm text-slate-500">{t('home.myWork.myTicketsEmpty')}</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-start text-sm">
                    <thead>
                      <tr className="border-b border-slate-200">
                        <th className="py-1.5 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                          {t('home.admin.agent')}
                        </th>
                        <th className="py-1.5 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                          {t('home.admin.openAssigned')}
                        </th>
                        <th className="py-1.5 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                          {t('home.admin.resolved')}
                        </th>
                        <th className="py-1.5 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                          {t('home.admin.avgSatisfaction')}
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.adminSummary.topAgents.map((agent) => (
                        <tr key={agent.userId} className="border-b border-slate-100 last:border-0">
                          <td className="py-1.5 text-slate-700">{agent.displayName}</td>
                          <td className="py-1.5 text-slate-800">{agent.openAssignedCount}</td>
                          <td className="py-1.5 text-slate-800">{agent.resolvedCount}</td>
                          <td className="py-1.5 text-slate-800">
                            {agent.averageSatisfaction === null ? '—' : agent.averageSatisfaction.toFixed(1)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}

              <Link to="/reports/dashboard" className="mt-3 inline-block text-sm font-medium text-slate-600 hover:text-slate-800 hover:underline">
                {t('home.admin.viewFullReports')}
              </Link>
            </section>
          )}

          {/* Quick actions */}
          <section className="flex flex-col gap-2">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">{t('home.quickActions.title')}</h2>
            <div className="flex flex-wrap gap-2">
              <Link
                to="/tickets"
                className="rounded border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100"
              >
                {t('home.quickActions.goToTickets')}
              </Link>
              <Link
                to="/customers"
                className="rounded border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100"
              >
                {t('home.quickActions.goToCustomers')}
              </Link>
              <Link
                to="/knowledge-base"
                className="rounded border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100"
              >
                {t('home.quickActions.goToKnowledge')}
              </Link>
            </div>
          </section>
        </>
      )}
    </div>
  );
}
