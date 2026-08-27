import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  TICKET_STATUSES,
  type Ticket,
  type TicketStatus,
  createTicket,
  deleteTicket,
  listTickets,
  updateTicket,
} from '../api/tickets';
import { type Customer, listCustomers } from '../api/customers';
import { useAuthStore } from '../store/useAuthStore';

interface TicketFormValues {
  customerId: string;
  subject: string;
  description: string;
  status: TicketStatus;
}

const EMPTY_FORM: TicketFormValues = { customerId: '', subject: '', description: '', status: 'Open' };

interface FormErrors {
  customerId?: string;
  subject?: string;
  description?: string;
  status?: string;
}

function validateForm(values: TicketFormValues, customerIds: Set<string>): FormErrors {
  const errors: FormErrors = {};

  if (!values.customerId || !customerIds.has(values.customerId)) {
    errors.customerId = 'tickets.errors.customer_not_found';
  }

  const subject = values.subject.trim();
  if (!subject) {
    errors.subject = 'tickets.errors.subject_required';
  } else if (subject.length > 200) {
    errors.subject = 'tickets.errors.subject_max';
  }

  if ((values.description ?? '').length > 4000) {
    errors.description = 'tickets.errors.description_max';
  }

  if (!TICKET_STATUSES.includes(values.status)) {
    errors.status = 'tickets.errors.status_invalid';
  }

  return errors;
}

