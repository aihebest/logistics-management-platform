import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  parcelsApi, departmentsApi, locationsApi, authApi, apiErrorMessage,
  type ParcelDispatch,
} from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

/**
 * Parcel Dispatch — documents, award plaques, certificates sent by courier.
 *
 * Flow: sender submits → head of the sender's department approves → GM
 * Logistics approves → Logistics dispatches and records the tracking number.
 */

/** Which couriers serve which scope. Mirrors CarriersByScope in the controller. */
const CARRIERS: Record<string, string[]> = {
  Domestic: ['FedEx', 'Airport Dispatch'],
  International: ['DHL'],
}

const PRIORITIES = ['Normal', 'High', 'Urgent']

const STATUS_FILTER = ['', 'PendingHOD', 'PendingManager', 'Approved', 'Dispatched', 'Rejected', 'Cancelled']

const STATUS_LABEL: Record<string, string> = {
  PendingHOD:     'Awaiting HOD approval',
  PendingManager: 'Awaiting GM Logistics',
  Approved:       'Approved — to dispatch',
  Dispatched:     'Dispatched',
  Rejected:       'Rejected',
  Cancelled:      'Cancelled',
}

const STATUS_STYLE: Record<string, string> = {
  PendingHOD:     'bg-amber-100 text-amber-800',
  PendingManager: 'bg-blue-100 text-blue-800',
  Approved:       'bg-indigo-100 text-indigo-800',
  Dispatched:     'bg-green-100 text-green-800',
  Rejected:       'bg-red-100 text-red-800',
  Cancelled:      'bg-gray-100 text-gray-600',
}

