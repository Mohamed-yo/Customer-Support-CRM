import { useEffect, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import {
  type AdminUserListItem,
  type RoleListItem,
  assignRole,
  createUser,
  deactivateUser,
  listRoles,
  listUsers,
  patchUser,
  reactivateUser,
  removeRole,
} from '../../api/admin';
import { type Department, listDepartments } from '../../api/departments';
import { type Branch, listBranches } from '../../api/branches';
import { useAuthStore } from '../../store/useAuthStore';

export default function UsersPage() {
  const { t } = useTranslation();
  const currentUserId = useAuthStore((s) => s.user?.id);

  const [users, setUsers] = useState<AdminUserListItem[]>([]);
  const [roles, setRoles] = useState<RoleListItem[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [branches, setBranches] = useState<Branch[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [branchId, setBranchId] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const loadUsers = (term?: string) => {
    setLoading(true);
    setError(null);
    listUsers(term)
      .then(setUsers)
      .catch(() => setError(t('admin.users.loadFailed')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadUsers();
    listRoles().then(setRoles).catch(() => setRoles([]));
    listDepartments().then(setDepartments).catch(() => setDepartments([]));
    listBranches().then(setBranches).catch(() => setBranches([]));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSearchSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    loadUsers(search.trim() || undefined);
  };

  const openCreateForm = () => {
    setEmail('');
    setDisplayName('');
    setPassword('');
    setDepartmentId('');
    setBranchId('');
    setFormError(null);
    setFormOpen(true);
  };

  const handleCreate = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setFormError(null);

    if (!email.trim() || !displayName.trim() || password.length < 8) {
      setFormError(t('admin.users.form.invalid'));
      return;
    }

    setSubmitting(true);
    try {
      await createUser({
        email: email.trim(),
        displayName: displayName.trim(),
        password,
        departmentId: departmentId || null,
        branchId: branchId || null,
      });
      setFormOpen(false);
      loadUsers(search.trim() || undefined);
    } catch (err: any) {
      const code = err?.response?.data?.error;
      setFormError(code === 'email_in_use' ? t('admin.users.form.emailInUse') : t('admin.users.form.saveFailed'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleAssignDepartment = async (user: AdminUserListItem, newDepartmentId: string) => {
    try {
      await patchUser(user.id, { departmentId: newDepartmentId || null });
      loadUsers(search.trim() || undefined);
    } catch {
      setError(t('admin.users.actionFailed'));
    }
  };

  const handleAssignBranch = async (user: AdminUserListItem, newBranchId: string) => {
    try {
      await patchUser(user.id, { branchId: newBranchId || null });
      loadUsers(search.trim() || undefined);
    } catch {
      setError(t('admin.users.actionFailed'));
    }
  };

  const handleDeactivate = async (user: AdminUserListItem) => {
    if (!window.confirm(t('admin.users.deactivateConfirm', { name: user.displayName }))) return;
    try {
      await deactivateUser(user.id);
      loadUsers(search.trim() || undefined);
    } catch {
      setError(t('admin.users.actionFailed'));
    }
  };

  const handleReactivate = async (user: AdminUserListItem) => {
    try {
      await reactivateUser(user.id);
      loadUsers(search.trim() || undefined);
    } catch {
      setError(t('admin.users.actionFailed'));
    }
  };

  const handleToggleRole = async (user: AdminUserListItem, role: RoleListItem, hasRole: boolean) => {
    try {
      if (hasRole) {
        const roleId = role.id;
        await removeRole(user.id, roleId);
      } else {
        await assignRole(user.id, role.id);
      }
      loadUsers(search.trim() || undefined);
    } catch {
      setError(t('admin.users.actionFailed'));
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl sm:text-3xl font-semibold text-slate-800">{t('admin.users.title')}</h1>
        <button
          type="button"
          onClick={openCreateForm}
          className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-slate-800"
        >
          {t('admin.users.new')}
        </button>
      </div>

      <form onSubmit={handleSearchSubmit} className="flex gap-2">
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t('admin.users.searchPlaceholder') ?? ''}
          className="w-full max-w-sm rounded border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800"
        />
        <button type="submit" className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100">
          {t('admin.users.search')}
        </button>
      </form>

      {error && <p className="text-sm text-red-600">{error}</p>}

      {loading ? null : (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          {users.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-1 px-4 py-16 text-center">
              <p className="text-sm text-slate-500">{t('admin.users.empty')}</p>
            </div>
          ) : (
            <table className="w-full text-start text-sm">
              <thead className="bg-slate-50">
                <tr className="border-b border-slate-200">
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('admin.users.columns.name')}</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('admin.users.columns.email')}</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('admin.users.columns.roles')}</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('tickets.form.department')}</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('tickets.form.branch')}</th>
                  <th className="px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-slate-500">{t('admin.users.columns.status')}</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50 align-top">
                    <td className="px-4 py-3 text-slate-800">{u.displayName}</td>
                    <td className="px-4 py-3 text-slate-600">{u.email}</td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {roles.map((role) => {
                          const hasRole = u.roles.includes(role.name);
                          const disableSelfAdminRemoval = hasRole && role.name === 'Admin' && u.id === currentUserId;
                          return (
                            <button
                              key={role.id}
                              type="button"
                              disabled={disableSelfAdminRemoval}
                              onClick={() => handleToggleRole(u, role, hasRole)}
                              title={disableSelfAdminRemoval ? t('admin.users.cannotRemoveOwnAdmin') : undefined}
                              className={`rounded-full border px-2.5 py-0.5 text-xs font-medium transition-colors disabled:cursor-not-allowed disabled:opacity-50 ${
                                hasRole
                                  ? 'border-slate-800 bg-slate-800 text-white'
                                  : 'border-slate-300 text-slate-600 hover:bg-slate-100'
                              }`}
                            >
                              {role.name}
                            </button>
                          );
                        })}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <select
                        value={u.departmentId ?? ''}
                        onChange={(e) => handleAssignDepartment(u, e.target.value)}
                        className="rounded border border-slate-300 bg-white px-2 py-1 text-slate-800"
                      >
                        <option value="">—</option>
                        {departments.map((d) => (
                          <option key={d.id} value={d.id}>
                            {d.name}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="px-4 py-3">
                      <select
                        value={u.branchId ?? ''}
                        onChange={(e) => handleAssignBranch(u, e.target.value)}
                        className="rounded border border-slate-300 bg-white px-2 py-1 text-slate-800"
                      >
                        <option value="">—</option>
                        {branches.map((b) => (
                          <option key={b.id} value={b.id}>
                            {b.name}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="px-4 py-3 text-slate-600">
                      {u.isActive ? t('admin.users.active') : t('admin.users.inactive')}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        {u.id === currentUserId ? null : u.isActive ? (
                          <button
                            type="button"
                            onClick={() => handleDeactivate(u)}
                            className="rounded px-2.5 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50"
                          >
                            {t('admin.users.deactivate')}
                          </button>
                        ) : (
                          <button
                            type="button"
                            onClick={() => handleReactivate(u)}
                            className="rounded px-2.5 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-100"
                          >
                            {t('admin.users.reactivate')}
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
          className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-slate-900/50 p-4"
          onClick={() => setFormOpen(false)}
        >
          <form
            onSubmit={handleCreate}
            onClick={(e) => e.stopPropagation()}
            noValidate
            className="flex max-h-full w-full max-w-lg flex-col gap-4 overflow-y-auto rounded bg-white p-6 shadow-sm"
          >
            <h2 className="text-lg font-semibold text-slate-800">{t('admin.users.new')}</h2>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('admin.users.form.email')}</span>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
            </label>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('admin.users.form.displayName')}</span>
              <input
                type="text"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
            </label>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('admin.users.form.password')}</span>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              />
              <span className="text-xs text-slate-500">{t('admin.users.form.passwordHint')}</span>
            </label>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('tickets.form.department')}</span>
              <select
                value={departmentId}
                onChange={(e) => setDepartmentId(e.target.value)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              >
                <option value="">—</option>
                {departments.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name}
                  </option>
                ))}
              </select>
            </label>

            <label className="flex flex-col gap-1 text-sm text-slate-700">
              <span>{t('tickets.form.branch')}</span>
              <select
                value={branchId}
                onChange={(e) => setBranchId(e.target.value)}
                className="rounded border border-slate-300 bg-white px-3 py-2 text-slate-800"
              >
                <option value="">—</option>
                {branches.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.name}
                  </option>
                ))}
              </select>
            </label>

            {formError && <p className="text-sm text-red-600">{formError}</p>}

            <div className="flex items-center justify-end gap-2">
              <button
                type="button"
                onClick={() => setFormOpen(false)}
                className="rounded border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100"
              >
                {t('admin.users.form.cancel')}
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="rounded bg-slate-800 px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
              >
                {t('admin.users.form.save')}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
