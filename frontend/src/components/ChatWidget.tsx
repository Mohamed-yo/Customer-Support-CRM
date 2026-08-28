import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { HubConnection } from '@microsoft/signalr';
import { createChatConnection, getChatHistory, type ChatMessage, type ChatSide } from '../api/chat';

interface Props {
  ticketId: string;
  side: ChatSide;
}

export default function ChatWidget({ ticketId, side }: Props) {
  const { t } = useTranslation();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [connected, setConnected] = useState(false);
  const [input, setInput] = useState('');
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);
  const listEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let cancelled = false;

    getChatHistory(ticketId, side)
      .then((history) => {
        if (!cancelled) setMessages(history);
      })
      .catch(() => {
        if (!cancelled) setError(t('chat.loadFailed'));
      });

    const connection = createChatConnection(side);
    connectionRef.current = connection;

    connection.on('ReceiveMessage', (message: ChatMessage) => {
      // Dedupe by id: the REST history fetch above and this live subscription start
      // concurrently, so a message sent in that narrow window can otherwise be present
      // in both the fetched history and this live event.
      setMessages((prev) => (prev.some((m) => m.id === message.id) ? prev : [...prev, message]));
    });
    connection.onreconnected(() => {
      // History is not re-fetched on reconnect (per design) - only new live messages
      // are appended, avoiding duplication.
      connection.invoke('JoinTicket', ticketId).catch(() => {});
      setConnected(true);
    });
    connection.onreconnecting(() => setConnected(false));
    connection.onclose(() => setConnected(false));

    connection
      .start()
      .then(() => connection.invoke('JoinTicket', ticketId))
      .then(() => {
        if (!cancelled) setConnected(true);
      })
      .catch(() => {
        if (!cancelled) setError(t('chat.connectFailed'));
      });

    return () => {
      cancelled = true;
      connection.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ticketId, side]);

  useEffect(() => {
    listEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async () => {
    const body = input.trim();
    if (!body || !connectionRef.current) return;
    try {
      await connectionRef.current.invoke('SendMessage', ticketId, body);
      setInput('');
    } catch {
      setError(t('chat.sendFailed'));
    }
  };

  const ownSenderType = side === 'staff' ? 'Staff' : 'Customer';

  return (
    <section className="rounded border border-slate-200 bg-white p-4">
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">{t('chat.heading')}</h2>
        <span className={`text-xs ${connected ? 'text-green-600' : 'text-slate-400'}`}>
          {connected ? t('chat.connected') : t('chat.connecting')}
        </span>
      </div>

      {error && <p className="mb-2 text-sm text-red-600">{error}</p>}

      <div className="mb-3 flex max-h-72 flex-col gap-2 overflow-y-auto rounded bg-slate-50 p-3">
        {messages.length === 0 ? (
          <p className="text-sm text-slate-500">{t('chat.empty')}</p>
        ) : (
          messages.map((m) => (
            <div
              key={m.id}
              className={`max-w-[80%] rounded px-3 py-2 text-sm ${
                m.senderType === ownSenderType
                  ? 'self-end bg-slate-800 text-white'
                  : 'self-start bg-white text-slate-800 border border-slate-200'
              }`}
            >
              <p className="whitespace-pre-wrap">{m.body}</p>
              <span className={`mt-1 block text-[10px] ${m.senderType === ownSenderType ? 'text-slate-300' : 'text-slate-400'}`}>
                {new Date(m.sentAtUtc).toLocaleTimeString()}
              </span>
            </div>
          ))
        )}
        <div ref={listEndRef} />
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          handleSend();
        }}
        className="flex gap-2"
      >
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder={t('chat.placeholder') ?? ''}
          disabled={!connected}
          className="flex-1 rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 disabled:opacity-60"
        />
        <button
          type="submit"
          disabled={!connected || !input.trim()}
          className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
        >
          {t('chat.send')}
        </button>
      </form>
    </section>
  );
}