export default function TicketsPage() {
  const { t } = useTranslation();
  const isAdmin = useAuthStore((s) => s.hasRole('Admin'));

  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editing, setEditing] = useState<Ticket | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [formValues, setFormValues] = useState<TicketFormValues>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [touched, setTouched] = useState<{ customerId: boolean; subject: boolean; description: boolean }>({
    customerId: false,
    subject: false,
    description: false,
  });
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);

  const customerIds = new Set(customers.map((c) => c.id));
  const fieldErrors = validateForm(formValues, customerIds);
  const showCustomerError = (touched.customerId || attemptedSubmit) && fieldErrors.customerId;
  const showSubjectError = (touched.subject || attemptedSubmit) && fieldErrors.subject;
  const showDescriptionError = (touched.description || attemptedSubmit) && fieldErrors.description;

  const loadData = () => {
    setLoading(true);
    setError(null);
    Promise.all([listTickets(), listCustomers()])
      .then(([ticketData, customerData]) => {
        setTickets(ticketData);
        setCustomers(customerData);
      })
      .catch(() => setError(t('tickets.errors.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const resetFormState = () => {
    setFormError(null);
    setTouched({ customerId: false, subject: false, description: false });
    setAttemptedSubmit(false);
  };

  const openCreateForm = () => {
    setEditing(null);
    setFormValues(EMPTY_FORM);
    resetFormState();
    setFormOpen(true);
  };

  const openEditForm = (ticket: Ticket) => {
    setEditing(ticket);
    setFormValues({
      customerId: ticket.customerId,
      subject: ticket.subject,
      description: ticket.description ?? '',
      status: ticket.status,
    });
    resetFormState();
    setFormOpen(true);
  };

  const closeForm = () => {
    setFormOpen(false);
    setEditing(null);
    setFormError(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError(null);

    const errors = validateForm(formValues, customerIds);
    if (Object.keys(errors).length > 0) {
      setAttemptedSubmit(true);
      return;
    }

    const payload = {
      customerId: formValues.customerId,
      subject: formValues.subject.trim(),
      description: formValues.description.trim() || null,
      status: formValues.status,
    };

    setSubmitting(true);
    try {
      if (editing) {
        await updateTicket(editing.id, payload);
      } else {
        await createTicket(payload);
      }
      closeForm();
      loadData();
    } catch (err) {
      const code = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      const knownCodes = ['subject_required', 'status_invalid', 'customer_not_found', 'ticket_not_found'];
      setFormError(
        code && knownCodes.includes(code) ? t(`tickets.errors.${code}`) : t('tickets.errors.saveFailed'),
      );
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (ticket: Ticket) => {
    if (!window.confirm(t('tickets.deleteConfirm'))) return;
    try {
      await deleteTicket(ticket.id);
      loadData();
    } catch {
      setError(t('tickets.errors.deleteFailed'));
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('tickets.title')}</h1>
        <button
          type="button"
          onClick={openCreateForm}
          className="flex items-center gap-1.5 rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
        >
          <PlusIcon />
          {t('tickets.new')}
        </button>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {tickets.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('tickets.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('tickets.columns.subject')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('tickets.columns.customer')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('tickets.columns.status')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('tickets.columns.createdAt')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500" />
                </tr>
              </thead>
              <tbody>
                {tickets.map((ticket) => (
                  <tr
                    key={ticket.id}
                    className="border-b border-slate-100 transition-colors last:border-0 hover:bg-slate-50"
                  >
                    <td className="px-4 py-3 text-slate-800">{ticket.subject}</td>
                    <td className="px-4 py-3 text-slate-600">{ticket.customerFullName}</td>
                    <td className="px-4 py-3 text-slate-600">{t(`tickets.status.${ticket.status}`)}</td>
                    <td className="px-4 py-3 text-slate-600">
                      {new Date(ticket.createdAtUtc).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => openEditForm(ticket)}
                          className="rounded px-2.5 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
                        >
                          {t('tickets.edit')}
                        </button>
                        {isAdmin && (
                          <button
                            type="button"
                            onClick={() => handleDelete(ticket)}
                            className="rounded px-2.5 py-1.5 text-sm font-medium text-red-600 transition-colors hover:bg-red-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-600"
                          >
                            {t('tickets.delete')}
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {formOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 px-4"
          onClick={closeForm}
        >
          <form
            onSubmit={handleSubmit}
            onClick={(e) => e.stopPropagation()}
            noValidate
            className="flex w-full max-w-sm flex-col gap-4 rounded bg-white p-6 shadow-sm"
          >
            <h2 className="text-lg font-semibold text-slate-800">
              {editing ? t('tickets.edit') : t('tickets.new')}
            </h2>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('tickets.form.customer')}</span>
              <select
                value={formValues.customerId}
                onChange={(e) => setFormValues((v) => ({ ...v, customerId: e.target.value }))}
                onBlur={() => setTouched((v) => ({ ...v, customerId: true }))}
                aria-invalid={Boolean(showCustomerError)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              >
                <option value="">{t('tickets.form.customerPlaceholder')}</option>
                {customers.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.fullName}
                  </option>
                ))}
              </select>
              {showCustomerError && <span className="text-sm text-red-600">{t(fieldErrors.customerId!)}</span>}
            </label>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('tickets.form.subject')}</span>
              <input
                type="text"
                value={formValues.subject}
                onChange={(e) => setFormValues((v) => ({ ...v, subject: e.target.value }))}
                onBlur={() => setTouched((v) => ({ ...v, subject: true }))}
                aria-invalid={Boolean(showSubjectError)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              {showSubjectError && <span className="text-sm text-red-600">{t(fieldErrors.subject!)}</span>}
            </label>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('tickets.form.description')}</span>
              <textarea
                value={formValues.description}
                onChange={(e) => setFormValues((v) => ({ ...v, description: e.target.value }))}
                onBlur={() => setTouched((v) => ({ ...v, description: true }))}
                aria-invalid={Boolean(showDescriptionError)}
                rows={3}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              {showDescriptionError && (
                <span className="text-sm text-red-600">{t(fieldErrors.description!)}</span>
              )}
            </label>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('tickets.form.status')}</span>
              <select
                value={formValues.status}
                onChange={(e) => setFormValues((v) => ({ ...v, status: e.target.value as TicketStatus }))}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              >
                {TICKET_STATUSES.map((status) => (
                  <option key={status} value={status}>
                    {t(`tickets.status.${status}`)}
                  </option>
                ))}
              </select>
            </label>

            {formError && <p className="text-sm text-red-600">{formError}</p>}

            <div className="flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={closeForm}
                className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
              >
                {t('tickets.actions.cancel')}
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
              >
                {t('tickets.actions.save')}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

function PlusIcon() {
  return (
    <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M12 5v14M5 12h14" strokeLinecap="round" />
    </svg>
  );
}
