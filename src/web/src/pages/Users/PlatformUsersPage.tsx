import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { platformUsersApi, apiErrorMessage, type User } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import toast from 'react-hot-toast'

/**
 * Platform user administration.
 *
 * The platform can only email people it knows about, and it only learned about
 * someone when they first signed in — which meant an HOD could be given a role
 * in Entra ID and still never receive the approval request telling them to log
 * in. Pre-registering someone here creates their record immediately so
 * notifications reach them, and their account links up on first sign-in.
 */

const ROLES = ['HOD', 'Manager', 'Coordinator', 'Mechanic', 'Driver', 'Staff', 'Admin']

const ROLE_STYLES: Record<string, string> = {
  Admin:       'bg-purple-100 text-purple-800',
  Manager:     'bg-blue-100 text-blue-800',
  HOD:         'bg-amber-100 text-amber-800',
  Coordinator: 'bg-teal-100 text-teal-800',
  Mechanic:    'bg-orange-100 text-orange-800',
  Driver:      'bg-gray-100 text-gray-700',
  Staff:       'bg-slate-100 text-slate-600',
}

export default function PlatformUsersPage() {
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [roleFilter, setRoleFilter] = useState('')
  const [editing, setEditing] = useState<User | null>(null)

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['platform-users', roleFilter],
    queryFn: () => platformUsersApi.getAll({ role: roleFilter || undefined }),
  })

  const refresh = () => {
    qc.invalidateQueries({ queryKey: ['platform-users'] })
    qc.invalidateQueries({ queryKey: ['drivers'] })
  }

  const register = useMutation({
    mutationFn: platformUsersApi.register,
    onSuccess: () => { refresh(); setShowForm(false); toast.success('User added — notifications will now reach them') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to add user'), { duration: 6000 }),
  })

  const update = useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => platformUsersApi.update(id, data),
    onSuccess: () => { refresh(); setEditing(null); toast.success('User updated') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to update user'), { duration: 6000 }),
  })

  const handleRegister = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    register.mutate({
      fullName: (fd.get('fullName') as string).trim(),
      email: (fd.get('email') as string).trim(),
      role: fd.get('role') as string,
      phoneNumber: (fd.get('phoneNumber') as string)?.trim() || undefined,
    })
  }

  const handleUpdate = (e: React.FormEvent<HTMLFormElement>, id: string) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    update.mutate({
      id,
      data: {
        fullName: (fd.get('fullName') as string)?.trim() || undefined,
        email: (fd.get('email') as string)?.trim() || undefined,
        role: fd.get('role') as string || undefined,
        phoneNumber: (fd.get('phoneNumber') as string)?.trim() || undefined,
        isActive: fd.get('isActive') === 'Active',
      },
    })
  }

  // Anyone in an approval role without an email can never be notified.
  const noEmailCount = users.filter(u => !u.email).length

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Platform Users</h1>
          <p className="text-xs text-gray-500 mt-0.5">
            Add colleagues so approval emails reach them — they don't need to have logged in yet
          </p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <select value={roleFilter} onChange={e => setRoleFilter(e.target.value)} className="input w-auto">
            <option value="">All Roles</option>
            {ROLES.map(r => <option key={r}>{r}</option>)}
          </select>
          <button className="btn-primary" onClick={() => { setShowForm(!showForm); setEditing(null) }}>
            + Add User
          </button>
        </div>
      </div>

      {/* ── Add user ──────────────────────────────────────────────────────── */}
      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-1">Add a Platform User</h2>
          <p className="text-xs text-gray-500 mb-4">
            The email address matters — it's how notifications reach them, and how their
            account links automatically when they first sign in. Assign the matching app
            role in Entra ID too, so their permissions stay correct long-term.
          </p>
          <form onSubmit={handleRegister} className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <label className="label">Full Name <span className="text-red-500">*</span></label>
              <input name="fullName" className="input" required placeholder="e.g. Etimbuk Umoh" />
            </div>
            <div className="md:col-span-2">
              <label className="label">Email <span className="text-red-500">*</span></label>
              <input name="email" type="email" className="input" required placeholder="name@desicongroup.com" />
            </div>
            <div>
              <label className="label">Role <span className="text-red-500">*</span></label>
              <select name="role" className="input" defaultValue="HOD">
                {ROLES.map(r => <option key={r}>{r}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Phone</label>
              <input name="phoneNumber" className="input" placeholder="Optional" />
            </div>
            <div className="col-span-full flex gap-3">
              <button type="submit" className="btn-primary" disabled={register.isPending}>
                {register.isPending ? 'Adding…' : 'Add User'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {/* ── Edit user ─────────────────────────────────────────────────────── */}
      {editing && (
        <div className="card p-5 border-l-4 border-amber-500">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-semibold text-gray-900">Edit — {editing.fullName}</h2>
            <button onClick={() => setEditing(null)} className="text-gray-400 hover:text-gray-600 text-sm">✕ Close</button>
          </div>
          <form onSubmit={e => handleUpdate(e, editing.id)} className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div>
              <label className="label">Full Name</label>
              <input name="fullName" className="input" defaultValue={editing.fullName} />
            </div>
            <div className="md:col-span-2">
              <label className="label">Email</label>
              <input name="email" type="email" className="input" defaultValue={editing.email ?? ''} />
            </div>
            <div>
              <label className="label">Role</label>
              <select name="role" className="input" defaultValue={editing.role}>
                {ROLES.map(r => <option key={r}>{r}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Phone</label>
              <input name="phoneNumber" className="input" defaultValue={editing.phoneNumber ?? ''} />
            </div>
            <div>
              <label className="label">Status</label>
              <select name="isActive" className="input" defaultValue={editing.isActive ? 'Active' : 'Inactive'}>
                <option>Active</option><option>Inactive</option>
              </select>
            </div>
            <div className="col-span-full flex gap-3">
              <button type="submit" className="btn-primary" disabled={update.isPending}>
                {update.isPending ? 'Saving…' : 'Save Changes'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setEditing(null)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {noEmailCount > 0 && (
        <div className="card p-3 text-xs text-amber-700 bg-amber-50 border border-amber-200">
          <strong>{noEmailCount}</strong> user{noEmailCount === 1 ? '' : 's'} have no email address and
          cannot receive notifications. That's expected for drivers, but anyone in an approval
          role needs one.
        </div>
      )}

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['Name', 'Email', 'Role', 'Phone', 'Status', ''].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {users.map(u => (
                <tr key={u.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-900 whitespace-nowrap">{u.fullName}</td>
                  <td className="px-4 py-3 text-gray-600">
                    {u.email || <span className="text-amber-600 italic">no email</span>}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${ROLE_STYLES[u.role] ?? 'bg-gray-100 text-gray-700'}`}>
                      {u.role}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-500 whitespace-nowrap">{u.phoneNumber || '—'}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                      u.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-500'
                    }`}>
                      {u.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => { setEditing(editing?.id === u.id ? null : u); setShowForm(false) }}
                      className="text-xs text-brand-600 hover:underline"
                    >
                      Edit
                    </button>
                  </td>
                </tr>
              ))}
              {users.length === 0 && (
                <tr><td colSpan={6} className="px-4 py-12 text-center text-gray-400">No users found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
