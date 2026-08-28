import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { type ManagementDashboardReport, getDashboard } from '../../api/reports';

function SummaryCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded border border-slate-200 bg-white p-4">
      <p className="text-sm text-slate-500">{label}</p>
      <p className="text-2xl font-semibold text-slate-800">{value}</p>
    </div>
  );
}

const SUB_PAGES = [
  { to: '/reports/tickets', labelKey: 'reports.tickets.title' },
  { to: '/reports/sla', labelKey: 'reports.sla.title' },
  { to: '/reports/agents', labelKey: 'reports.agents.title' },
  { to: '/reports/satisfaction', labelKey: 'reports.satisfaction.title' },
] as const;

export default function ReportsDashboardPage() {
  const { t } = useTranslation();

  const [report, setReport] = useState<ManagementDashboardReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getDashboard()
      .then(setReport)
      .catch(() => setError(t('reports.loadFailed')))
      .finally(() => setLoading(false));
  }, [t]);

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('reports.dashboard.title')}</h1>

      <nav aria-label={t('reports.dashboard.sectionsLabel')} className="flex flex-wrap gap-2">
        {SUB_PAGES.map((page) => (
          <Link
            key={page.to}
            to={page.to}
            className="rounded border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100"
          >
            {t(page.labelKey)}
          </Link>
        ))}
      </nav>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        report && (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <SummaryCard label={t('reports.dashboard.ticketVolume')} value={String(report.tickets.total)} />
            <SummaryCard
              label={t('reports.dashboard.slaCompliance')}
              value={`${report.sla.responseMetPercent.toFixed(0)}% / ${report.sla.resolutionMetPercent.toFixed(0)}%`}
            />
            <SummaryCard label={t('reports.dashboard.escalatedTickets')} value={String(report.sla.escalatedCount)} />
            <SummaryCard
              label={t('reports.dashboard.averageSatisfaction')}
              value={report.satisfaction.averageRating.toFixed(2)}
            />
          </div>
        )
      )}

      {!loading && report && (
        <div className="rounded border border-slate-200 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
            {t('reports.dashboard.topAgents')}
          </h2>
          {report.topAgents.length === 0 ? (
            <p className="text-sm text-slate-500">{t('reports.agents.empty')}</p>
          ) : (
            <table className="w-full text-start text-sm">
              <thead>
                <tr className="border-b border-slate-200">
                  <th className="py-1.5 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('reports.agents.columns.agent')}
                  </th>
                  <th className="py-1.5 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('reports.agents.columns.closed')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {report.topAgents.map((agent) => (
                  <tr key={agent.userId} className="border-b border-slate-100 last:border-0">
                    <td className="py-1.5 text-slate-700">{agent.displayName}</td>
                    <td className="py-1.5 text-slate-800">{agent.resolved}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  );
}
