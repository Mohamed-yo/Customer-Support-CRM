import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { type Customer, getCustomer, getCustomerHistory } from '../api/customers';
import { type HistoryEntry, type Ticket, listTickets } from '../api/tickets';

export default function CustomerDetailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const [customer, setCustomer] = useState<Customer | null>(null);
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    setError(null);
    Promise.all([getCustomer(id), listTickets(id), getCustomerHistory(id)])
      .then(([customerData, ticketData, historyData]) => {
        setCustomer(customerData);
        setTickets(ticketData);
        setHistory(historyData);
      })
      .catch(() => setError(t('customerDetail.loadFailed')))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  if (loading) return null;

  if (!customer) {
    return (
      <div className="flex flex-col gap-4">
        <p className="text-sm text-red-600">{error ?? t('customerDetail.notFound')}</p>
        <button
          type="button"
          onClick={() => navigate('/customers')}
          className="w-fit rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
        >
          {t('customerDetail.back')}
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <button
          type="button"
          onClick={() => navigate('/customers')}
          className="w-fit text-sm font-medium text-slate-600 hover:text-slate-800"
        >
          {t('customerDetail.back')}
        </button>
        <h1 className="text-2xl font-semibold text-slate-800">{customer.fullName}</h1>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
        <section className="rounded border border-slate-200 bg-white p-4">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
            {t('customers.title')}
          </h2>
          <div className="flex flex-col gap-1 text-sm text-slate-700">
            <span>{customer.email}</span>
            {customer.phone && <span>{customer.phone}</span>}
          </div>
        </section>

        <section className="rounded border border-slate-200 bg-white p-4 md:col-span-2">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
            {t('customerDetail.tickets.heading')}
          </h2>
          {tickets.length === 0 ? (
            <p className="text-sm text-slate-500">{t('tickets.empty')}</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-start text-sm">
                <thead>
                  <tr className="border-b border-slate-200 text-xs font-semibold uppercase tracking-wide text-slate-500">
                    <th className="px-2 py-2 text-start">{t('tickets.columns.subject')}</th>
                    <th className="px-2 py-2 text-start">{t('tickets.columns.status')}</th>
                    <th className="px-2 py-2 text-start">{t('tickets.columns.category')}</th>
                    <th className="px-2 py-2 text-start">{t('tickets.columns.priority')}</th>
                    <th className="px-2 py-2 text-start">{t('tickets.columns.createdAt')}</th>
                    <th className="px-2 py-2" />
                  </tr>
                </thead>
                <tbody>
                  {tickets.map((ticket) => (
                    <tr key={ticket.id} className="border-b border-slate-100 last:border-0">
                      <td className="px-2 py-2 text-slate-800">{ticket.subject}</td>
                      <td className="px-2 py-2 text-slate-600">{t(`tickets.status.${ticket.status}`)}</td>
                      <td className="px-2 py-2 text-slate-600">{t(`tickets.category.${ticket.category}`)}</td>
                      <td className="px-2 py-2 text-slate-600">{t(`tickets.priority.${ticket.priority}`)}</td>
                      <td className="px-2 py-2 text-slate-600">
                        {new Date(ticket.createdAtUtc).toLocaleDateString()}
                      </td>
                      <td className="px-2 py-2 text-end">
                        <button
                          type="button"
                          onClick={() => navigate(`/tickets/${ticket.id}`)}
                          className="rounded px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-100"
                        >
                          {t('tickets.view')}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section className="rounded border border-slate-200 bg-white p-4 md:col-span-3">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
            {t('customerDetail.history.heading')}
          </h2>
          {history.length === 0 ? (
            <p className="text-sm text-slate-500">{t('ticketDetail.history.empty')}</p>
          ) : (
            <ul className="flex flex-col gap-2 text-sm">
              {history.map((h) => (
                <li key={h.id} className="flex items-center justify-between gap-2 text-slate-600">
                  <span>
                    {t(`customerDetail.history.action.${h.action}`, { defaultValue: h.action })}
                    {h.actorDisplayName ? ` — ${h.actorDisplayName}` : ''}
                  </span>
                  <span className="text-xs text-slate-400">{new Date(h.timestampUtc).toLocaleString()}</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  );
}
