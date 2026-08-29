import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import {
  type HistoryEntry,
  type MentionableUser,
  type Ticket,
  type TicketAttachment,
  type TicketNote,
  type TicketTask,
  createTicketNote,
  createTicketTask,
  deleteTicketAttachment,
  deleteTicketTask,
  downloadTicketAttachment,
  getTicket,
  getTicketHistory,
  listMentionableUsers,
  listTicketAttachments,
  listTicketNotes,
  listTicketTasks,
  updateTicketTask,
  uploadTicketAttachment,
} from '../api/tickets';
import { type Customer, getCustomer } from '../api/customers';
import { listQuickReplies, type QuickReplyTemplate } from '../api/quickReplies';
import { type AiKbArticleSuggestion, suggestKbArticles, suggestReply, summarizeTicket } from '../api/ai';
import ChatWidget from '../components/ChatWidget';

const MAX_ATTACHMENT_BYTES = 5 * 1024 * 1024;

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  return `${(bytes / 1024).toFixed(1)} KB`;
}

export default function TicketDetailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const [ticket, setTicket] = useState<Ticket | null>(null);
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [notes, setNotes] = useState<TicketNote[]>([]);
  const [attachments, setAttachments] = useState<TicketAttachment[]>([]);
  const [tasks, setTasks] = useState<TicketTask[]>([]);
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  const [quickReplies, setQuickReplies] = useState<QuickReplyTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [noteBody, setNoteBody] = useState('');
  const [selectedQuickReply, setSelectedQuickReply] = useState('');
  const [noteSubmitting, setNoteSubmitting] = useState(false);

  const [aiLoading, setAiLoading] = useState<'summarize' | 'reply' | 'kb' | null>(null);
  const [aiResult, setAiResult] = useState<
    { kind: 'summarize' | 'reply'; text: string } | { kind: 'kb'; articles: AiKbArticleSuggestion[] } | null
  >(null);
  const [aiNotConfigured, setAiNotConfigured] = useState(false);
  const [aiError, setAiError] = useState<string | null>(null);

  const [mentionQuery, setMentionQuery] = useState<string | null>(null);
  const [mentionRange, setMentionRange] = useState<{ start: number; end: number } | null>(null);
  const [mentionCandidates, setMentionCandidates] = useState<MentionableUser[]>([]);
  const [mentionedUsers, setMentionedUsers] = useState<Map<string, MentionableUser>>(new Map());

  const [attachmentError, setAttachmentError] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);

  const [taskTitle, setTaskTitle] = useState('');
  const [taskDue, setTaskDue] = useState('');

  const loadAll = () => {
    if (!id) return;
    setLoading(true);
    setError(null);
    getTicket(id)
      .then((t) => {
        setTicket(t);
        return Promise.all([
          getCustomer(t.customerId),
          listTicketNotes(id),
          listTicketAttachments(id),
          listTicketTasks(id),
          getTicketHistory(id),
          listQuickReplies(),
        ]);
      })
      .then(([customerData, notesData, attachmentsData, tasksData, historyData, quickReplyData]) => {
        setCustomer(customerData);
        setNotes(notesData);
        setAttachments(attachmentsData);
        setTasks(tasksData);
        setHistory(historyData);
        setQuickReplies(quickReplyData);
      })
      .catch(() => setError(t('ticketDetail.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleQuickReplyChange = (e: ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    setSelectedQuickReply(value);
    const template = quickReplies.find((q) => q.id === value);
    if (template) {
      setNoteBody(template.body);
    }
  };

  // Detects an in-progress "@word" token ending at the cursor - the last "@" before the
  // cursor with no whitespace between it and the cursor.
  const detectMentionRange = (text: string, cursorPos: number): { start: number; end: number } | null => {
    const upToCursor = text.slice(0, cursorPos);
    const at = upToCursor.lastIndexOf('@');
    if (at === -1) return null;
    const token = upToCursor.slice(at + 1);
    if (/\s/.test(token)) return null;
    return { start: at, end: cursorPos };
  };

  const handleNoteBodyChange = (e: ChangeEvent<HTMLTextAreaElement>) => {
    const value = e.target.value;
    setNoteBody(value);
    const range = detectMentionRange(value, e.target.selectionStart);
    setMentionRange(range);
    const query = range ? value.slice(range.start + 1, range.end) : null;
    setMentionQuery(query);
    if (query !== null) {
      listMentionableUsers(query || undefined)
        .then(setMentionCandidates)
        .catch(() => setMentionCandidates([]));
    }
  };

  const handleSelectMention = (candidate: MentionableUser) => {
    if (!mentionRange) return;
    const before = noteBody.slice(0, mentionRange.start);
    const after = noteBody.slice(mentionRange.end);
    setNoteBody(`${before}@${candidate.displayName} ${after}`);
    setMentionedUsers((prev) => new Map(prev).set(candidate.id, candidate));
    setMentionQuery(null);
    setMentionRange(null);
    setMentionCandidates([]);
  };

  const handleSummarize = async () => {
    if (!id) return;
    setAiLoading('summarize');
    setAiError(null);
    setAiNotConfigured(false);
    try {
      const result = await summarizeTicket(id);
      if (result.status === 'NotConfigured') setAiNotConfigured(true);
      else if (result.status === 'Ok') setAiResult({ kind: 'summarize', text: result.value ?? '' });
      else setAiError(t('ai.error'));
    } catch {
      setAiError(t('ai.error'));
    } finally {
      setAiLoading(null);
    }
  };

  const handleSuggestReply = async () => {
    if (!id) return;
    setAiLoading('reply');
    setAiError(null);
    setAiNotConfigured(false);
    try {
      const result = await suggestReply(id);
      if (result.status === 'NotConfigured') setAiNotConfigured(true);
      else if (result.status === 'Ok') setAiResult({ kind: 'reply', text: result.value ?? '' });
      else setAiError(t('ai.error'));
    } catch {
      setAiError(t('ai.error'));
    } finally {
      setAiLoading(null);
    }
  };

  const handleSuggestKb = async () => {
    if (!id) return;
    setAiLoading('kb');
    setAiError(null);
    setAiNotConfigured(false);
    try {
      const result = await suggestKbArticles(id);
      if (result.status === 'NotConfigured') setAiNotConfigured(true);
      else if (result.status === 'Ok') setAiResult({ kind: 'kb', articles: result.value ?? [] });
      else setAiError(t('ai.error'));
    } catch {
      setAiError(t('ai.error'));
    } finally {
      setAiLoading(null);
    }
  };

  // The agent must explicitly click Apply - an AI suggestion is never auto-inserted or
  // auto-sent.
  const handleApplySuggestedReply = () => {
    if (aiResult?.kind === 'reply') {
      setNoteBody(aiResult.text);
      setAiResult(null);
    }
  };

  const handleAddNote = async (e: FormEvent) => {
    e.preventDefault();
    if (!id || !noteBody.trim()) return;
    setNoteSubmitting(true);
    try {
      // Only ids whose "@DisplayName" text still appears in the note survive - if the agent
      // deleted the mention text, they're no longer mentioned.
      const mentionedUserIds = [...mentionedUsers.values()]
        .filter((u) => noteBody.includes(`@${u.displayName}`))
        .map((u) => u.id);
      const note = await createTicketNote(id, noteBody.trim(), mentionedUserIds);
      setNotes((prev) => [...prev, note]);
      setNoteBody('');
      setSelectedQuickReply('');
      setMentionedUsers(new Map());
    } catch {
      setError(t('ticketDetail.notes.saveFailed'));
    } finally {
      setNoteSubmitting(false);
    }
  };

  const handleUpload = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file || !id) return;
    setAttachmentError(null);
    if (file.size > MAX_ATTACHMENT_BYTES) {
      setAttachmentError(t('ticketDetail.attachments.tooLarge'));
      return;
    }
    setUploading(true);
    try {
      const attachment = await uploadTicketAttachment(id, file);
      setAttachments((prev) => [...prev, attachment]);
    } catch {
      setAttachmentError(t('ticketDetail.attachments.uploadFailed'));
    } finally {
      setUploading(false);
    }
  };

  const handleDownload = async (attachment: TicketAttachment) => {
    if (!id) return;
    try {
      await downloadTicketAttachment(id, attachment.id, attachment.fileName);
    } catch {
      setAttachmentError(t('ticketDetail.attachments.downloadFailed'));
    }
  };

  const handleDeleteAttachment = async (attachment: TicketAttachment) => {
    if (!id) return;
    try {
      await deleteTicketAttachment(id, attachment.id);
      setAttachments((prev) => prev.filter((a) => a.id !== attachment.id));
    } catch {
      setAttachmentError(t('ticketDetail.attachments.deleteFailed'));
    }
  };

  const handleAddTask = async (e: FormEvent) => {
    e.preventDefault();
    if (!id || !taskTitle.trim()) return;
    try {
      const task = await createTicketTask(id, {
        title: taskTitle.trim(),
        dueAtUtc: taskDue ? new Date(taskDue).toISOString() : null,
        isDone: false,
      });
      setTasks((prev) => [...prev, task]);
      setTaskTitle('');
      setTaskDue('');
    } catch {
      setError(t('ticketDetail.tasks.saveFailed'));
    }
  };

  const handleToggleTask = async (task: TicketTask) => {
    if (!id) return;
    try {
      await updateTicketTask(id, task.id, {
        title: task.title,
        dueAtUtc: task.dueAtUtc,
        isDone: !task.isDone,
      });
      setTasks((prev) => prev.map((t2) => (t2.id === task.id ? { ...t2, isDone: !t2.isDone } : t2)));
    } catch {
      setError(t('ticketDetail.tasks.saveFailed'));
    }
  };

  const handleDeleteTask = async (task: TicketTask) => {
    if (!id) return;
    try {
      await deleteTicketTask(id, task.id);
      setTasks((prev) => prev.filter((t2) => t2.id !== task.id));
    } catch {
      setError(t('ticketDetail.tasks.saveFailed'));
    }
  };

  const taskDueClass = (task: TicketTask): string => {
    if (task.isDone || !task.dueAtUtc) return 'text-slate-600';
    const due = new Date(task.dueAtUtc);
    const now = new Date();
    const isSameDay = due.toDateString() === now.toDateString();
    if (due.getTime() < now.getTime() && !isSameDay) return 'text-red-600 font-medium';
    if (isSameDay) return 'text-amber-600 font-medium';
    return 'text-slate-600';
  };

  if (loading) return null;

  if (!ticket) {
    return (
      <div className="flex flex-col gap-4">
        <p className="text-sm text-red-600">{error ?? t('ticketDetail.notFound')}</p>
        <button
          type="button"
          onClick={() => navigate('/tickets')}
          className="w-fit rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
        >
          {t('ticketDetail.back')}
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <button
          type="button"
          onClick={() => navigate('/tickets')}
          className="w-fit text-sm font-medium text-slate-600 hover:text-slate-800"
        >
          {t('ticketDetail.back')}
        </button>
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-2xl font-semibold text-slate-800">{ticket.subject}</h1>
          <span className="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
            {t(`tickets.status.${ticket.status}`)}
          </span>
          <span className="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
            {t(`tickets.category.${ticket.category}`)}
          </span>
          <span className="rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700">
            {t(`tickets.priority.${ticket.priority}`)}
          </span>
          {ticket.isEscalated && (
            <span className="rounded bg-red-600 px-2 py-0.5 text-xs font-medium text-white">
              {t('tickets.escalatedBadge')}
            </span>
          )}
        </div>
        {ticket.description && <p className="text-sm text-slate-600">{ticket.description}</p>}
      </div>

      {error && <p className="text-sm text-red-600">{error}</p>}

      <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
        <div className="flex flex-col gap-6 md:col-span-2">
          {/* AI assistant */}
          <section className="rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">{t('ai.chat.title')}</h2>
            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={handleSummarize}
                disabled={aiLoading !== null}
                className="rounded border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-60"
              >
                {t('ai.summarize')}
              </button>
              <button
                type="button"
                onClick={handleSuggestReply}
                disabled={aiLoading !== null}
                className="rounded border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-60"
              >
                {t('ai.suggestReply')}
              </button>
              <button
                type="button"
                onClick={handleSuggestKb}
                disabled={aiLoading !== null}
                className="rounded border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-60"
              >
                {t('ai.suggestKbArticles')}
              </button>
            </div>

            {aiLoading && <p className="mt-3 text-sm text-slate-400">{t('ai.loading')}</p>}
            {aiNotConfigured && <p className="mt-3 text-sm text-slate-500">{t('ai.notConfigured')}</p>}
            {aiError && <p className="mt-3 text-sm text-red-600">{aiError}</p>}

            {aiResult && (
              <div className="mt-3 rounded bg-slate-50 p-3">
                {aiResult.kind === 'kb' ? (
                  aiResult.articles.length === 0 ? (
                    <p className="text-sm text-slate-500">{t('kb.empty')}</p>
                  ) : (
                    <ul className="flex flex-col gap-1">
                      {aiResult.articles.map((article) => (
                        <li key={article.id} className="text-sm text-slate-800">
                          {article.title}
                        </li>
                      ))}
                    </ul>
                  )
                ) : (
                  <>
                    <p className="whitespace-pre-wrap text-sm text-slate-800">{aiResult.text}</p>
                    <div className="mt-2 flex gap-2">
                      {aiResult.kind === 'reply' && (
                        <button
                          type="button"
                          onClick={handleApplySuggestedReply}
                          className="rounded bg-slate-800 px-3 py-1.5 text-sm font-medium text-white"
                        >
                          {t('ai.apply')}
                        </button>
                      )}
                      <button
                        type="button"
                        onClick={() => setAiResult(null)}
                        className="rounded px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
                      >
                        {t('ai.dismiss')}
                      </button>
                    </div>
                  </>
                )}
              </div>
            )}
          </section>

          {/* Notes / team collaboration */}
          <section className="rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
              {t('ticketDetail.notes.heading')}
            </h2>
            <div className="flex flex-col gap-3">
              {notes.length === 0 ? (
                <p className="text-sm text-slate-500">{t('ticketDetail.notes.empty')}</p>
              ) : (
                notes.map((note) => (
                  <div key={note.id} className="rounded bg-slate-50 p-3">
                    <div className="flex items-center justify-between text-xs text-slate-500">
                      <span className="font-medium text-slate-700">{note.authorDisplayName}</span>
                      <span>{new Date(note.createdAtUtc).toLocaleString()}</span>
                    </div>
                    <p className="mt-1 whitespace-pre-wrap text-sm text-slate-800">{note.body}</p>
                  </div>
                ))
              )}
            </div>
            <form onSubmit={handleAddNote} className="mt-3 flex flex-col gap-2">
              {quickReplies.length > 0 && (
                <select
                  value={selectedQuickReply}
                  onChange={handleQuickReplyChange}
                  className="rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
                >
                  <option value="">{t('ticketDetail.notes.quickReplyPlaceholder')}</option>
                  {quickReplies.map((q) => (
                    <option key={q.id} value={q.id}>
                      {q.title}
                    </option>
                  ))}
                </select>
              )}
              <div className="relative">
                <textarea
                  value={noteBody}
                  onChange={handleNoteBodyChange}
                  placeholder={t('mentions.placeholder') ?? ''}
                  rows={3}
                  className="w-full rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
                />
                {mentionQuery !== null && mentionCandidates.length > 0 && (
                  <ul className="absolute inset-x-0 top-full z-10 mt-1 max-h-48 overflow-y-auto rounded border border-slate-300 bg-white shadow-sm">
                    {mentionCandidates.map((candidate) => (
                      <li key={candidate.id}>
                        <button
                          type="button"
                          onMouseDown={(e) => e.preventDefault()}
                          onClick={() => handleSelectMention(candidate)}
                          className="flex w-full flex-col items-start px-3 py-2 text-start text-sm hover:bg-slate-100"
                        >
                          <span className="text-slate-800">{candidate.displayName}</span>
                          <span className="text-xs text-slate-500">{candidate.email}</span>
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              <button
                type="submit"
                disabled={noteSubmitting || !noteBody.trim()}
                className="w-fit rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
              >
                {t('ticketDetail.notes.send')}
              </button>
            </form>
          </section>

          {/* Attachments */}
          <section className="rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
              {t('ticketDetail.attachments.heading')}
            </h2>
            {attachments.length === 0 ? (
              <p className="text-sm text-slate-500">{t('ticketDetail.attachments.empty')}</p>
            ) : (
              <ul className="flex flex-col gap-2">
                {attachments.map((a) => (
                  <li key={a.id} className="flex items-center justify-between gap-2 rounded bg-slate-50 p-2 text-sm">
                    <div className="flex flex-col">
                      <span className="font-medium text-slate-800">{a.fileName}</span>
                      <span className="text-xs text-slate-500">
                        {formatBytes(a.sizeBytes)} &middot; {a.uploadedByDisplayName} &middot;{' '}
                        {new Date(a.createdAtUtc).toLocaleDateString()}
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={() => handleDownload(a)}
                        className="rounded px-2 py-1 text-xs font-medium text-slate-700 hover:bg-slate-100"
                      >
                        {t('ticketDetail.attachments.download')}
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDeleteAttachment(a)}
                        className="rounded px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50"
                      >
                        {t('ticketDetail.attachments.delete')}
                      </button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
            {attachmentError && <p className="mt-2 text-sm text-red-600">{attachmentError}</p>}
            <label className="mt-3 flex w-fit cursor-pointer items-center gap-2 rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100">
              {uploading ? t('ticketDetail.attachments.uploading') : t('ticketDetail.attachments.upload')}
              <input type="file" onChange={handleUpload} disabled={uploading} className="hidden" />
            </label>
          </section>

          {/* History */}
          <section className="rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
              {t('ticketDetail.history.heading')}
            </h2>
            {history.length === 0 ? (
              <p className="text-sm text-slate-500">{t('ticketDetail.history.empty')}</p>
            ) : (
              <ul className="flex flex-col gap-2 text-sm">
                {history.map((h) => (
                  <li key={h.id} className="flex items-center justify-between gap-2 text-slate-600">
                    <span>
                      {t(`ticketDetail.history.action.${h.action}`, { defaultValue: h.action })}
                      {h.actorDisplayName ? ` — ${h.actorDisplayName}` : ''}
                    </span>
                    <span className="text-xs text-slate-400">{new Date(h.timestampUtc).toLocaleString()}</span>
                  </li>
                ))}
              </ul>
            )}
          </section>

          {/* Live chat (Story 12) */}
          {ticket && <ChatWidget ticketId={ticket.id} side="staff" />}
        </div>

        <div className="flex flex-col gap-6">
          {/* SLA */}
          <section className="rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
              {t('ticketDetail.sla.heading')}
            </h2>
            <div className="flex flex-col gap-2 text-sm text-slate-700">
              {ticket.isEscalated && (
                <p className="rounded bg-red-50 px-2 py-1 text-sm font-medium text-red-700">
                  {t('ticketDetail.sla.escalated')}
                </p>
              )}
              <div className="flex items-center justify-between">
                <span className="text-slate-500">{t('ticketDetail.sla.responseDue')}</span>
                <span>{new Date(ticket.responseDueAtUtc).toLocaleString()}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-500">{t('ticketDetail.sla.resolutionDue')}</span>
                <span>{new Date(ticket.resolutionDueAtUtc).toLocaleString()}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-500">{t('ticketDetail.sla.firstResponded')}</span>
                <span>
                  {ticket.firstRespondedAtUtc
                    ? new Date(ticket.firstRespondedAtUtc).toLocaleString()
                    : t('ticketDetail.sla.notYet')}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-500">{t('ticketDetail.sla.resolved')}</span>
                <span>
                  {ticket.resolvedAtUtc ? new Date(ticket.resolvedAtUtc).toLocaleString() : t('ticketDetail.sla.notYet')}
                </span>
              </div>
            </div>
          </section>

          {/* Customer context */}
          <section className="rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
              {t('ticketDetail.customerContext')}
            </h2>
            {customer && (
              <div className="flex flex-col gap-1 text-sm text-slate-700">
                <span className="font-medium text-slate-800">{customer.fullName}</span>
                <span>{customer.email}</span>
                {customer.phone && <span>{customer.phone}</span>}
                <button
                  type="button"
                  onClick={() => navigate(`/customers/${customer.id}`)}
                  className="mt-2 w-fit text-sm font-medium text-slate-700 underline hover:text-slate-900"
                >
                  {t('ticketDetail.viewCustomer')}
                </button>
              </div>
            )}
          </section>

          {/* Tasks */}
          <section className="rounded border border-slate-200 bg-white p-4">
            <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
              {t('ticketDetail.tasks.heading')}
            </h2>
            {tasks.length === 0 ? (
              <p className="text-sm text-slate-500">{t('ticketDetail.tasks.empty')}</p>
            ) : (
              <ul className="flex flex-col gap-2">
                {tasks.map((task) => (
                  <li key={task.id} className="flex items-start gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={task.isDone}
                      onChange={() => handleToggleTask(task)}
                      className="mt-1"
                    />
                    <div className="flex flex-1 flex-col">
                      <span className={task.isDone ? 'text-slate-400 line-through' : 'text-slate-800'}>
                        {task.title}
                      </span>
                      {task.dueAtUtc && (
                        <span className={`text-xs ${taskDueClass(task)}`}>
                          {t('ticketDetail.tasks.dueLabel')}: {new Date(task.dueAtUtc).toLocaleDateString()}
                        </span>
                      )}
                    </div>
                    <button
                      type="button"
                      onClick={() => handleDeleteTask(task)}
                      className="text-xs font-medium text-red-600 hover:underline"
                    >
                      {t('ticketDetail.attachments.delete')}
                    </button>
                  </li>
                ))}
              </ul>
            )}
            <form onSubmit={handleAddTask} className="mt-3 flex flex-col gap-2">
              <input
                type="text"
                value={taskTitle}
                onChange={(e) => setTaskTitle(e.target.value)}
                placeholder={t('ticketDetail.tasks.titlePlaceholder') ?? ''}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
              />
              <input
                type="date"
                value={taskDue}
                onChange={(e) => setTaskDue(e.target.value)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
              />
              <button
                type="submit"
                disabled={!taskTitle.trim()}
                className="w-fit rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
              >
                {t('ticketDetail.tasks.add')}
              </button>
            </form>
          </section>
        </div>
      </div>
    </div>
  );
}
