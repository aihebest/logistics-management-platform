import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { driversApi, apiErrorMessage, type User } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'

const STATUS_OPTIONS = ['Available', 'OnAssignment', 'OffDuty', 'OnBreak']

export default function DriversPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  // Driver currently open for correction. Coordinators register drivers, so
  // they are also the ones who fix a mistyped name or licence number.
  const [editing, setEditing] = useState<User | null>(null)
  const canEdit = hasRole('Coordinator', 'Manager', 'Admin')

  const { data: drivers = [], isLoading } = useQuery({
    queryKey: ['drivers'],
    queryFn: driversApi.getAll,
    refetchInterval: 30_000,
  })

  const updateStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      driversApi.updateStatus(id, status),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['drivers'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      toast.success('Status updated')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to update status'), { duration: 6000 }),
  })

  const registerDriver = useMutation({
    mutationFn: driversApi.register,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['drivers'] })
      setShowForm(false)
      toast.success('Driver registered successfully')
    },
    onError: (err: any) => {
      const msg = err?.response?.data?.error ?? 'Failed to register driver'
      toast.error(msg)
    },
  })

  const updateDriver = useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => driversApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['drivers'] })
      setEditing(null)
      toast.success('Driver record updated')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to update driver'), { duration: 6000 }),
  })

  const removeDriver = useMutation({
    mutationFn: driversApi.remove,
    onSuccess: res => {
      qc.invalidateQueries({ queryKey: ['drivers'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      setEditing(null)
      // The API decides between delete and deactivate, so echo what it did
      // rather than claiming the record was removed.
      toast.success(res.message, { duration: res.deactivated ? 8000 : 4000 })
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to remove driver'), { duration: 6000 }),
  })

  const handleRemove = (driver: User) => {
    const ok = window.confirm(
      `Remove ${driver.fullName}?\n\n` +
      'If they have trip, fuel or movement history the record is deactivated ' +
      'instead of deleted, so that history is not lost.',
    )
    if (ok) removeDriver.mutate(driver.id)
  }

  const handleUpdate = (e: React.FormEvent<HTMLFormElement>, id: string) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    const str = (k: string) => { const v = (fd.get(k) as string | null)?.trim(); return v ? v : undefined }
    updateDriver.mutate({
      id,
      data: {
        fullName: str('fullName'),
        phoneNumber: str('phoneNumber'),
        licenceNo: str('licenceNo'),
        licenceExpiry: str('licenceExpiry'),
      },
    })
  }

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    registerDriver.mutate({
      fullName: fd.get('fullName') as string,
      phoneNumber: fd.get('phoneNumber') as string || undefined,
      licenceNo: fd.get('licenceNo') as string || undefined,
      licenceExpiry: fd.get('licenceExpiry') as string || undefined,
    })
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Drivers</h1>
        <div className="flex items-center gap-3">
          <span className="text-sm text-gray-500">{drivers.length} driver{drivers.length !== 1 ? 's' : ''}</span>
          {canEdit && (
            <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
              + Register Driver
            </button>
          )}
        </div>
      </div>

      {/* ── Register Driver Form ──────────────────────────────────────────────── */}
      {showForm && (
        <div className="card p-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-semibold text-gray-900">Register New Driver</h2>
            <p className="text-xs text-gray-400 max-w-sm text-right">
              Registered by a coordinator or manager. The driver starts as
              <strong> Off Duty</strong> and does not need an email address or a
              platform login — coordinators assign trips and record mileage on
              their behalf.
            </p>
          </div>
          <form onSubmit={handleSubmit} className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <div>
              <label className="label">Full Name <span className="text-red-500">*</span></label>
              <input name="fullName" className="input" placeholder="e.g. Kwame Asante" required />
            </div>
            <div>
              <label className="label">Phone Number</label>
              <input name="phoneNumber" className="input" placeholder="e.g. +234 80 111 0011" />
            </div>
            <div>
              <label className="label">Licence No</label>
              <input name="licenceNo" className="input" placeholder="e.g. GP-DL-011" />
            </div>
            <div>
              <label className="label">Licence Expiry</label>
              <input name="licenceExpiry" type="date" className="input" />
            </div>
            <div className="col-span-full flex gap-3 pt-1">
              <button type="submit" className="btn-primary" disabled={registerDriver.isPending}>
                {registerDriver.isPending ? 'Registering…' : 'Register Driver'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      {/* ── Edit Driver ───────────────────────────────────────────────────────── */}
      {editing && (
        <div className="card p-5 border-l-4 border-amber-500">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-semibold text-gray-900">Edit — {editing.fullName}</h2>
            <button onClick={() => setEditing(null)} className="text-gray-400 hover:text-gray-600 text-sm">✕ Close</button>
          </div>
          <form onSubmit={e => handleUpdate(e, editing.id)} className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <label className="label">Full Name</label>
              <input name="fullName" className="input" defaultValue={editing.fullName} />
            </div>
            <div>
              <label className="label">Phone Number</label>
              <input name="phoneNumber" className="input" defaultValue={editing.phoneNumber ?? ''} />
            </div>
            <div>
              <label className="label">Licence No</label>
              <input name="licenceNo" className="input" defaultValue={editing.licenceNo ?? ''} />
            </div>
            <div>
              <label className="label">Licence Expiry</label>
              <input name="licenceExpiry" type="date" className="input" defaultValue={editing.licenceExpiry ?? ''} />
            </div>
            <div className="col-span-full flex gap-3 items-center">
              <button type="submit" className="btn-primary" disabled={updateDriver.isPending}>
                {updateDriver.isPending ? 'Saving…' : 'Save Changes'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setEditing(null)}>Cancel</button>
              <button
                type="button"
                className="text-sm text-red-600 hover:underline ml-auto"
                onClick={() => handleRemove(editing)}
                disabled={removeDriver.isPending}
              >
                {removeDriver.isPending ? 'Removing…' : 'Remove this driver'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* ── Drivers Table ─────────────────────────────────────────────────────── */}
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Name', 'Phone', 'Status', 'Licence No', 'Licence Expiry', ''].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-100">
              {drivers.map(driver => (
                <tr key={driver.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 bg-brand-100 rounded-full flex items-center justify-center text-brand-700 font-semibold text-sm flex-shrink-0">
                        {driver.fullName.charAt(0)}
                      </div>
                      <span className="text-sm font-medium text-gray-900">{driver.fullName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{driver.phoneNumber ?? '—'}</td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    <StatusBadge status={driver.driverStatus ?? 'Unknown'} />
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{driver.licenceNo ?? '—'}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm">
                    {driver.licenceExpiry ? (
                      <span className={
                        new Date(driver.licenceExpiry) < new Date()
                          ? 'text-red-600 font-medium'
                          : 'text-gray-500'
                      }>
                        {driver.licenceExpiry}
                      </span>
                    ) : '—'}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    {canEdit && (
                      <div className="flex items-center gap-3">
                        <select
                          value={driver.driverStatus ?? ''}
                          onChange={e => updateStatus.mutate({ id: driver.id, status: e.target.value })}
                          className="text-sm border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-1 focus:ring-brand-500"
                        >
                          {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s}</option>)}
                        </select>
                        <button
                          className="text-xs text-brand-600 hover:underline"
                          onClick={() => setEditing(editing?.id === driver.id ? null : driver)}
                        >
                          Edit
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
              {drivers.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-12 text-center text-gray-400">
                    No drivers registered yet.{' '}
                    {canEdit && (
                      <button className="text-brand-600 hover:underline" onClick={() => setShowForm(true)}>
                        Register the first driver →
                      </button>
                    )}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
