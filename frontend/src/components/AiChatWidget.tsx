import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { sendAiChatMessage } from '../api/ai';

interface ChatEntry {
  id: string;
  role: 'user' | 'assistant';
  text: string;
}

// Distinct from ChatWidget.tsx: this is a stateless request/response AI conversation
// (no SignalR, no ChatHub, not tied to any ticket) - a floating, portal-wide self-service
// assistant. sessionId is a client-generated, per-mount identifier with no server-side
// account tie, matching the anonymous AiChatController endpoint it calls.
export default function AiChatWidget() {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [sessionId] = useState(() => crypto.randomUUID());
  const [entries, setEntries] = useState<ChatEntry[]>([]);
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);
  const [notConfigured, setNotConfigured] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSend = async (e: FormEvent) => {
    e.preventDefault();
    const message = input.trim();
    if (!message) return;

    setError(null);
    setEntries((prev) => [...prev, { id: crypto.randomUUID(), role: 'user', text: message }]);
    setInput('');
    setSending(true);
    try {
      const result = await sendAiChatMessage(sessionId, message);
      if (result.status === 'NotConfigured') {
        setNotConfigured(true);
      } else if (result.status === 'Ok' && result.value) {
        setEntries((prev) => [...prev, { id: crypto.randomUUID(), role: 'assistant', text: result.value! }]);
      } else {
        setError(t('ai.error'));
      }
    } catch {
      setError(t('ai.error'));
    } finally {
      setSending(false);
    }
  };

  return (
    <div className="fixed bottom-4 end-4 z-40">
      {open ? (
        <div className="flex h-96 w-80 max-w-[90vw] flex-col rounded-md border border-slate-200 bg-white shadow-lg">
          <div className="flex items-center justify-between border-b border-slate-200 px-3 py-2">
            <span className="text-sm font-semibold text-slate-800">{t('ai.chat.title')}</span>
            <button
              type="button"
              onClick={() => setOpen(false)}
              aria-label={t('ai.dismiss') ?? ''}
              className="rounded p-1 text-slate-500 hover:bg-slate-100"
            >
              <CloseIcon />
            </button>
          </div>

          <div className="flex flex-1 flex-col gap-2 overflow-y-auto p-3">
            {notConfigured ? (
              <p className="text-sm text-slate-500">{t('ai.notConfigured')}</p>
            ) : entries.length === 0 ? (
              <p className="text-sm text-slate-500">{t('ai.chat.placeholder')}</p>
            ) : (
              entries.map((entry) => (
                <div
                  key={entry.id}
                  className={`max-w-[85%] rounded px-3 py-2 text-sm ${
                    entry.role === 'user'
                      ? 'self-end bg-slate-800 text-white'
                      : 'self-start border border-slate-200 bg-slate-50 text-slate-800'
                  }`}
                >
                  <p className="whitespace-pre-wrap">{entry.text}</p>
                </div>
              ))
            )}
            {sending && <p className="text-xs text-slate-400">{t('ai.loading')}</p>}
            {error && <p className="text-sm text-red-600">{error}</p>}
          </div>

          <form onSubmit={handleSend} className="flex gap-2 border-t border-slate-200 p-2">
            <input
              type="text"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder={t('ai.chat.placeholder') ?? ''}
              disabled={notConfigured || sending}
              className="flex-1 rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 disabled:opacity-60"
            />
            <button
              type="submit"
              disabled={notConfigured || sending || !input.trim()}
              className="rounded bg-slate-800 px-3 py-2 text-sm font-medium text-white disabled:opacity-60"
            >
              {t('ai.chat.send')}
            </button>
          </form>
        </div>
      ) : (
        <button
          type="button"
          onClick={() => setOpen(true)}
          aria-label={t('ai.chat.title') ?? ''}
          className="rounded-full bg-slate-800 p-3 text-white shadow-lg transition-colors hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
        >
          <AiIcon />
        </button>
      )}
    </div>
  );
}

function AiIcon() {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M12 2a7 7 0 0 0-7 7c0 3 2 5 2 8h10c0-3 2-5 2-8a7 7 0 0 0-7-7Z" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M9 21h6" strokeLinecap="round" />
    </svg>
  );
}

function CloseIcon() {
  return (
    <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M6 6l12 12M18 6L6 18" strokeLinecap="round" />
    </svg>
  );
}
