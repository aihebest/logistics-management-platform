import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  travelApi, departmentsApi, authApi, apiErrorMessage,
  type TravelRequest, type TravelLeg,
} from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { useAuth } from '../../auth/useAuth'
import TravelRequestForm from './TravelRequestForm'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

/**
 * Travel Request Form — DEL-LG-FRM-002 Rev 07.
 *
 * Flow: the traveller submits, their own head of department verifies, the
 * DMD/MD approves, and Logistics downloads the completed form to book from.
 */

const STATUS_FILTER = ['', 'PendingVerification', 'PendingApproval', 'Approved', 'Rejected', 'Cancelled']

const STATUS_LABEL: Record<string, string> = {
  PendingVerification: 'Awaiting HOD verification',
  PendingApproval:     'Awaiting management approval',
  Approved:            'Approved',
  Rejected:            'Rejected',
  Cancelled:           'Cancelled',
}

const STATUS_STYLE: Record<string, string> = {
  PendingVerification: 'bg-amber-100 text-amber-800',
  PendingApproval:     'bg-blue-100 text-blue-800',
  Approved:            'bg-green-100 text-green-800',
  Rejected:            'bg-red-100 text-red-800',
  Cancelled:           'bg-gray-100 text-gray-600',
}

/** Blank routing row. The paper form gives several, so we start with one each. */
const emptyLeg = (direction: string): TravelLeg => ({
  direction, sequence: 0, travelDate: '', from: '', to: '',
  preferredAirline: '', preferredTime: '',
})

