import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { getMyRequest, submitFeedback, type PortalTicketDetail } from '../../api/portal';

const RATINGS = [1, 2, 3, 4, 5];

export default function MyRequestDetailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const [ticket, setTicket] = useState<PortalTicketDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState('');
  const [feedbackSubmitting, setFeedbackSubmitting] = useState(false);
  const [feedbackError, setFeedbackError] = useState<string | null>(null);

  const load = () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    getMyRequest(id)
      .then(setTicket)
      .catch(() => setError(t('portal.myRequestDetail.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleSubmitFeedback = async (event: FormEvent) => {
    event.preventDefault();
    if (!id) return;
    setFeedbackError(null);
    setFeedbackSubmitting(true);
    try {
      await submitFeedback(id, rating, comment.trim() || null);
      load();
    } catch {
      setFeedbackError(t('portal.feedback.saveFailed'));
    } finally {
      setFeedbackSubmitting(false);
    }
  };

  if (loading) return null;

  if (!ticket) {
    return (
      <div className="flex flex-col gap-4">
        <p className="text-sm text-red-600">{error ?? t('portal.myRequestDetail.notFound')}</p>
        <button
          type="button"
          onClick={() => navigate('/portal/my-requests')}
          className="w-fit rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
        >
          {t('portal.myRequestDetail.back')}
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <button
        type="button"
        onClick={() => navigate('/portal/my-requests')}
        className="w-fit text-sm font-medium text-slate-600 hover:text-slate-800"
      >
        {t('portal.myRequestDetail.back')}
      </button>

      <div className="flex flex-col gap-2">
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-2xl font-semibold text-slate-800">{ticket.subject}</h1>
          <span className="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
            {t(`tickets.status.${ticket.status}`)}
          </span>
          <span className="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
            {t(`tickets.priority.${ticket.priority}`)}
          </span>
        </div>
        {ticket.description && <p className="text-sm text-slate-600">{ticket.description}</p>}
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      <section className="rounded border border-slate-200 bg-white p-4">
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
          {t('portal.myRequestDetail.history')}
        </h2>
        {ticket.history.length === 0 ? (
          <p className="text-sm text-slate-500">{t('portal.myRequestDetail.historyEmpty')}</p>
        ) : (
          <ul className="flex flex-col gap-2 text-sm">
            {ticket.history.map((h, i) => (
              <li key={i} className="flex items-center justify-between gap-2 text-slate-600">
                <span>{t(`ticketDetail.history.action.${h.action}`, { defaultValue: h.action })}</span>
                <span className="text-xs text-slate-400">{new Date(h.timestampUtc).toLocaleString()}</span>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="rounded border border-slate-200 bg-white p-4">
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
          {t('portal.feedback.heading')}
        </h2>

        {ticket.feedback ? (
          <div className="flex flex-col gap-1 text-sm text-slate-700">
            <span className="font-medium text-slate-800">
              {t('portal.feedback.yourRating')}: {ticket.feedback.rating} / 5
            </span>
            {ticket.feedback.comment && <p className="text-slate-600">{ticket.feedback.comment}</p>}
          </div>
        ) : ticket.status === 'Closed' ? (
          <form onSubmit={handleSubmitFeedback} className="flex flex-col gap-3">
            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('portal.feedback.rating')}</span>
              <select
                value={rating}
                onChange={(e) => setRating(Number(e.target.value))}
                className="w-32 rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              >
                {RATINGS.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </label>
            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('portal.feedback.comment')}</span>
              <textarea
                value={comment}
                onChange={(e) => setComment(e.target.value)}
                rows={3}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
            </label>
            {feedbackError && <p className="text-sm text-red-600">{feedbackError}</p>}
            <button
              type="submit"
              disabled={feedbackSubmitting}
              className="w-fit rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
            >
              {t('portal.feedback.submit')}
            </button>
          </form>
        ) : (
          <p className="text-sm text-slate-500">{t('portal.feedback.onlyOnClosed')}</p>
        )}
      </section>
    </div>
  );
}
