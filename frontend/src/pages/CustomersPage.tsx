import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  type Customer,
  type CustomerUpsert,
  createCustomer,
  deleteCustomer,
  listCustomers,
  updateCustomer,
} from '../api/customers';
import { useAuthStore } from '../store/useAuthStore';

const EMPTY_FORM: CustomerUpsert = { fullName: '', email: '', phone: '' };

// Practical, non-RFC-5322 email check: local@domain.tld, no whitespace. Matches the
// project's existing preference (backend's [EmailAddress] attribute) for a sensible
// standard check over an exhaustive regex.
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function isValidEmail(value: string): boolean {
  return EMAIL_PATTERN.test(value);
}

// Optional field. Accepts digits plus common formatting characters (+, spaces, -, parens);
// rejects letters/other text via the character-class check, and rejects values with too few
// digits to plausibly be a phone number. Upper bound follows ITU E.164 (max 15 digits) so
// arbitrarily long digit strings aren't accepted either, without constraining to one country's format.
const PHONE_ALLOWED_CHARS = /^[+()\-\s\d]+$/;

function isValidPhone(value: string): boolean {
  if (!value) return true;
  if (!PHONE_ALLOWED_CHARS.test(value)) return false;
  const digitCount = value.replace(/\D/g, '').length;
  return digitCount >= 7 && digitCount <= 15;
}

interface FormErrors {
  fullName?: string;
  email?: string;
  phone?: string;
}

function validateForm(values: CustomerUpsert): FormErrors {
  const errors: FormErrors = {};

  if (!values.fullName.trim()) {
    errors.fullName = 'customers.validation.nameRequired';
  }

  const email = values.email.trim();
  if (!email) {
    errors.email = 'customers.validation.emailRequired';
  } else if (!isValidEmail(email)) {
    errors.email = 'customers.validation.emailInvalid';
  }

  const phone = (values.phone ?? '').trim();
  if (phone && !isValidPhone(phone)) {
    errors.phone = 'customers.validation.phoneInvalid';
  }

  return errors;
}

export default function CustomersPage() {
  const { t } = useTranslation();
  const isAdmin = useAuthStore((s) => s.hasRole('Admin'));

  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editing, setEditing] = useState<Customer | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [formValues, setFormValues] = useState<CustomerUpsert>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [touched, setTouched] = useState<{ email: boolean; phone: boolean }>({ email: false, phone: false });
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);

  const fieldErrors = validateForm(formValues);
  const showNameError = attemptedSubmit && fieldErrors.fullName;
  const showEmailError = (touched.email || attemptedSubmit) && fieldErrors.email;
  const showPhoneError = (touched.phone || attemptedSubmit) && fieldErrors.phone;

  const loadCustomers = () => {
    setLoading(true);
    setError(null);
    listCustomers()
      .then(setCustomers)
      .catch(() => setError(t('customers.errors.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadCustomers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const openCreateForm = () => {
    setEditing(null);
    setFormValues(EMPTY_FORM);
    setFormError(null);
    setTouched({ email: false, phone: false });
    setAttemptedSubmit(false);
    setFormOpen(true);
  };

  const openEditForm = (customer: Customer) => {
    setEditing(customer);
    setFormValues({ fullName: customer.fullName, email: customer.email, phone: customer.phone ?? '' });
    setFormError(null);
    setTouched({ email: false, phone: false });
    setAttemptedSubmit(false);
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

    const errors = validateForm(formValues);
    if (Object.keys(errors).length > 0) {
      setAttemptedSubmit(true);
      return;
    }

    const payload: CustomerUpsert = {
      fullName: formValues.fullName.trim(),
      email: formValues.email.trim(),
      phone: formValues.phone?.trim() || null,
    };

    setSubmitting(true);
    try {
      if (editing) {
        await updateCustomer(editing.id, payload);
      } else {
        await createCustomer(payload);
      }
      closeForm();
      loadCustomers();
    } catch (err) {
      const data = (err as { response?: { data?: { error?: string } } })?.response?.data;
      setFormError(
        data?.error === 'name_required'
          ? t('customers.validation.nameRequired')
          : t('customers.errors.saveFailed'),
      );
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (customer: Customer) => {
    if (!window.confirm(t('customers.deleteConfirm'))) return;
    try {
      await deleteCustomer(customer.id);
      loadCustomers();
    } catch {
      setError(t('customers.errors.deleteFailed'));
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('customers.title')}</h1>
        <button
          type="button"
          onClick={openCreateForm}
          className="flex items-center gap-1.5 rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
        >
          <PlusIcon />
          {t('customers.new')}
        </button>
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {customers.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('customers.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('customers.fields.fullName')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('customers.fields.email')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('customers.fields.phone')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">
                    {t('customers.fields.createdAt')}
                  </th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500" />
                </tr>
              </thead>
              <tbody>
                {customers.map((customer) => (
                  <tr
                    key={customer.id}
                    className="border-b border-slate-100 transition-colors last:border-0 hover:bg-slate-50"
                  >
                    <td className="px-4 py-3 text-slate-800">{customer.fullName}</td>
                    <td className="px-4 py-3 text-slate-600">{customer.email}</td>
                    <td className="px-4 py-3 text-slate-600">{customer.phone ?? ''}</td>
                    <td className="px-4 py-3 text-slate-600">
                      {new Date(customer.createdAtUtc).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => openEditForm(customer)}
                          className="rounded px-2.5 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
                        >
                          {t('customers.edit')}
                        </button>
                        {isAdmin && (
                          <button
                            type="button"
                            onClick={() => handleDelete(customer)}
                            className="rounded px-2.5 py-1.5 text-sm font-medium text-red-600 transition-colors hover:bg-red-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-600"
                          >
                            {t('customers.delete')}
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
              {editing ? t('customers.edit') : t('customers.new')}
            </h2>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('customers.fields.fullName')}</span>
              <input
                type="text"
                value={formValues.fullName}
                onChange={(e) => setFormValues((v) => ({ ...v, fullName: e.target.value }))}
                aria-invalid={Boolean(showNameError)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              {showNameError && <span className="text-sm text-red-600">{t(fieldErrors.fullName!)}</span>}
            </label>
            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('customers.fields.email')}</span>
              <input
                type="email"
                value={formValues.email}
                onChange={(e) => setFormValues((v) => ({ ...v, email: e.target.value }))}
                onBlur={() => setTouched((v) => ({ ...v, email: true }))}
                aria-invalid={Boolean(showEmailError)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              {showEmailError && <span className="text-sm text-red-600">{t(fieldErrors.email!)}</span>}
            </label>
            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('customers.fields.phone')}</span>
              <input
                type="tel"
                value={formValues.phone ?? ''}
                onChange={(e) => setFormValues((v) => ({ ...v, phone: e.target.value }))}
                onBlur={() => setTouched((v) => ({ ...v, phone: true }))}
                aria-invalid={Boolean(showPhoneError)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              {showPhoneError && <span className="text-sm text-red-600">{t(fieldErrors.phone!)}</span>}
            </label>

            {formError && <p className="text-sm text-red-600">{formError}</p>}

            <div className="flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={closeForm}
                className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
              >
                {t('customers.actions.cancel')}
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
              >
                {t('customers.actions.save')}
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
