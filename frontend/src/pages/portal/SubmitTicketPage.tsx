import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { submitTicket } from '../../api/portal';

const PRIORITIES = ['Low', 'Normal', 'High', 'Urgent'] as const;

export default function SubmitTicketPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [subject, setSubject] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<string>('Normal');
  const [attemptedSubmit, setAttemptedSubmit] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const subjectError = !subject.trim() ? 'portal.submitTicket.errors.subjectRequired' : null;

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    if (subjectError) {
      setAttemptedSubmit(true);
      return;
    }

    setSubmitting(true);
    try {
      const ticket = await submitTicket({
        subject: subject.trim(),
        description: description.trim() || null,
        priority,
      });
      navigate(`/portal/my-requests/${ticket.id}`, { replace: true });
    } catch {
      setError(t('portal.submitTicket.errors.saveFailed'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('portal.submitTicket.title')}</h1>

      <form onSubmit={handleSubmit} noValidate className="flex w-full max-w-lg flex-col gap-4">
        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('portal.submitTicket.subject')}</span>
          <input
            type="text"
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
          {attemptedSubmit && subjectError && <span className="text-sm text-red-600">{t(subjectError)}</span>}
        </label>

        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('portal.submitTicket.description')}</span>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={4}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm text-slate-700">
          <span>{t('portal.submitTicket.priority')}</span>
          <select
            value={priority}
            onChange={(e) => setPriority(e.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
          >
            {PRIORITIES.map((p) => (
              <option key={p} value={p}>
                {t(`tickets.priority.${p}`)}
              </option>
            ))}
          </select>
        </label>

        {error && <p className="text-sm text-red-600">{error}</p>}

        <button
          type="submit"
          disabled={submitting}
          className="w-fit rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
        >
          {t('portal.submitTicket.submit')}
        </button>
      </form>
    </div>
  );
}
