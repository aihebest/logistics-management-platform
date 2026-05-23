import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { maintenanceApi, vehiclesApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'

const STATUS_FILTER = ['', 'Scheduled', 'InProgress', 'Completed', 'Overdue']

export default function MaintenancePage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)

  const { data: records = [], isLoading } = useQuery({
    queryKey: ['maintenance', statusFilter],
    queryFn: () => maintenanceApi.getAll({ status: statusFilter || undefined }),
  })

  const { data: vehicles = [] } = useQuery({
    queryKey: ['vehicles'],
    queryFn: () => vehiclesApi.getAll(),
    enabled: showForm,
  })

  const createRecord = useMutation({
    mutationFn: maintenanceApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['maintenance'] }); setShowForm(false); toast.success('Maintenance record created') },
    onError: () => toast.error('Failed to create record'),
  })

  const updateRecord = useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => maintenanceApi.update(id, data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['maintenance'] }); toast.success('Record updated') },
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    createRecord.mutate({
      vehicleId: fd.get('vehicleId'),
      type: fd.get('type'),
      scheduledDate: fd.get('scheduledDate'),
      vendorName: fd.get('vendorName') || undefined,
      vendorContact: fd.get('vendorContact') || undefined,
      notes: fd.get('notes') || undefined,
    })
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Maintenance</h1>
        <div className="flex items-center gap-3">
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
            {STATUS_FILTER.map(s => <option key={s} value={s}>{s || 'All Status'}</option>)}
          </select>
          {hasRole('Manager', 'Mechanic', 'Admin') && (
            <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ Add Record</button>
          )}
        </div>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">New Maintenance Record</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="label">Vehicle</label>
              <select name="vehicleId" className="input" required>
                <option value="">Select vehicle…</option>
                {vehicles.map(v => <option key={v.id} value={v.id}>{v.registrationNo} — {v.make} {v.model}</option>)}
              </select>
            </div>
            <div><label className="label">Service Type</label><input name="type" className="input" placeholder="e.g. Oil Change, Routine Service" required /></div>
            <div><label className="label">Scheduled Date</label><input name="scheduledDate" type="date" className="input" required /></div>
            <div><label className="label">Vendor Name</label><input name="vendorName" className="input" /></div>
            <div><label className="label">Vendor Contact</label><input name="vendorContact" className="input" /></div>
            <div><label className="label">Notes</label><textarea name="notes" className="input" rows={2} /></div>
            <div className="md:col-span-2 flex gap-3">
              <button type="submit" className="btn-primary" disabled={createRecord.isPending}>Save</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Vehicle', 'Type', 'Scheduled', 'Completed', 'Status', 'Cost', 'Vendor', ''].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {records.map(m => (
                <tr key={m.id} className={`hover:bg-gray-50 ${m.status === 'Overdue' ? 'bg-red-50' : ''}`}>
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-semibold text-gray-900">{m.vehicleReg}</td>
                  <td className="px-4 py-3 text-sm text-gray-700 max-w-xs truncate">{m.type}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{m.scheduledDate}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{m.completedDate ?? '—'}</td>
                  <td className="px-4 py-3 whitespace-nowrap"><StatusBadge status={m.status} /></td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700">
                    {m.cost != null ? `R ${m.cost.toLocaleString()}` : '—'}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{m.vendorName ?? '—'}</td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    {(m.status === 'Scheduled' || m.status === 'InProgress' || m.status === 'Overdue') &&
                     hasRole('Manager', 'Mechanic', 'Admin') && (
                      <button className="btn-secondary text-xs"
                        onClick={() => updateRecord.mutate({ id: m.id, data: { status: 'Completed', completedDate: new Date().toISOString().split('T')[0] } })}>
                        Mark Complete
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {records.length === 0 && <p className="text-center text-gray-400 py-12">No maintenance records</p>}
        </div>
      </div>
    </div>
  )
}
