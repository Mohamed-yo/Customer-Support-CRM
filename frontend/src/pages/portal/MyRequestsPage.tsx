import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { listMyRequests, type PortalTicketListItem } from '../../api/portal';

export default function MyRequestsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [tickets, setTickets] = useState<PortalTicketListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    listMyRequests()
      .then(setTickets)
      .catch(() => setError(t('portal.myRequests.loadFailed')))
      .finally(() => setLoading(false));
  }, [t]);

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('portal.myRequests.title')}</h1>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {!loading && (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {tickets.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('portal.myRequests.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('tickets.columns.subject')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('tickets.columns.priority')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('tickets.columns.status')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('tickets.columns.createdAt')}
                  </th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {tickets.map((ticket) => (
                  <tr key={ticket.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                    <td className="px-4 py-3 text-slate-800">{ticket.subject}</td>
                    <td className="px-4 py-3 text-slate-600">{t(`tickets.priority.${ticket.priority}`)}</td>
                    <td className="px-4 py-3 text-slate-600">{t(`tickets.status.${ticket.status}`)}</td>
                    <td className="px-4 py-3 text-slate-600">{new Date(ticket.createdAtUtc).toLocaleDateString()}</td>
                    <td className="px-4 py-3">
                      <button
                        type="button"
                        onClick={() => navigate(`/portal/my-requests/${ticket.id}`)}
                        className="rounded px-2.5 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
                      >
                        {t('tickets.view')}
                      </button>
                    </td>
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
