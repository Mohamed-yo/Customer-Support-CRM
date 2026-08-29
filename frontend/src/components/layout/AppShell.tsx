import { useEffect, useRef, useState, type CSSProperties } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '../../store/useAuthStore';
import { useBranding } from '../../hooks/useBranding';
import LanguageToggle from '../LanguageToggle';
import NotificationBell from '../NotificationBell';
import Breadcrumbs from './Breadcrumbs';
import { BreadcrumbLabelContext } from './useBreadcrumbLabel';

// Extensibility point: future stories add sections here (Tickets, Customers, Agents,
// Reports, Settings, ...) without touching the shell's structure.
// `adminOnly` (Story 12): the first role-conditional nav item - filtered below against
// hasRole('Admin'), not a new permissions abstraction.
const NAV_ITEMS: Array<{ to: string; labelKey: string; adminOnly?: boolean }> = [
  { to: '/', labelKey: 'shell.nav.home' },
  { to: '/customers', labelKey: 'shell.nav.customers' },
  { to: '/tickets', labelKey: 'shell.nav.tickets' },
  { to: '/quick-replies', labelKey: 'shell.nav.quickReplies' },
  { to: '/knowledge-base', labelKey: 'shell.nav.knowledgeBase' },
  { to: '/webhooks', labelKey: 'shell.nav.webhooks', adminOnly: true },
  { to: '/reports', labelKey: 'shell.nav.reports', adminOnly: true },
  { to: '/api-keys', labelKey: 'shell.nav.apiKeys', adminOnly: true },
  { to: '/admin/users', labelKey: 'shell.nav.adminUsers', adminOnly: true },
  { to: '/admin/audit-log', labelKey: 'shell.nav.adminAuditLog', adminOnly: true },
  { to: '/admin/departments', labelKey: 'shell.nav.departments', adminOnly: true },
  { to: '/admin/branches', labelKey: 'shell.nav.branches', adminOnly: true },
  { to: '/admin/branding', labelKey: 'shell.nav.branding', adminOnly: true },
  { to: '/admin/sla-targets', labelKey: 'shell.nav.slaTargets', adminOnly: true },
  { to: '/admin/reminder-lead-time', labelKey: 'shell.nav.reminderLeadTime', adminOnly: true },
];

const navLinkClassName = ({ isActive }: { isActive: boolean }) =>
  `rounded px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800 ${
    isActive ? 'bg-[var(--brand-primary,#1e293b)] text-white' : 'text-slate-700 hover:bg-slate-100 active:bg-slate-200'
  }`;

export default function AppShell() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { appName, logoDataUrl, primaryColorHex } = useBranding();
  const user = useAuthStore((s) => s.user);
  const clearSession = useAuthStore((s) => s.clearSession);
  const isAdmin = useAuthStore((s) => s.hasRole('Admin'));
  const visibleNavItems = NAV_ITEMS.filter((item) => !item.adminOnly || isAdmin);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const menuButtonRef = useRef<HTMLButtonElement>(null);
  const [dynamicBreadcrumbLabel, setDynamicBreadcrumbLabel] = useState<string | null>(null);

  // Close the mobile drawer and clear any published dynamic breadcrumb label whenever the
  // route changes. Reset during render (React's documented pattern for "adjusting state
  // when a prop changes") instead of in an effect, to avoid an extra post-commit render
  // pass. Without the label reset, navigating from e.g. /tickets/1 to /customers could
  // briefly show the previous page's stale dynamic label.
  const [lastPathname, setLastPathname] = useState(location.pathname);
  if (location.pathname !== lastPathname) {
    setLastPathname(location.pathname);
    setDrawerOpen(false);
    setDynamicBreadcrumbLabel(null);
  }

  // Close on Escape and return focus to the toggle button.
  useEffect(() => {
    if (!drawerOpen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        setDrawerOpen(false);
        menuButtonRef.current?.focus();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [drawerOpen]);

  const handleLogout = () => {
    clearSession();
    navigate('/login', { replace: true });
  };

  return (
    <div
      className="flex min-h-screen bg-slate-50"
      style={primaryColorHex ? ({ '--brand-primary': primaryColorHex } as CSSProperties) : undefined}
    >
      {drawerOpen && (
        <div
          className="fixed inset-0 z-30 bg-slate-900/50 lg:hidden"
          onClick={() => setDrawerOpen(false)}
          aria-hidden="true"
        />
      )}

      <aside
        className={`${drawerOpen ? 'flex' : 'hidden'} lg:flex fixed inset-y-0 start-0 z-40 w-64 flex-col gap-6 bg-white px-4 py-6 shadow-sm lg:static`}
        aria-label={t('shell.sidebar')}
      >
        <div className="flex items-center gap-2">
          {logoDataUrl && <img src={logoDataUrl} alt="" className="h-7 w-7 rounded object-contain" />}
          <span className="text-lg font-semibold text-slate-800">{appName}</span>
        </div>

        <div className="flex flex-col gap-4 border-b border-slate-200 pb-4">
          <div className="flex flex-col gap-0.5 text-sm">
            <span className="font-medium text-slate-800">{user?.displayName}</span>
            <span className="text-slate-500">{user?.email}</span>
          </div>
        </div>

        <nav aria-label="Primary" className="flex flex-col gap-1">
          {visibleNavItems.map((item) => (
            <NavLink key={item.to} to={item.to} end className={navLinkClassName}>
              {t(item.labelKey)}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-between gap-3 border-b border-slate-200 bg-white px-4 py-3">
          <div className="flex min-w-0 items-center gap-3">
            <button
              ref={menuButtonRef}
              type="button"
              onClick={() => setDrawerOpen((open) => !open)}
              aria-label={drawerOpen ? t('shell.closeMenu') : t('shell.openMenu')}
              aria-expanded={drawerOpen}
              className="rounded p-2 text-slate-700 hover:bg-slate-100 active:bg-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800 lg:hidden"
            >
              <MenuIcon open={drawerOpen} />
            </button>
            <Breadcrumbs currentDynamicLabel={dynamicBreadcrumbLabel} />
          </div>

          <div className="flex items-center gap-2">
            <NotificationBell />
            <LanguageToggle />
            <button
              type="button"
              onClick={handleLogout}
              aria-label={t('auth.logout')}
              title={t('auth.logout')}
              className="rounded p-2 text-slate-700 transition-colors hover:bg-slate-100 active:bg-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
            >
              <LogoutIcon />
            </button>
          </div>
        </header>

        <main className="flex-1 px-4 py-6 sm:px-6 sm:py-8 lg:px-8">
          <div className="mx-auto w-full max-w-5xl">
            <BreadcrumbLabelContext.Provider value={setDynamicBreadcrumbLabel}>
              <Outlet />
            </BreadcrumbLabelContext.Provider>
          </div>
        </main>
      </div>
    </div>
  );
}

function MenuIcon({ open }: { open: boolean }) {
  if (open) {
    return (
      <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
        <path d="M6 6l12 12M18 6L6 18" strokeLinecap="round" />
      </svg>
    );
  }
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M4 7h16M4 12h16M4 17h16" strokeLinecap="round" />
    </svg>
  );
}

function LogoutIcon() {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M16 17l5-5-5-5M21 12H9" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
