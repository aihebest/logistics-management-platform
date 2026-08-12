import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { projectMaterialsApi, apiErrorMessage } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'

const DELIVERY_STATUSES = ['Pending', 'InTransit', 'Customs', 'Delivered', 'Delayed', 'OnHold']
const CURRENT_YEAR = new Date().getFullYear()
const YEARS = Array.from({ length: 4 }, (_, i) => CURRENT_YEAR - i)

export default function ProjectMaterialsPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [year, setYear] = useState(CURRENT_YEAR)
  const [projectFilter, setProjectFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  const { data: materials = [], isLoading } = useQuery({
    queryKey: ['project-materials', year, projectFilter, statusFilter],
    queryFn: () => projectMaterialsApi.getAll({
      year,
      project: projectFilter || undefined,
      status: statusFilter || undefined,
    }),
  })

  const { data: projects = [] } = useQuery({
    queryKey: ['project-materials-projects', year],
    queryFn: () => projectMaterialsApi.getProjects(year),
  })

  const createEntry = useMutation({
    mutationFn: projectMaterialsApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['project-materials'] }); setShowForm(false); toast.success('Entry added') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to add entry'), { duration: 6000 }),
  })

  const updateEntry = useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => projectMaterialsApi.update(id, data),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['project-materials'] }); setEditingId(null); toast.success('Entry updated') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to update'), { duration: 6000 }),
  })

  const deleteEntry = useMutation({
    mutationFn: projectMaterialsApi.delete,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['project-materials'] }); toast.success('Entry deleted') },
  })

  const handleCreate = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    createEntry.mutate({
      trackingYear: year,
      poNumber: fd.get('poNumber') as string || undefined,
      poLineItem: fd.get('poLineItem') as string || undefined,
      project: fd.get('project') as string || undefined,
      buyer: fd.get('buyer') as string || undefined,
      description: fd.get('description') as string,
      quantity: fd.get('quantity') ? Number(fd.get('quantity')) : undefined,
      supplier: fd.get('supplier') as string || undefined,
      freightForwarder: fd.get('freightForwarder') as string || undefined,
      readinessDate: fd.get('readinessDate') as string || undefined,
      modeOfTransport: fd.get('modeOfTransport') as string || undefined,
    })
  }

  const handleUpdate = (e: React.FormEvent<HTMLFormElement>, id: string) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    updateEntry.mutate({
      id,
      data: {
        deliveryStatus: fd.get('deliveryStatus') as string || undefined,
        pickupAuthDate: fd.get('pickupAuthDate') as string || undefined,
        pickupDate: fd.get('pickupDate') as string || undefined,
        formMNumber: fd.get('formMNumber') as string || undefined,
        blAwbNumber: fd.get('blAwbNumber') as string || undefined,
        vesselName: fd.get('vesselName') as string || undefined,
        etd: fd.get('etd') as string || undefined,
        eta: fd.get('eta') as string || undefined,
        actualDeliveryDate: fd.get('actualDeliveryDate') as string || undefined,
        remarks: fd.get('remarks') as string || undefined,
        freightForwarder: fd.get('freightForwarder') as string || undefined,
        // ISO audit fields
        expectedDeliveryDateProjectTeam: fd.get('expectedDeliveryDateProjectTeam') as string || undefined,
        storeNotificationDate: fd.get('storeNotificationDate') as string || undefined,
        expectedDeliveryDateStoreTeam: fd.get('expectedDeliveryDateStoreTeam') as string || undefined,
        expectedDeliveryDateAgreed: fd.get('expectedDeliveryDateAgreed') as string || undefined,
        paarNumber: fd.get('paarNumber') as string || undefined,
        paarDate: fd.get('paarDate') as string || undefined,
        blNumber: fd.get('blNumber') as string || undefined,
        awbNumber: fd.get('awbNumber') as string || undefined,
      },
    })
  }

  const statusColor: Record<string, string> = {
    Pending: 'bg-gray-100 text-gray-700',
    InTransit: 'bg-blue-100 text-blue-800',
    Customs: 'bg-yellow-100 text-yellow-800',
    Delivered: 'bg-green-100 text-green-800',
    Delayed: 'bg-red-100 text-red-800',
    OnHold: 'bg-orange-100 text-orange-800',
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Project Materials Status Register</h1>
          <p className="text-xs text-gray-500 mt-0.5">Digital replacement for the annual STATUS REPORT spreadsheet</p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <select value={year} onChange={e => setYear(Number(e.target.value))} className="input w-auto">
            {YEARS.map(y => <option key={y}>{y}</option>)}
          </select>
          <select value={projectFilter} onChange={e => setProjectFilter(e.target.value)} className="input w-auto">
            <option value="">All Projects</option>
            {projects.map(p => <option key={p}>{p}</option>)}
          </select>
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
            <option value="">All Status</option>
            {DELIVERY_STATUSES.map(s => <option key={s}>{s}</option>)}
          </select>
          {hasRole('Coordinator', 'Manager', 'Admin') && (
            <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ Add Entry</button>
          )}
        </div>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">New Material Tracking Entry — {year}</h2>
          <form onSubmit={handleCreate} className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div><label className="label">PO Number</label><input name="poNumber" className="input" /></div>
            <div><label className="label">PO Line Item</label><input name="poLineItem" className="input" /></div>
            <div><label className="label">Project</label><input name="project" className="input" /></div>
            <div><label className="label">Buyer</label><input name="buyer" className="input" /></div>
            <div className="col-span-2"><label className="label">Description</label><input name="description" className="input" required /></div>
            <div><label className="label">Quantity</label><input name="quantity" type="number" step="0.01" className="input" /></div>
            <div><label className="label">Supplier</label><input name="supplier" className="input" /></div>
            <div><label className="label">Freight Forwarder</label><input name="freightForwarder" className="input" /></div>
            <div><label className="label">Readiness Date</label><input name="readinessDate" type="date" className="input" /></div>
            <div><label className="label">Mode of Transport</label>
              <select name="modeOfTransport" className="input">
                <option value="">—</option>
                <option>Sea Freight</option><option>Air Freight</option><option>Road</option><option>Rail</option>
              </select>
            </div>
            <div className="col-span-full flex gap-3">
              <button type="submit" className="btn-primary" disabled={createEntry.isPending}>Save</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {/* Status summary bar */}
      <div className="flex gap-3 flex-wrap">
        {DELIVERY_STATUSES.map(s => {
          const count = materials.filter(m => m.deliveryStatus === s).length
          return count > 0 ? (
            <span key={s} className={`px-3 py-1 rounded-full text-xs font-medium ${statusColor[s]}`}>
              {s}: {count}
            </span>
          ) : null
        })}
      </div>

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['PO No', 'Project', 'Description', 'Qty', 'Supplier', 'Mode', 'Form M',
                  'PAAR', 'BL No', 'AWB No', 'ETD', 'ETA',
                  'Exp. (Project)', 'Store Notified', 'Exp. (Store)', 'Agreed', 'Actual Delivery',
                  'Status', ''].map(h => (
                  <th key={h} className="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {materials.map(m => (
                editingId === m.id ? (
                  <tr key={m.id} className="bg-blue-50">
                    <td colSpan={19} className="px-4 py-3">
                      <form onSubmit={e => handleUpdate(e, m.id)} className="grid grid-cols-2 md:grid-cols-4 gap-3">
                        <div>
                          <label className="label">Status</label>
                          <select name="deliveryStatus" className="input" defaultValue={m.deliveryStatus}>
                            {DELIVERY_STATUSES.map(s => <option key={s}>{s}</option>)}
                          </select>
                        </div>
                        <div><label className="label">Freight Forwarder</label><input name="freightForwarder" className="input" defaultValue={m.freightForwarder ?? ''} /></div>
                        <div><label className="label">Form M No</label><input name="formMNumber" className="input" defaultValue={m.formMNumber ?? ''} /></div>
                        <div><label className="label">Vessel</label><input name="vesselName" className="input" defaultValue={m.vesselName ?? ''} /></div>
                        <div><label className="label">Pickup Auth Date</label><input name="pickupAuthDate" type="date" className="input" defaultValue={m.pickupAuthDate ?? ''} /></div>
                        <div><label className="label">Pickup Date</label><input name="pickupDate" type="date" className="input" defaultValue={m.pickupDate ?? ''} /></div>

                        {/* ── Shipping documents (ISO audit) ─────────────────── */}
                        <div className="col-span-full mt-1">
                          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Shipping Documents</p>
                        </div>
                        <div><label className="label">PAAR No</label><input name="paarNumber" className="input" defaultValue={m.paarNumber ?? ''} placeholder="Pre-Arrival Assessment Report" /></div>
                        <div><label className="label">PAAR Date</label><input name="paarDate" type="date" className="input" defaultValue={m.paarDate ?? ''} /></div>
                        <div><label className="label">BL No <span className="text-gray-400">(sea)</span></label><input name="blNumber" className="input" defaultValue={m.blNumber ?? ''} placeholder="Bill of Lading" /></div>
                        <div><label className="label">AWB No <span className="text-gray-400">(air)</span></label><input name="awbNumber" className="input" defaultValue={m.awbNumber ?? ''} placeholder="Air Waybill" /></div>
                        <div><label className="label">ETD</label><input name="etd" type="date" className="input" defaultValue={m.etd ?? ''} /></div>
                        <div><label className="label">ETA</label><input name="eta" type="date" className="input" defaultValue={m.eta ?? ''} /></div>

                        {/* ── Delivery date chain (ISO audit) ────────────────── */}
                        <div className="col-span-full mt-1">
                          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide">Delivery Dates</p>
                        </div>
                        <div><label className="label">Expected — Project Team</label><input name="expectedDeliveryDateProjectTeam" type="date" className="input" defaultValue={m.expectedDeliveryDateProjectTeam ?? ''} /></div>
                        <div><label className="label">Store Team Notified</label><input name="storeNotificationDate" type="date" className="input" defaultValue={m.storeNotificationDate ?? ''} /></div>
                        <div><label className="label">Expected — Store Team</label><input name="expectedDeliveryDateStoreTeam" type="date" className="input" defaultValue={m.expectedDeliveryDateStoreTeam ?? ''} /></div>
                        <div><label className="label">Agreed — Logistics &amp; Supplier</label><input name="expectedDeliveryDateAgreed" type="date" className="input" defaultValue={m.expectedDeliveryDateAgreed ?? ''} /></div>
                        <div><label className="label">Actual Delivery</label><input name="actualDeliveryDate" type="date" className="input" defaultValue={m.actualDeliveryDate ?? ''} /></div>

                        <div className="col-span-full"><label className="label">Remarks</label><input name="remarks" className="input" defaultValue={m.remarks ?? ''} /></div>
                        <div className="col-span-full flex gap-2">
                          <button type="submit" className="btn-primary text-xs" disabled={updateEntry.isPending}>Save Changes</button>
                          <button type="button" className="btn-secondary text-xs" onClick={() => setEditingId(null)}>Cancel</button>
                        </div>
                      </form>
                    </td>
                  </tr>
                ) : (
                  <tr key={m.id} className="hover:bg-gray-50">
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.poNumber || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-700 font-medium">{m.project || '—'}</td>
                    <td className="px-3 py-2 max-w-xs truncate text-gray-900" title={m.description}>{m.description}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.quantity ?? '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.supplier || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.modeOfTransport || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.formMNumber || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.paarNumber || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.blNumber || m.blAwbNumber || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.awbNumber || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.etd || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.eta || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.expectedDeliveryDateProjectTeam || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.storeNotificationDate || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.expectedDeliveryDateStoreTeam || '—'}</td>
                    <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.expectedDeliveryDateAgreed || '—'}</td>
                    {/* Late against the agreed date is flagged for the audit trail */}
                    <td className={`px-3 py-2 whitespace-nowrap ${
                      m.actualDeliveryDate && m.expectedDeliveryDateAgreed
                        && m.actualDeliveryDate > m.expectedDeliveryDateAgreed
                        ? 'text-red-600 font-medium' : 'text-gray-600'
                    }`}>
                      {m.actualDeliveryDate || '—'}
                    </td>
                    <td className="px-3 py-2 whitespace-nowrap">
                      <span className={`px-2 py-0.5 rounded text-xs font-medium ${statusColor[m.deliveryStatus] ?? 'bg-gray-100 text-gray-700'}`}>
                        {m.deliveryStatus}
                      </span>
                    </td>
                    <td className="px-3 py-2 whitespace-nowrap">
                      {hasRole('Coordinator', 'Manager', 'Admin') && (
                        <div className="flex gap-1">
                          <button onClick={() => setEditingId(m.id)} className="text-xs text-brand-600 hover:underline">Edit</button>
                          <button onClick={() => { if (confirm('Delete this entry?')) deleteEntry.mutate(m.id) }}
                            className="text-xs text-red-500 hover:underline">Del</button>
                        </div>
                      )}
                    </td>
                  </tr>
                )
              ))}
              {materials.length === 0 && (
                <tr><td colSpan={19} className="px-4 py-12 text-center text-gray-400">No entries for {year}</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
