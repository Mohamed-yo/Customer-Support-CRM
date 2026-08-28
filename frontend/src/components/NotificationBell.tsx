import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  getUnreadNotificationCount,
  listNotifications,
  markNotificationRead,
  type NotificationItem,
} from '../api/notifications';

// Decision 5: polling, not push/real-time — no SignalR or similar in this app.
const POLL_INTERVAL_MS = 45_000;

export default function NotificationBell() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [unreadCount, setUnreadCount] = useState(0);
  const [items, setItems] = useState<NotificationItem[]>([]);
  const [open, setOpen] = useState(false);
  const [loadFailed, setLoadFailed] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let cancelled = false;

    const pollUnreadCount = async () => {
      try {
        const count = await getUnreadNotificationCount();
        if (!cancelled) setUnreadCount(count);
      } catch {
        // Silent: a poll failure shouldn't interrupt whatever else the user is doing.
      }
    };

    pollUnreadCount();
    const intervalId = window.setInterval(pollUnreadCount, POLL_INTERVAL_MS);
    return () => {
      cancelled = true;
      window.clearInterval(intervalId);
    };
  }, []);

  useEffect(() => {
    if (!open) return;
    const onClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, [open]);

  const handleToggle = async () => {
    const next = !open;
    setOpen(next);
    if (next) {
      setLoadFailed(false);
      try {
        const data = await listNotifications();
        setItems(data);
      } catch {
        setLoadFailed(true);
      }
    }
  };

  const handleItemClick = async (item: NotificationItem) => {
    if (!item.isRead) {
      try {
        await markNotificationRead(item.id);
        setItems((prev) => prev.map((n) => (n.id === item.id ? { ...n, isRead: true } : n)));
        setUnreadCount((prev) => Math.max(0, prev - 1));
      } catch {
        // Ignore: the item still navigates even if marking-as-read failed.
      }
    }
    setOpen(false);
    if (item.ticketId) {
      navigate(`/tickets/${item.ticketId}`);
    }
  };

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={handleToggle}
        aria-label={t('notifications.title')}
        title={t('notifications.title')}
        aria-expanded={open}
        className="relative rounded p-2 text-slate-700 transition-colors hover:bg-slate-100 active:bg-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
      >
        <BellIcon />
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 end-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-red-600 px-1 text-[10px] font-semibold leading-none text-white">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute end-0 z-50 mt-2 w-80 max-w-[90vw] rounded-md border border-slate-200 bg-white shadow-lg">
          <div className="border-b border-slate-200 px-3 py-2 text-sm font-semibold text-slate-800">
            {t('notifications.title')}
          </div>
          <div className="max-h-96 overflow-y-auto">
            {loadFailed && <div className="px-3 py-4 text-sm text-red-600">{t('notifications.loadFailed')}</div>}
            {!loadFailed && items.length === 0 && (
              <div className="px-3 py-4 text-sm text-slate-500">{t('notifications.empty')}</div>
            )}
            {!loadFailed &&
              items.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => handleItemClick(item)}
                  className={`block w-full border-b border-slate-100 px-3 py-2 text-start text-sm last:border-b-0 hover:bg-slate-50 ${
                    item.isRead ? 'text-slate-600' : 'bg-slate-50 font-medium text-slate-900'
                  }`}
                >
                  <div>{t(`notifications.type.${item.type}`)}</div>
                  <div className="mt-0.5 text-xs text-slate-400">
                    {new Date(item.createdAtUtc).toLocaleString()}
                  </div>
                </button>
              ))}
          </div>
        </div>
      )}
    </div>
  );
}

function BellIcon() {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M18 8a6 6 0 1 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M13.73 21a2 2 0 0 1-3.46 0" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