export default function ParcelDispatchPage() {
  const qc = useQueryClient()
  const { hasRole } = useAuth()
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [scope, setScope] = useState('Domestic')

  const { data: parcels = [], isLoading } = useQuery({
    queryKey: ['parcels', statusFilter],
    queryFn: () => parcelsApi.getAll({ status: statusFilter || undefined }),
  })
  const { data: departments = [] } = useQuery({ queryKey: ['departments'], queryFn: () => departmentsApi.getAll() })
  const { data: locations = [] } = useQuery({ queryKey: ['locations'], queryFn: locationsApi.getAll })
  const { data: me } = useQuery({ queryKey: ['me'], queryFn: authApi.me })

  const isGm = hasRole('Manager', 'Admin')
  const canDispatch = hasRole('Coordinator', 'Manager', 'Admin')
  const isOps = hasRole('Coordinator', 'Manager', 'Admin')

  const refresh = () => qc.invalidateQueries({ queryKey: ['parcels'] })
  const onErr = (fallback: string) => (err: unknown) =>
    toast.error(apiErrorMessage(err, fallback), { duration: 7000 })

  const create = useMutation({
    mutationFn: parcelsApi.create,
    onSuccess: (p: ParcelDispatch) => {
      refresh(); setShowForm(false)
      toast.success(`${p.formNumber} submitted for approval`)
    },
    onError: onErr('Failed to submit parcel request'),
  })

  const hod = useMutation({
    mutationFn: ({ id, action, remarks }: { id: string; action: 'Approve' | 'Reject'; remarks?: string }) =>
      parcelsApi.hodDecision(id, action, remarks),
    onSuccess: () => { refresh(); toast.success('Decision recorded') },
    onError: onErr('Failed to record decision'),
  })

  const gm = useMutation({
    mutationFn: ({ id, action, remarks }: { id: string; action: 'Approve' | 'Reject'; remarks?: string }) =>
      parcelsApi.managerDecision(id, action, remarks),
    onSuccess: () => { refresh(); toast.success('Decision recorded') },
    onError: onErr('Failed to record decision'),
  })

  const dispatch = useMutation({
    mutationFn: ({ id, trackingNumber }: { id: string; trackingNumber?: string }) =>
      parcelsApi.dispatch(id, trackingNumber),
    onSuccess: () => { refresh(); toast.success('Marked as dispatched — sender notified') },
    onError: onErr('Failed to mark as dispatched'),
  })

  const cancel = useMutation({
    mutationFn: parcelsApi.cancel,
    onSuccess: () => { refresh(); toast.success('Parcel request cancelled') },
    onError: onErr('Failed to cancel'),
  })

  /** Ask for a reason, then reject at whichever stage the parcel is in. */
  const handleReject = (p: ParcelDispatch, stage: 'hod' | 'gm') => {
    const reason = window.prompt(`Why is ${p.formNumber} not being approved?`)
    if (reason === null) return
    const args = { id: p.id, action: 'Reject' as const, remarks: reason.trim() || undefined }
    stage === 'hod' ? hod.mutate(args) : gm.mutate(args)
  }

  const handleDispatch = (p: ParcelDispatch) => {
    const tracking = window.prompt(`Tracking / waybill number for ${p.formNumber} (${p.carrier}):`)
    if (tracking === null) return
    dispatch.mutate({ id: p.id, trackingNumber: tracking.trim() || undefined })
  }

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    const str = (k: string) => (fd.get(k) as string | null)?.trim() || undefined
    create.mutate({
      senderName: str('senderName'),
      senderLocation: str('senderLocation'),
      senderDepartment: str('senderDepartment'),
      senderPhone: str('senderPhone'),
      receiverName: str('receiverName'),
      receiverAddress: str('receiverAddress'),
      receiverPhone: str('receiverPhone'),
      description: str('description'),
      isFragile: fd.get('isFragile') === 'on',
      priority: str('priority') ?? 'Normal',
      scope,
      carrier: str('carrier'),
    })
  }

  const headsDept = (dept: string) => !!me && departments.some(d => d.name === dept && d.hodUserId === me.id)

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="rounded-lg bg-blue-50 border border-blue-200 px-4 py-3 text-sm text-blue-800">
        <strong>Parcel Dispatch.</strong> Parcels from the <strong>DEL PHC office</strong> are dispatched
        once a week, on <strong>Fridays</strong>. <strong>Urgent</strong> parcels are dispatched immediately
        once approved. Requests are approved by your head of department, then GM Logistics.
      </div>

      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Parcel Dispatch</h1>
        <div className="flex items-center gap-2 flex-wrap">
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
            {STATUS_FILTER.map(s => <option key={s} value={s}>{s ? STATUS_LABEL[s] : 'All Statuses'}</option>)}
          </select>
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ New Parcel</button>
        </div>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">New Parcel Dispatch Request</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="md:col-span-3"><h3 className="text-sm font-semibold text-gray-700">Sender</h3></div>
            <div>
              <label className="label">Name of Sender <span className="text-red-500">*</span></label>
              <input name="senderName" className="input" required defaultValue={me?.fullName ?? ''} />
            </div>
            <div>
              <label className="label">Location <span className="text-red-500">*</span></label>
              <select name="senderLocation" className="input" required defaultValue="">
                <option value="" disabled>Select office…</option>
                {locations.map(l => <option key={l.id} value={l.name}>{l.name}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Department <span className="text-red-500">*</span></label>
              <select name="senderDepartment" className="input" required defaultValue={me?.departmentName ?? ''}>
                <option value="" disabled>Select department…</option>
                {departments.map(d => <option key={d.id} value={d.name}>{d.name}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Phone Number</label>
              <input name="senderPhone" className="input" defaultValue={me?.phoneNumber ?? ''} />
            </div>

            <div className="md:col-span-3 pt-2 border-t border-gray-100"><h3 className="text-sm font-semibold text-gray-700">Receiver</h3></div>
            <div>
              <label className="label">Receiver's Name <span className="text-red-500">*</span></label>
              <input name="receiverName" className="input" required />
            </div>
            <div>
              <label className="label">Receiver's Phone Number</label>
              <input name="receiverPhone" className="input" />
            </div>
            <div className="md:col-span-3">
              <label className="label">Receiver's Address <span className="text-red-500">*</span></label>
              <textarea name="receiverAddress" className="input" rows={2} required />
            </div>

            <div className="md:col-span-3 pt-2 border-t border-gray-100"><h3 className="text-sm font-semibold text-gray-700">Parcel</h3></div>
            <div className="md:col-span-3">
              <label className="label">Description of Parcel <span className="text-red-500">*</span></label>
              <input name="description" className="input" required
                     placeholder="e.g. Documents, award plaques, certificates" />
            </div>
            <div>
              <label className="label">Priority</label>
              <select name="priority" className="input" defaultValue="Normal">
                {PRIORITIES.map(p => <option key={p}>{p}</option>)}
              </select>
              <p className="text-xs text-gray-500 mt-1">Urgent parcels skip the Friday run.</p>
            </div>
            <div>
              <label className="label">Dispatch Option</label>
              <select className="input" value={scope} onChange={e => setScope(e.target.value)}>
                <option value="Domestic">Domestic</option>
                <option value="International">International</option>
              </select>
            </div>
            <div>
              <label className="label">Courier</label>
              {/* Keyed on scope so switching Domestic/International resets the choice. */}
              <select key={scope} name="carrier" className="input" defaultValue={CARRIERS[scope][0]}>
                {CARRIERS[scope].map(c => <option key={c}>{c}</option>)}
              </select>
            </div>
            <div className="md:col-span-3">
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input type="checkbox" name="isFragile" className="h-4 w-4" />
                Fragile — handle with care
              </label>
            </div>

            <div className="md:col-span-3 flex gap-3">
              <button type="submit" className="btn-primary" disabled={create.isPending}>
                {create.isPending ? 'Submitting…' : 'Submit Request'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      <div className="space-y-3">
        {parcels.map(p => {
          const mine = p.requestedById === me?.id
          const showHod = p.status === 'PendingHOD' && !mine && (headsDept(p.senderDepartment) || hasRole('Admin'))
          const showGm = p.status === 'PendingManager' && isGm && !mine && p.hodApprovedByName !== me?.fullName
          return (
            <div key={p.id} className="card p-4">
              <div className="flex items-start justify-between gap-4 flex-wrap">
                <div className="flex-1 min-w-[260px]">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-sm font-bold text-gray-900">{p.formNumber}</span>
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${STATUS_STYLE[p.status] ?? 'bg-gray-100'}`}>
                      {STATUS_LABEL[p.status] ?? p.status}
                    </span>
                    {p.priority !== 'Normal' && (
                      <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                        p.priority === 'Urgent' ? 'bg-red-100 text-red-800' : 'bg-orange-100 text-orange-800'}`}>
                        {p.priority}
                      </span>
                    )}
                    {p.isFragile && <span className="px-2 py-0.5 rounded text-xs font-medium bg-purple-100 text-purple-800">Fragile</span>}
                    <span className="px-2 py-0.5 rounded text-xs bg-gray-100 text-gray-600">{p.scope} · {p.carrier}</span>
                  </div>
                  <p className="text-sm text-gray-700 mt-1">
                    {p.senderName} ({p.senderLocation}) → <strong>{p.receiverName}</strong>
                  </p>
                  <p className="text-xs text-gray-500">{p.receiverAddress}</p>
                  <p className="text-sm text-gray-600 mt-1">{p.description}</p>
                  <p className="text-xs text-gray-400 mt-1">
                    {p.senderDepartment} · raised {format(new Date(p.createdAt), 'dd MMM yyyy')}
                  </p>
                  {p.expectedDispatchDate && (
                    <p className="text-xs text-blue-700 mt-1">
                      Expected dispatch: Friday {format(new Date(p.expectedDispatchDate), 'dd MMM yyyy')} (weekly PHC run)
                    </p>
                  )}
                  {p.hodApprovedByName && p.status !== 'Rejected' && (
                    <p className="text-xs text-green-700 mt-1">HOD approved: {p.hodApprovedByName}</p>
                  )}
                  {p.managerApprovedByName && p.status !== 'Rejected' && (
                    <p className="text-xs text-green-700">GM Logistics approved: {p.managerApprovedByName}</p>
                  )}
                  {p.status === 'Dispatched' && (
                    <p className="text-xs text-green-800 mt-1 font-medium">
                      Dispatched {p.dispatchedAt && format(new Date(p.dispatchedAt), 'dd MMM yyyy HH:mm')}
                      {p.trackingNumber && ` · Tracking: ${p.trackingNumber}`}
                    </p>
                  )}
                  {p.rejectionReason && <p className="text-xs text-red-600 mt-1">Rejected: {p.rejectionReason}</p>}
                </div>

                <div className="flex items-center gap-2 flex-wrap flex-shrink-0">
                  {showHod && (
                    <>
                      <button className="btn-primary text-xs" disabled={hod.isPending}
                        onClick={() => hod.mutate({ id: p.id, action: 'Approve' })}>Approve</button>
                      <button className="btn-secondary text-xs text-red-600" onClick={() => handleReject(p, 'hod')}>Reject</button>
                    </>
                  )}
                  {showGm && (
                    <>
                      <button className="btn-primary text-xs" disabled={gm.isPending}
                        onClick={() => gm.mutate({ id: p.id, action: 'Approve' })}>Approve (GM)</button>
                      <button className="btn-secondary text-xs text-red-600" onClick={() => handleReject(p, 'gm')}>Reject</button>
                    </>
                  )}
                  {p.status === 'Approved' && canDispatch && (
                    <button className="btn-primary text-xs" disabled={dispatch.isPending}
                      onClick={() => handleDispatch(p)}>Mark Dispatched</button>
                  )}
                  {(p.status === 'PendingHOD' || p.status === 'PendingManager') && (mine || isOps) && (
                    <button className="btn-secondary text-xs" disabled={cancel.isPending}
                      onClick={() => cancel.mutate(p.id)}>Cancel</button>
                  )}
                </div>
              </div>
            </div>
          )
        })}
        {parcels.length === 0 && (
          <div className="card p-12 text-center text-gray-400">No parcel requests found</div>
        )}
      </div>
    </div>
  )
}
