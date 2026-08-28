import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { listMyRequests, type PortalTicketListItem } from '../../api/portal';
import { startChat } from '../../api/chat';

export default function MyRequestsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [tickets, setTickets] = useState<PortalTicketListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [startingChat, setStartingChat] = useState(false);

  useEffect(() => {
    listMyRequests()
      .then(setTickets)
      .catch(() => setError(t('portal.myRequests.loadFailed')))
      .finally(() => setLoading(false));
  }, [t]);

  const handleStartChat = async () => {
    setStartingChat(true);
    setError(null);
    try {
      const { ticketId } = await startChat();
      navigate(`/portal/my-requests/${ticketId}`);
    } catch {
      setError(t('chat.startFailed'));
    } finally {
      setStartingChat(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('portal.myRequests.title')}</h1>
        <button
          type="button"
          onClick={handleStartChat}
          disabled={startingChat}
          className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 disabled:opacity-60"
        >
          {t('chat.startNew')}
        </button>
      </div>

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
