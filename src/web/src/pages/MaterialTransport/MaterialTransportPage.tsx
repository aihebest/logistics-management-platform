import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { materialTransportApi, vehiclesApi, driversApi, apiErrorMessage } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const statusLabel: Record<string, string> = {
  PendingHOD: 'Pending HOD',
  PendingManager: 'Pending GM Logistics',
  Approved: 'Approved',
  Rejected: 'Rejected',
  InProgress: 'In Progress',
}

export default function MaterialTransportPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [itemRows, setItemRows] = useState([{ sNo: 1, material: '', description: '', quantity: 1 }])

  const { data: requests = [], isLoading } = useQuery({
    queryKey: ['material-transport', statusFilter],
    queryFn: () => materialTransportApi.getAll({ status: statusFilter || undefined }),
  })

  const { data: selected } = useQuery({
    queryKey: ['material-transport', selectedId],
    queryFn: () => materialTransportApi.get(selectedId!),
    enabled: !!selectedId,
  })

  const { data: drivers = [] } = useQuery({ queryKey: ['drivers'], queryFn: driversApi.getAll })
  const { data: vehicles = [] } = useQuery({ queryKey: ['vehicles'], queryFn: () => vehiclesApi.getAll() })

  const createRequest = useMutation({
    mutationFn: materialTransportApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['material-transport'] }); setShowForm(false); toast.success('Request submitted — pending HOD approval') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to submit request'), { duration: 6000 }),
  })

  const hodApproval = useMutation({
    mutationFn: ({ id, action, remarks }: { id: string; action: string; remarks?: string }) =>
      materialTransportApi.hodApproval(id, action, remarks),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['material-transport'] }); toast.success('HOD decision recorded') },
  })

  const managerApproval = useMutation({
    mutationFn: ({ id, action, remarks }: { id: string; action: string; remarks?: string }) =>
      materialTransportApi.managerApproval(id, action, remarks),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['material-transport'] }); toast.success('Manager decision recorded') },
  })

  const assignResources = useMutation({
    mutationFn: ({ id, driverId, vehicleId }: { id: string; driverId: string; vehicleId: string }) =>
      materialTransportApi.assign(id, driverId, vehicleId),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['material-transport'] }); toast.success('Driver & vehicle assigned') },
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    createRequest.mutate({
      projectName: fd.get('projectName') as string,
      purpose: fd.get('purpose') as string,
      loadingPoint: fd.get('loadingPoint') as string,
      loadingContactPerson: fd.get('loadingContactPerson') as string || undefined,
      loadingContactPhone: fd.get('loadingContactPhone') as string || undefined,
      loadingDate: fd.get('loadingDate') as string || undefined,
      deliveryPoint: fd.get('deliveryPoint') as string,
      deliveryContactPerson: fd.get('deliveryContactPerson') as string || undefined,
      deliveryContactPhone: fd.get('deliveryContactPhone') as string || undefined,
      deliveryDate: fd.get('deliveryDate') as string || undefined,
      items: itemRows.filter(r => r.material.trim()),
    })
  }

  const addRow = () => setItemRows(r => [...r, { sNo: r.length + 1, material: '', description: '', quantity: 1 }])
  const removeRow = (i: number) => setItemRows(r => r.filter((_, idx) => idx !== i).map((r, idx) => ({ ...r, sNo: idx + 1 })))
  const updateRow = (i: number, field: string, value: string | number) =>
    setItemRows(r => r.map((row, idx) => idx === i ? { ...row, [field]: value } : row))

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Material Transport</h1>
          <p className="text-xs text-gray-500 mt-0.5">Form DEL-LG-FRM-009 — 3-level approval workflow</p>
        </div>
        <div className="flex items-center gap-2">
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
            <option value="">All Status</option>
            {Object.entries(statusLabel).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ New Request</button>
        </div>
      </div>

      {/* Approval workflow banner */}
      <div className="flex items-center gap-2 text-xs text-gray-500 bg-gray-50 border border-gray-200 rounded-lg px-4 py-2">
        <span className="font-medium text-gray-700">Workflow:</span>
        <span className="px-2 py-0.5 bg-blue-100 text-blue-800 rounded">Requestor submits</span>
        <span>→</span>
        <span className="px-2 py-0.5 bg-yellow-100 text-yellow-800 rounded">HOD approves</span>
        <span>→</span>
        <span className="px-2 py-0.5 bg-purple-100 text-purple-800 rounded">GM Logistics approves</span>
        <span>→</span>
        <span className="px-2 py-0.5 bg-green-100 text-green-800 rounded">Driver assigned</span>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">New Material Transport Request</h2>
          <form onSubmit={handleSubmit} className="space-y-5">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div><label className="label">Project Name</label><input name="projectName" className="input" required /></div>
              <div><label className="label">Purpose / Description</label><input name="purpose" className="input" required /></div>
              <div><label className="label">Loading Point</label><input name="loadingPoint" className="input" required /></div>
              <div><label className="label">Loading Contact Person</label><input name="loadingContactPerson" className="input" /></div>
              <div><label className="label">Loading Contact Phone</label><input name="loadingContactPhone" className="input" /></div>
              <div><label className="label">Loading Date</label><input name="loadingDate" type="date" className="input" /></div>
              <div><label className="label">Delivery Point</label><input name="deliveryPoint" className="input" required /></div>
              <div><label className="label">Delivery Contact Person</label><input name="deliveryContactPerson" className="input" /></div>
              <div><label className="label">Delivery Contact Phone</label><input name="deliveryContactPhone" className="input" /></div>
              <div><label className="label">Delivery Date</label><input name="deliveryDate" type="date" className="input" /></div>
            </div>

            <div>
              <div className="flex items-center justify-between mb-2">
                <label className="label mb-0">Materials to Transport</label>
                <button type="button" onClick={addRow} className="text-xs text-brand-600 hover:underline">+ Add Row</button>
              </div>
              <div className="overflow-x-auto border border-gray-200 rounded-lg">
                <table className="min-w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 w-12">S/No</th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-gray-500">Material</th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-gray-500">Description</th>
                      <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 w-24">Qty</th>
                      <th className="w-8" />
                    </tr>
                  </thead>
                  <tbody>
                    {itemRows.map((row, i) => (
                      <tr key={i} className="border-t border-gray-100">
                        <td className="px-3 py-1 text-gray-500">{row.sNo}</td>
                        <td className="px-3 py-1">
                          <input className="input py-1 text-sm" value={row.material}
                            onChange={e => updateRow(i, 'material', e.target.value)} required />
                        </td>
                        <td className="px-3 py-1">
                          <input className="input py-1 text-sm" value={row.description}
                            onChange={e => updateRow(i, 'description', e.target.value)} />
                        </td>
                        <td className="px-3 py-1">
                          <input type="number" className="input py-1 text-sm" value={row.quantity} min={1}
                            onChange={e => updateRow(i, 'quantity', Number(e.target.value))} />
                        </td>
                        <td className="px-3 py-1">
                          {itemRows.length > 1 && (
                            <button type="button" onClick={() => removeRow(i)} className="text-red-400 hover:text-red-600 text-xs">✕</button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="flex gap-3">
              <button type="submit" className="btn-primary" disabled={createRequest.isPending}>Submit Request</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {/* Selected request detail */}
      {selected && (
        <div className="card p-5 border-l-4 border-brand-500">
          <div className="flex items-center justify-between mb-3">
            <div>
              <h2 className="font-semibold text-gray-900">{selected.formNumber}</h2>
              <p className="text-xs text-gray-500">{selected.projectName} · {selected.requestedByName}</p>
            </div>
            <button onClick={() => setSelectedId(null)} className="text-gray-400 hover:text-gray-600 text-sm">✕</button>
          </div>

          {/* Approval actions */}
          {hasRole('Manager', 'Admin') && selected.status === 'PendingHOD' && (
            <div className="flex gap-2 mb-4">
              <button onClick={() => hodApproval.mutate({ id: selected.id, action: 'Approve' })}
                className="btn-primary text-xs">HOD Approve</button>
              <button onClick={() => hodApproval.mutate({ id: selected.id, action: 'Reject', remarks: 'Rejected by HOD' })}
                className="text-xs px-3 py-1.5 bg-red-600 text-white rounded-lg">HOD Reject</button>
            </div>
          )}
          {hasRole('Manager', 'Admin') && selected.status === 'PendingManager' && (
            <div className="flex gap-2 mb-4">
              <button onClick={() => managerApproval.mutate({ id: selected.id, action: 'Approve' })}
                className="btn-primary text-xs">GM Approve</button>
              <button onClick={() => managerApproval.mutate({ id: selected.id, action: 'Reject', remarks: 'Rejected by GM' })}
                className="text-xs px-3 py-1.5 bg-red-600 text-white rounded-lg">GM Reject</button>
            </div>
          )}
          {hasRole('Coordinator', 'Manager', 'Admin') && selected.status === 'Approved' && (
            <div className="flex gap-2 items-end mb-4">
              <div>
                <label className="label">Driver</label>
                <select id="sel-driver" className="input">
                  {drivers.map(d => <option key={d.id} value={d.id}>{d.fullName}</option>)}
                </select>
              </div>
              <div>
                <label className="label">Vehicle</label>
                <select id="sel-vehicle" className="input">
                  {vehicles.map(v => <option key={v.id} value={v.id}>{v.registrationNo}</option>)}
                </select>
              </div>
              <button className="btn-primary text-xs" onClick={() => {
                const d = (document.getElementById('sel-driver') as HTMLSelectElement).value
                const v = (document.getElementById('sel-vehicle') as HTMLSelectElement).value
                assignResources.mutate({ id: selected.id, driverId: d, vehicleId: v })
              }}>Assign</button>
            </div>
          )}

          {/* Items table */}
          <table className="min-w-full text-sm border border-gray-200 rounded-lg overflow-hidden">
            <thead className="bg-gray-50">
              <tr>
                {['S/No', 'Material', 'Description', 'Qty'].map(h => (
                  <th key={h} className="px-3 py-2 text-left text-xs font-medium text-gray-500">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {selected.items.map(item => (
                <tr key={item.id} className="border-t border-gray-100">
                  <td className="px-3 py-2 text-gray-500">{item.sNo}</td>
                  <td className="px-3 py-2 font-medium text-gray-900">{item.material}</td>
                  <td className="px-3 py-2 text-gray-600">{item.description || '—'}</td>
                  <td className="px-3 py-2">{item.quantity}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="space-y-3">
        {requests.map(r => (
          <div key={r.id} className="card p-4 hover:shadow-md transition-shadow cursor-pointer"
            onClick={() => setSelectedId(selectedId === r.id ? null : r.id)}>
            <div className="flex items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-xs font-mono text-gray-500">{r.formNumber}</span>
                  <span className="text-sm font-semibold text-gray-900">{r.projectName}</span>
                  <span className="px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700">
                    {statusLabel[r.status] ?? r.status}
                  </span>
                </div>
                <p className="text-sm text-gray-500 mt-1">{r.purpose}</p>
                <p className="text-xs text-gray-400 mt-1">
                  {r.loadingPoint} → {r.deliveryPoint}
                  {r.loadingDate && ` · Loading: ${r.loadingDate}`}
                </p>
                <p className="text-xs text-gray-400">
                  By {r.requestedByName} · {format(new Date(r.createdAt), 'dd MMM yyyy')}
                  {r.assignedDriverName && ` · Driver: ${r.assignedDriverName} (${r.assignedVehicleReg})`}
                </p>
              </div>
              <div className="text-xs text-gray-400">{r.items.length} item(s)</div>
            </div>
          </div>
        ))}
        {requests.length === 0 && (
          <div className="card p-12 text-center text-gray-400">No material transport requests found</div>
        )}
      </div>
    </div>
  )
}
