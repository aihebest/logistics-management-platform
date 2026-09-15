import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { departmentsApi, platformUsersApi, apiErrorMessage, type Department } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import toast from 'react-hot-toast'

/**
 * Departments and their heads.
 *
 * The head is held against the department rather than the person, because one
 * person can head more than one — Maurizio Benassi heads both Business
 * Development and Contracts.
 *
 * This is what routes a travel request to the right verifier, so a department
 * without a head is a real gap: its requests fall back to notifying every HOD.
 */
export default function DepartmentsPage() {
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<Department | null>(null)

  const { data: departments = [], isLoading } = useQuery({
    queryKey: ['departments', 'all'],
    queryFn: () => departmentsApi.getAll(true),
  })

  // Only people who could actually act as a head are offered.
  const { data: hods = [] } = useQuery({
    queryKey: ['platform-users', 'HOD'],
    queryFn: () => platformUsersApi.getAll({ role: 'HOD' }),
  })

  const refresh = () => {
    qc.invalidateQueries({ queryKey: ['departments'] })
    qc.invalidateQueries({ queryKey: ['platform-users'] })
  }

  const create = useMutation({
    mutationFn: departmentsApi.create,
    onSuccess: () => { refresh(); setShowForm(false); toast.success('Department added') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to add department'), { duration: 6000 }),
  })

  const update = useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => departmentsApi.update(id, data),
    onSuccess: () => { refresh(); setEditing(null); toast.success('Department updated') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to update department'), { duration: 7000 }),
  })

  const handleCreate = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    create.mutate({
      name: (fd.get('name') as string).trim(),
      hodUserId: (fd.get('hodUserId') as string) || undefined,
    })
  }

  const handleUpdate = (e: React.FormEvent<HTMLFormElement>, id: string) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    const hod = fd.get('hodUserId') as string
    update.mutate({
      id,
      data: {
        name: (fd.get('name') as string)?.trim() || undefined,
        hodUserId: hod || undefined,
        clearHod: hod === '' ? true : undefined,
        isActive: fd.get('isActive') === 'Active',
      },
    })
  }

  const unassigned = departments.filter(d => d.isActive && !d.hodUserId).length

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Departments</h1>
          <p className="text-xs text-gray-500 mt-0.5">
            The head of each department verifies its travel requests before management approval
          </p>
        </div>
        <button className="btn-primary" onClick={() => { setShowForm(!showForm); setEditing(null) }}>
          + Add Department
        </button>
      </div>

      {unassigned > 0 && (
        <div className="card p-3 text-xs text-amber-700 bg-amber-50 border border-amber-200">
          <strong>{unassigned}</strong> department{unassigned === 1 ? ' has' : 's have'} no head assigned.
          Travel requests from {unassigned === 1 ? 'it' : 'them'} are sent to every HOD until a head is set,
          and any HOD can verify. Assign a head below to route them properly.
          {hods.length === 0 && (
            <span className="block mt-1">
              No users hold the HOD role yet — add them under <strong>Platform Users</strong> first,
              with their real email address.
            </span>
          )}
        </div>
      )}

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">Add a Department</h2>
          <form onSubmit={handleCreate} className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="md:col-span-2">
              <label className="label">Name <span className="text-red-500">*</span></label>
              <input name="name" className="input" required placeholder="e.g. Technical Services" />
            </div>
            <div>
              <label className="label">Head of Department</label>
              <select name="hodUserId" className="input" defaultValue="">
                <option value="">Assign later</option>
                {hods.map(h => <option key={h.id} value={h.id}>{h.fullName}</option>)}
              </select>
            </div>
            <div className="col-span-full flex gap-3">
              <button type="submit" className="btn-primary" disabled={create.isPending}>
                {create.isPending ? 'Adding…' : 'Add Department'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {editing && (
        <div className="card p-5 border-l-4 border-amber-500">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-semibold text-gray-900">Edit — {editing.name}</h2>
            <button onClick={() => setEditing(null)} className="text-gray-400 hover:text-gray-600 text-sm">✕ Close</button>
          </div>
          <form onSubmit={e => handleUpdate(e, editing.id)} className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="md:col-span-2">
              <label className="label">Name</label>
              <input name="name" className="input" defaultValue={editing.name} />
            </div>
            <div>
              <label className="label">Head of Department</label>
              <select name="hodUserId" className="input" defaultValue={editing.hodUserId ?? ''}>
                <option value="">No head assigned</option>
                {hods.map(h => <option key={h.id} value={h.id}>{h.fullName}</option>)}
              </select>
              <p className="text-xs text-gray-500 mt-1">
                One person can head more than one department.
              </p>
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

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['Department', 'Head of Department', 'Email', 'Members', 'Status', ''].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {departments.map(d => (
                <tr key={d.id} className={`hover:bg-gray-50 ${d.isActive && !d.hodUserId ? 'bg-amber-50' : ''}`}>
                  <td className="px-4 py-3 font-medium text-gray-900">{d.name}</td>
                  <td className="px-4 py-3 text-gray-700">
                    {d.hodName ?? <span className="text-amber-600 italic">not assigned</span>}
                  </td>
                  <td className="px-4 py-3 text-gray-500">{d.hodEmail ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-500 tabular-nums">{d.memberCount}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                      d.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-500'
                    }`}>
                      {d.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => { setEditing(editing?.id === d.id ? null : d); setShowForm(false) }}
                      className="text-xs text-brand-600 hover:underline"
                    >
                      Edit
                    </button>
                  </td>
                </tr>
              ))}
              {departments.length === 0 && (
                <tr><td colSpan={6} className="px-4 py-12 text-center text-gray-400">No departments yet</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