export default function TravelRequestPage() {
  const qc = useQueryClient()
  const { hasRole } = useAuth()
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [printing, setPrinting] = useState<TravelRequest | null>(null)

  const { data: requests = [], isLoading } = useQuery({
    queryKey: ['travel', statusFilter],
    queryFn: () => travelApi.getAll({ status: statusFilter || undefined }),
  })

  const { data: departments = [] } = useQuery({
    queryKey: ['departments'],
    queryFn: () => departmentsApi.getAll(),
  })

  // Used to pre-fill the traveller's own details, as the paper form expects
  // them filled in but the platform already knows most of them.
  const { data: me } = useQuery({ queryKey: ['me'], queryFn: authApi.me })

  // Heading a department is what grants verification, not the role name. The
  // heads of Logistics and General Services carry the Management role because
  // they also approve travel, so a role check alone would hide Verify from them.
  const headsADepartment = !!me && departments.some(d => d.hodUserId === me.id)
  const canVerify  = hasRole('HOD', 'Admin') || headsADepartment
  const canApprove = hasRole('Management', 'Admin')

  const refresh = () => {
    qc.invalidateQueries({ queryKey: ['travel'] })
    qc.invalidateQueries({ queryKey: ['notifications'] })
  }

  const create = useMutation({
    mutationFn: travelApi.create,
    onSuccess: (r: TravelRequest) => {
      refresh()
      setShowForm(false)
      toast.success(`${r.formNumber} submitted — sent to your head of department`)
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to submit travel request'), { duration: 7000 }),
  })

  const verify = useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string }) => travelApi.verify(id, notes),
    onSuccess: () => { refresh(); toast.success('Verified — sent to management for approval') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to verify'), { duration: 7000 }),
  })

  const approve = useMutation({
    mutationFn: ({ id, notes }: { id: string; notes?: string }) => travelApi.approve(id, notes),
    onSuccess: () => { refresh(); toast.success('Approved — Logistics has been notified') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to approve'), { duration: 7000 }),
  })

  const reject = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => travelApi.reject(id, reason),
    onSuccess: () => { refresh(); toast.success('Rejected — the requester has been notified') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to reject'), { duration: 7000 }),
  })

  const cancel = useMutation({
    mutationFn: travelApi.cancel,
    onSuccess: () => { refresh(); toast.success('Request cancelled') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to cancel'), { duration: 6000 }),
  })

  const handleReject = (r: TravelRequest) => {
    const reason = window.prompt(`Why is ${r.formNumber} not being approved?`)
    if (reason === null) return
    reject.mutate({ id: r.id, reason: reason.trim() || 'No reason given' })
  }

  /**
   * Opens the printable replica. The browser's print dialog is what produces
   * the PDF — it keeps the layout identical to the paper form and needs nothing
   * extra running on the server.
   */
  const handleDownload = (r: TravelRequest) => {
    setPrinting(r)
    // Let the replica render before handing over to the print dialog.
    setTimeout(() => window.print(), 100)
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      {/* The replica is the only thing that prints; everything else is hidden
          by the print rules in TravelRequestForm. */}
      {printing && <TravelRequestForm request={printing} onClose={() => setPrinting(null)} />}

      <div className="no-print space-y-4">
        <div className="rounded-lg bg-blue-50 border border-blue-200 px-4 py-3 text-sm text-blue-800">
          <strong>Travel Request Form</strong> — DEL-LG-FRM-002 Rev 07. Requests are
          verified by your head of department, then approved by management. Once
          approved, Logistics downloads the completed form to book from.
        </div>

        <div className="flex items-center justify-between flex-wrap gap-3">
          <h1 className="text-2xl font-bold text-gray-900">Travel Requests</h1>
          <div className="flex items-center gap-2 flex-wrap">
            <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
              {STATUS_FILTER.map(s => (
                <option key={s} value={s}>{s ? STATUS_LABEL[s] : 'All Statuses'}</option>
              ))}
            </select>
            <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ New Request</button>
          </div>
        </div>

        {showForm && (
          <NewRequestForm
            departments={departments.map(d => d.name)}
            me={me}
            pending={create.isPending}
            onCancel={() => setShowForm(false)}
            onSubmit={data => create.mutate(data)}
          />
        )}

        <div className="space-y-3">
          {requests.map(r => (
            <div key={r.id} className="card p-4">
              <div className="flex items-start justify-between gap-4 flex-wrap">
                <div className="flex-1 min-w-[260px]">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-sm font-bold text-gray-900">{r.formNumber}</span>
                    <span className="text-sm text-gray-700">{r.givenName} {r.surname}</span>
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${STATUS_STYLE[r.status] ?? 'bg-gray-100 text-gray-700'}`}>
                      {STATUS_LABEL[r.status] ?? r.status}
                    </span>
                    {r.hotelBookingRequired && (
                      <span className="px-2 py-0.5 rounded text-xs font-medium bg-purple-100 text-purple-800">
                        Hotel required
                      </span>
                    )}
                  </div>

                  <p className="text-sm text-gray-600 mt-1">{r.purposeOfTravel}</p>

                  <div className="text-xs text-gray-500 mt-1 space-y-0.5">
                    {r.legs.map((l, i) => (
                      <p key={i}>
                        <span className="font-medium">{l.direction}:</span>{' '}
                        {l.from} → {l.to} on {l.travelDate}
                        {l.preferredAirline && ` · ${l.preferredAirline}`}
                        {l.preferredTime && ` · ${l.preferredTime}`}
                      </p>
                    ))}
                  </div>

                  <p className="text-xs text-gray-400 mt-1">
                    {r.department}
                    {r.position && ` · ${r.position}`}
                    {' · raised '}{format(new Date(r.formDate), 'dd MMM yyyy')}
                  </p>

                  {r.verifiedByName && (
                    <p className="text-xs text-green-700 mt-1">
                      Verified by {r.verifiedByName}
                      {r.verifiedAt && ` · ${format(new Date(r.verifiedAt), 'dd MMM yyyy HH:mm')}`}
                    </p>
                  )}
                  {r.approvedByName && (
                    <p className="text-xs text-green-700">
                      Approved by {r.approvedByName}
                      {r.approvedAt && ` · ${format(new Date(r.approvedAt), 'dd MMM yyyy HH:mm')}`}
                    </p>
                  )}
                  {r.rejectionReason && (
                    <p className="text-xs text-red-600 mt-1">Rejected: {r.rejectionReason}</p>
                  )}
                </div>

                <div className="flex items-center gap-2 flex-wrap flex-shrink-0">
                  {canVerify && r.status === 'PendingVerification' && (
                    <>
                      <button
                        className="btn-primary text-xs"
                        onClick={() => verify.mutate({ id: r.id })}
                        disabled={verify.isPending}
                      >
                        Verify
                      </button>
                      <button className="btn-secondary text-xs text-red-600" onClick={() => handleReject(r)}>
                        Reject
                      </button>
                    </>
                  )}

                  {/* Whoever verified a request cannot also approve it — two
                      signatures mean two people. Hidden here as well as blocked
                      server-side, so nobody clicks into a refusal. */}
                  {canApprove && r.status === 'PendingApproval' && r.verifiedByName !== me?.fullName && (
                    <>
                      <button
                        className="btn-primary text-xs"
                        onClick={() => approve.mutate({ id: r.id })}
                        disabled={approve.isPending}
                      >
                        Approve
                      </button>
                      <button className="btn-secondary text-xs text-red-600" onClick={() => handleReject(r)}>
                        Reject
                      </button>
                    </>
                  )}

                  {(r.status === 'PendingVerification' || r.status === 'PendingApproval') && (
                    <button
                      className="btn-secondary text-xs"
                      onClick={() => cancel.mutate(r.id)}
                      disabled={cancel.isPending}
                    >
                      Cancel
                    </button>
                  )}

                  <button className="btn-secondary text-xs" onClick={() => handleDownload(r)}>
                    ⬇ Download Form
                  </button>
                </div>
              </div>
            </div>
          ))}

          {requests.length === 0 && (
            <div className="card p-12 text-center text-gray-400">No travel requests found</div>
          )}
        </div>
      </div>
    </div>
  )
}

// ── New request form ────────────────────────────────────────────────────────

interface NewRequestFormProps {
  departments: string[]
  me?: { fullName: string; email: string; phoneNumber?: string; departmentName?: string; position?: string }
  pending: boolean
  onCancel: () => void
  onSubmit: (data: object) => void
}

/**
 * Laid out in the same order as the paper form so anyone who knows the
 * original can fill this in without rereading it.
 */
function NewRequestForm({ departments, me, pending, onCancel, onSubmit }: NewRequestFormProps) {
  // Split the signed-in user's name for the Surname / Given Name boxes. The
  // guess is editable, since name order varies.
  const nameParts = (me?.fullName ?? '').trim().split(/\s+/)
  const defaultGiven = nameParts.length > 1 ? nameParts.slice(0, -1).join(' ') : (nameParts[0] ?? '')
  const defaultSurname = nameParts.length > 1 ? nameParts[nameParts.length - 1] : ''

  const [outbound, setOutbound] = useState<TravelLeg[]>([emptyLeg('Outbound')])
  const [inbound, setInbound] = useState<TravelLeg[]>([emptyLeg('Inbound')])

  const updateLeg = (
    rows: TravelLeg[],
    setRows: (r: TravelLeg[]) => void,
    index: number,
    field: keyof TravelLeg,
    value: string,
  ) => {
    const next = [...rows]
    next[index] = { ...next[index], [field]: value }
    setRows(next)
  }

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)

    // Only rows the user actually filled in are sent.
    const legs = [...outbound, ...inbound]
      .filter(l => l.travelDate && l.from.trim() && l.to.trim())
      .map(l => ({
        direction: l.direction,
        travelDate: l.travelDate,
        from: l.from.trim(),
        to: l.to.trim(),
        preferredAirline: l.preferredAirline?.trim() || undefined,
        preferredTime: l.preferredTime?.trim() || undefined,
      }))

    if (!legs.some(l => l.direction === 'Outbound')) {
      toast.error('Add at least one outbound journey under Routing Required')
      return
    }

    onSubmit({
      surname: fd.get('surname') as string,
      givenName: fd.get('givenName') as string,
      department: fd.get('department') as string,
      position: (fd.get('position') as string)?.trim() || undefined,
      phoneNumber: (fd.get('phoneNumber') as string)?.trim() || undefined,
      email: (fd.get('email') as string)?.trim() || undefined,
      projectCostCentreCode: (fd.get('projectCostCentreCode') as string)?.trim() || undefined,
      purposeOfTravel: fd.get('purposeOfTravel') as string,
      hotelBookingRequired: fd.get('hotelBookingRequired') === 'Yes',
      otherInformation: (fd.get('otherInformation') as string)?.trim() || undefined,
      legs,
    })
  }

  const legRows = (
    rows: TravelLeg[],
    setRows: (r: TravelLeg[]) => void,
    label: string,
    dateLabel: string,
    timeLabel: string,
  ) => (
    <div className="md:col-span-3">
      <div className="flex items-center justify-between mb-2">
        <h4 className="text-xs font-bold text-gray-700 uppercase">{label}</h4>
        <button
          type="button"
          className="text-xs text-brand-600 hover:underline"
          onClick={() => setRows([...rows, emptyLeg(rows[0].direction)])}
        >
          + Add row
        </button>
      </div>
      <div className="overflow-x-auto">
        <table className="min-w-full text-xs border border-gray-200">
          <thead className="bg-gray-100">
            <tr>
              {[dateLabel, 'FROM', 'TO', 'PREFERRED AIRLINE', timeLabel, ''].map(h => (
                <th key={h} className="px-2 py-2 text-left font-semibold text-gray-600">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row, i) => (
              <tr key={i} className="border-t border-gray-200">
                <td className="p-1">
                  <input type="date" className="input text-xs py-1"
                    value={row.travelDate}
                    onChange={e => updateLeg(rows, setRows, i, 'travelDate', e.target.value)} />
                </td>
                <td className="p-1">
                  <input className="input text-xs py-1" placeholder="e.g. Lagos"
                    value={row.from}
                    onChange={e => updateLeg(rows, setRows, i, 'from', e.target.value)} />
                </td>
                <td className="p-1">
                  <input className="input text-xs py-1" placeholder="e.g. Port Harcourt"
                    value={row.to}
                    onChange={e => updateLeg(rows, setRows, i, 'to', e.target.value)} />
                </td>
                <td className="p-1">
                  <input className="input text-xs py-1" placeholder="Optional"
                    value={row.preferredAirline ?? ''}
                    onChange={e => updateLeg(rows, setRows, i, 'preferredAirline', e.target.value)} />
                </td>
                <td className="p-1">
                  <input className="input text-xs py-1" placeholder="e.g. Morning"
                    value={row.preferredTime ?? ''}
                    onChange={e => updateLeg(rows, setRows, i, 'preferredTime', e.target.value)} />
                </td>
                <td className="p-1 text-right">
                  {rows.length > 1 && (
                    <button type="button" className="text-gray-400 hover:text-red-600 px-1"
                      onClick={() => setRows(rows.filter((_, x) => x !== i))}>✕</button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )

  return (
    <div className="card p-5">
      <h2 className="text-base font-semibold mb-1">New Travel Request</h2>
      <p className="text-xs text-gray-500 mb-4">
        The form number and date are added automatically when you submit. Submitting
        this form stands as your signature.
      </p>

      <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div>
          <label className="label">Project / Cost Center Code</label>
          <input name="projectCostCentreCode" className="input" placeholder="Optional" />
        </div>
        <div>
          <label className="label">Surname <span className="text-red-500">*</span></label>
          <input name="surname" className="input" required defaultValue={defaultSurname} />
        </div>
        <div>
          <label className="label">Given Name <span className="text-red-500">*</span></label>
          <input name="givenName" className="input" required defaultValue={defaultGiven} />
        </div>

        <div>
          <label className="label">Department <span className="text-red-500">*</span></label>
          <select name="department" className="input" required defaultValue={me?.departmentName ?? ''}>
            <option value="">Select department…</option>
            {departments.map(d => <option key={d} value={d}>{d}</option>)}
          </select>
        </div>
        <div>
          <label className="label">Position</label>
          <input name="position" className="input" defaultValue={me?.position ?? ''} />
        </div>
        <div>
          <label className="label">Phone No</label>
          <input name="phoneNumber" className="input" defaultValue={me?.phoneNumber ?? ''} />
        </div>

        <div className="md:col-span-2">
          <label className="label">Email</label>
          <input name="email" type="email" className="input" defaultValue={me?.email ?? ''} />
        </div>
        <div>
          <label className="label">Hotel Booking Required?</label>
          <select name="hotelBookingRequired" className="input" defaultValue="No">
            <option>No</option><option>Yes</option>
          </select>
        </div>

        <div className="md:col-span-3">
          <label className="label">Purpose of Travel <span className="text-red-500">*</span></label>
          <textarea name="purposeOfTravel" className="input" rows={2} required />
        </div>

        <div className="md:col-span-3 pt-2 border-t border-gray-100">
          <h3 className="text-sm font-semibold text-gray-700">Routing Required</h3>
        </div>

        {legRows(outbound, setOutbound, 'Outbound', 'DEPARTURE DATE', 'PREFERRED DEPARTURE TIME')}
        {legRows(inbound, setInbound, 'Inbound', 'RETURN DATE', 'PREFERRED RETURN TIME')}

        <div className="md:col-span-3">
          <label className="label">Any Other Information / Special Requirements</label>
          <textarea name="otherInformation" className="input" rows={2} />
        </div>

        <div className="md:col-span-3 rounded bg-gray-50 border border-gray-200 p-3 text-xs text-gray-600">
          <strong className="text-gray-800">Important notice.</strong> By signing and
          submitting this form, you agree that the information given will be used for
          the purposes stated in this form. Also, kindly notify Logistics Unit of any
          changes in travel date 24hrs prior to departure.
        </div>

        <div className="md:col-span-3 flex gap-3">
          <button type="submit" className="btn-primary" disabled={pending}>
            {pending ? 'Submitting…' : 'Submit Request'}
          </button>
          <button type="button" className="btn-secondary" onClick={onCancel}>Cancel</button>
        </div>
      </form>
    </div>
  )
}
