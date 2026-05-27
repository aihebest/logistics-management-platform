import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { maintenanceApi, vehiclesApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import toast from 'react-hot-toast'

const STATUS_FILTER = ['', 'Scheduled', 'InProgress', 'Completed', 'Overdue']
const CATEGORY_FILTER = ['', 'Routine', 'FaultRepair']

export default function MaintenancePage() {
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [category, setCategory] = useState('Routine')

  const { data: records = [], isLoading } = useQuery({
    queryKey: ['maintenance', statusFilter, categoryFilter],
    queryFn: () => maintenanceApi.getAll({
      status: statusFilter || undefined,
      category: categoryFilter || undefined,
    }),
  })

  const { data: vehicles = [] } = useQuery({
    queryKey: ['vehicles'],
    queryFn: () => vehiclesApi.getAll(),
  })

  const createRecord = useMutation({
    mutationFn: maintenanceApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['maintenance'] })
      setShowForm(false)
      toast.success('Maintenance record created')
    },
    onError: () => toast.error('Failed to create record'),
  })

  const completeRecord = useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => maintenanceApi.update(id, data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['maintenance'] }); toast.success('Record updated') },
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    createRecord.mutate({
      vehicleId: fd.get('vehicleId') as string,
      type: fd.get('type') as string,
      category: fd.get('category') as string,
      scheduledDate: fd.get('scheduledDate') as string,
      vendorName: fd.get('vendorName') as string || undefined,
      vendorContact: fd.get('vendorContact') as string || undefined,
      notes: fd.get('notes') as string || undefined,
      faultReported: category === 'FaultRepair',
      faultDescription: fd.get('faultDescription') as string || undefined,
      dateReported: fd.get('dateReported') as string || undefined,
      partsReplaced: fd.get('partsReplaced') as string || undefined,
      repairRemarks: fd.get('repairRemarks') as string || undefined,
    })
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Maintenance</h1>
        <div className="flex items-center gap-2 flex-wrap">
          <select value={categoryFilter} onChange={e => setCategoryFilter(e.target.value)} className="input w-auto">
            {CATEGORY_FILTER.map(c => <option key={c} value={c}>{c || 'All Categories'}</option>)}
          </select>
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
            {STATUS_FILTER.map(s => <option key={s} value={s}>{s || 'All Status'}</option>)}
          </select>
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ New Record</button>
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
                {vehicles.map(v => (
                  <option key={v.id} value={v.id}>{v.registrationNo} — {v.make} {v.model}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="label">Category</label>
              <select name="category" className="input" value={category}
                onChange={e => setCategory(e.target.value)}>
                <option value="Routine">Routine Service</option>
                <option value="FaultRepair">Fault / Repair</option>
              </select>
            </div>
            <div>
              <label className="label">Service Type</label>
              <select name="type" className="input">
                <option>Oil Change</option>
                <option>Routine Service</option>
                <option>Tyre Replacement</option>
                <option>Brake Service</option>
                <option>Engine Repair</option>
                <option>Electrical Fault</option>
                <option>Inspection</option>
                <option>Other</option>
              </select>
            </div>
            <div>
              <label className="label">Scheduled Date</label>
              <input name="scheduledDate" type="date" className="input" required />
            </div>
            <div><label className="label">Vendor / Workshop</label><input name="vendorName" className="input" /></div>
            <div><label className="label">Vendor Contact</label><input name="vendorContact" className="input" /></div>
            {category === 'FaultRepair' && (
              <>
                <div className="md:col-span-2">
                  <label className="label">Fault Description</label>
                  <textarea name="faultDescription" className="input" rows={2} placeholder="Describe the fault reported…" />
                </div>
                <div>
                  <label className="label">Date Fault Reported</label>
                  <input name="dateReported" type="date" className="input" />
                </div>
                <div>
                  <label className="label">Parts Replaced</label>
                  <input name="partsReplaced" className="input" placeholder="e.g. Brake pads, oil filter" />
                </div>
                <div className="md:col-span-2">
                  <label className="label">Repair Remarks</label>
                  <textarea name="repairRemarks" className="input" rows={2} />
                </div>
              </>
            )}
            <div className="md:col-span-2">
              <label className="label">Notes</label>
              <textarea name="notes" className="input" rows={2} />
            </div>
            <div className="md:col-span-2 flex gap-3">
              <button type="submit" className="btn-primary" disabled={createRecord.isPending}>Save</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      <div className="space-y-3">
        {records.map(r => (
          <div key={r.id} className={`card p-4 ${
            r.status === 'Overdue' ? 'border-l-4 border-red-500' :
            r.category === 'FaultRepair' ? 'border-l-4 border-orange-400' : ''
          }`}>
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-sm font-bold text-gray-900">{r.vehicleReg}</span>
                  <span className="text-sm text-gray-600">{r.type}</span>
                  <StatusBadge status={r.status} />
                  <StatusBadge status={r.category} />
                </div>
                {r.faultDescription && (
                  <p className="text-xs text-orange-700 mt-1">⚠ {r.faultDescription}</p>
                )}
                {r.partsReplaced && (
                  <p className="text-xs text-gray-500 mt-1">Parts replaced: {r.partsReplaced}</p>
                )}
                {r.repairRemarks && (
                  <p className="text-xs text-gray-500">Remarks: {r.repairRemarks}</p>
                )}
                <p className="text-xs text-gray-400 mt-1">
                  Scheduled: {r.scheduledDate}
                  {r.vendorName && ` · ${r.vendorName}`}
                  {r.cost != null && ` · ₦${r.cost.toLocaleString()}`}
                </p>
              </div>
              {(r.status === 'Scheduled' || r.status === 'InProgress' || r.status === 'Overdue') && (
                <button
                  className="btn-secondary text-xs"
                  onClick={() => completeRecord.mutate({
                    id: r.id,
                    data: {
                      status: 'Completed',
                      completedDate: new Date().toISOString().split('T')[0],
                    },
                  })}
                  disabled={completeRecord.isPending}
                >
                  Mark Complete
                </button>
              )}
            </div>
          </div>
        ))}
        {records.length === 0 && (
          <div className="card p-12 text-center text-gray-400">No maintenance records found</div>
        )}
      </div>
    </div>
  )
}
