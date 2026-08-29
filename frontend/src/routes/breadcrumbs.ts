export type BreadcrumbHandle = {
  /** i18n key for the static label of this route (e.g. 'shell.breadcrumb.tickets'). */
  labelKey: string;
  /** Absolute path of the parent crumb, or null when this route is a top-level page. */
  parentPath: string | null;
  /**
   * When true, the label for this crumb is expected to be supplied dynamically by the
   * rendered page (via useBreadcrumbLabel). If the page has not yet published a label,
   * the parent's labelKey is shown instead - the raw :id must never be rendered.
   */
  dynamic?: boolean;
};

// Single source of truth for Breadcrumbs.tsx - both the current route's handle and every
// ancestor's label/path are resolved from this one map via matchBreadcrumbRoute() below.
// (The app uses <BrowserRouter><Routes> in declarative mode, not a data router, so this is
// looked up from the pathname via useLocation() - not via React Router's useMatches(),
// which throws outside a data router.)
//
// To add a new authenticated page: add one entry here, matching its <Route path="..."> in
// AppRouter.tsx exactly.
export const BREADCRUMB_ROUTES: Record<string, BreadcrumbHandle> = {
  '/': { labelKey: 'shell.breadcrumb.home', parentPath: null },
  '/customers': { labelKey: 'shell.breadcrumb.customers', parentPath: '/' },
  '/customers/:id': { labelKey: 'shell.breadcrumb.customerDetail', parentPath: '/customers', dynamic: true },
  '/tickets': { labelKey: 'shell.breadcrumb.tickets', parentPath: '/' },
  '/tickets/:id': { labelKey: 'shell.breadcrumb.ticketDetail', parentPath: '/tickets', dynamic: true },
  '/quick-replies': { labelKey: 'shell.breadcrumb.quickReplies', parentPath: '/' },
  '/knowledge-base': { labelKey: 'shell.breadcrumb.knowledgeBase', parentPath: '/' },
  '/webhooks': { labelKey: 'shell.breadcrumb.webhooks', parentPath: '/' },
  '/api-keys': { labelKey: 'shell.breadcrumb.apiKeys', parentPath: '/' },
  '/reports': { labelKey: 'shell.breadcrumb.reports', parentPath: '/' },
  '/reports/tickets': { labelKey: 'shell.breadcrumb.reportsTickets', parentPath: '/reports' },
  '/reports/sla': { labelKey: 'shell.breadcrumb.reportsSla', parentPath: '/reports' },
  '/reports/agents': { labelKey: 'shell.breadcrumb.reportsAgents', parentPath: '/reports' },
  '/reports/satisfaction': { labelKey: 'shell.breadcrumb.reportsSatisfaction', parentPath: '/reports' },
  '/admin/users': { labelKey: 'shell.breadcrumb.adminUsers', parentPath: '/' },
  '/admin/audit-log': { labelKey: 'shell.breadcrumb.adminAuditLog', parentPath: '/' },
  '/admin/departments': { labelKey: 'shell.breadcrumb.adminDepartments', parentPath: '/' },
  '/admin/branches': { labelKey: 'shell.breadcrumb.adminBranches', parentPath: '/' },
  '/admin/branding': { labelKey: 'shell.breadcrumb.adminBranding', parentPath: '/' },
  '/admin/sla-targets': { labelKey: 'shell.breadcrumb.adminSlaTargets', parentPath: '/' },
  '/admin/reminder-lead-time': { labelKey: 'shell.breadcrumb.adminReminderLeadTime', parentPath: '/' },
};

export type BreadcrumbRouteMatch = {
  /** The route pattern that matched (e.g. '/tickets/:id') - used for handle lookups only. */
  routeKey: string;
  handle: BreadcrumbHandle;
};

// Converts a route pattern like '/tickets/:id' into a RegExp that matches only that exact
// shape (segment-by-segment, anchored) - '/tickets/:id' must match '/tickets/abc-123' but
// never '/tickets' or '/tickets/abc-123/extra' or an unrelated '/ticketsish/abc-123'.
function routePatternToRegex(pattern: string): RegExp {
  const segments = pattern
    .split('/')
    .map((segment) => (segment.startsWith(':') ? '[^/]+' : segment.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')));
  return new RegExp(`^${segments.join('/')}$`);
}

/**
 * Resolves the current pathname against BREADCRUMB_ROUTES without React Router's
 * useMatches() (which requires a data router this app does not use). An exact static
 * match (e.g. '/tickets') is always preferred; parameterized routes (e.g. '/tickets/:id')
 * are only matched when no static route matches directly.
 */
export function matchBreadcrumbRoute(pathname: string): BreadcrumbRouteMatch | null {
  // Normalize a trailing slash (except the root '/' itself) so '/tickets/' and '/tickets'
  // resolve identically.
  const normalized = pathname.length > 1 && pathname.endsWith('/') ? pathname.slice(0, -1) : pathname;

  const exactHandle = BREADCRUMB_ROUTES[normalized];
  if (exactHandle) {
    return { routeKey: normalized, handle: exactHandle };
  }

  for (const [routeKey, handle] of Object.entries(BREADCRUMB_ROUTES)) {
    if (!routeKey.includes(':')) continue;
    if (routePatternToRegex(routeKey).test(normalized)) {
      return { routeKey, handle };
    }
  }

  return null;
}
