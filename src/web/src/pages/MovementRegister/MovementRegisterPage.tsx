import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  movementRegisterApi, vehiclesApi, driversApi, apiErrorMessage,
  type MovementRegister,
} from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const MOVEMENT_TYPES = [
  { value: 'StaffTransfer', label: '👤 Staff Transfer' },
  { value: 'Delivery', label: '📦 Delivery' },
  { value: 'Collection', label: '📥 Collection' },
  { value: 'SiteVisit', label: '🏗️ Site Visit' },
  { value: 'AirportTrip', label: '✈️ Airport Trip' },
  { value: 'OfficialErrand', label: '🏢 Official Errand' },
  { value: 'Other', label: '🔖 Other' },
]

export default function MovementRegisterPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [closingId, setClosingId] = useState<string | null>(null)
  // Entry currently open for correction. Register figures feed distance and
  // vendor reconciliation, so edits are restricted and audited.
  const [editing, setEditing] = useState<MovementRegister | null>(null)
  const canEdit = hasRole('Coordinator', 'Manager', 'Admin')

  const { data: movements = [], isLoading } = useQuery({
    queryKey: ['movement-register', statusFilter, typeFilter],
    queryFn: () => movementRegisterApi.getAll({
      status: statusFilter || undefined,
      movementType: typeFilter || undefined,
    }),
    refetchInterval: 30_000,
  })

  const { data: vehicles = [] } = useQuery({ queryKey: ['vehicles'], queryFn: () => vehiclesApi.getAll() })
  const { data: drivers = [] } = useQuery({ queryKey: ['drivers'], queryFn: driversApi.getAll })

  const createMovement = useMutation({
    mutationFn: movementRegisterApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['movement-register'] }); setShowForm(false); toast.success('Movement logged') },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to log movement'), { duration: 6000 }),
  })

  const closeMovement = useMutation({
    mutationFn: ({ id, returnDateTime, mileageIn }: { id: string; returnDateTime: string; mileageIn?: number }) =>
      movementRegisterApi.close(id, returnDateTime, mileageIn),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['movement-register'] }); setClosingId(null); toast.success('Movement closed') },
  })

  const updateMovement = useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => movementRegisterApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['movement-register'] })
      setEditing(null)
      toast.success('Movement record updated')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to update movement record'), { duration: 6000 }),
  })

  /** Only sends the fields that were actually filled in, so blanks don't wipe data. */
  const handleUpdate = (e: React.FormEvent<HTMLFormElement>, id: string) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    const str = (k: string) => { const v = (fd.get(k) as string | null)?.trim(); return v ? v : undefined }
    const num = (k: string) => { const v = (fd.get(k) as string | null)?.trim(); return v ? Number(v) : undefined }
    updateMovement.mutate({
      id,
      data: {
        vehicleId: str('vehicleId'),
        driverId: str('driverId'),
        passengers: str('passengers'),
        purpose: str('purpose'),
        origin: str('origin'),
        destination: str('destination'),
        movementDateTime: str('movementDateTime'),
        returnDateTime: str('returnDateTime'),
        mileageOut: num('mileageOut'),
        mileageIn: num('mileageIn'),
        gatePassNo: str('gatePassNo'),
        status: str('status'),
        correctionReason: str('correctionReason'),
      },
    })
  }

  // Drives the conditional "Specify Type" input when Other is selected.
  const [movementType, setMovementType] = useState(MOVEMENT_TYPES[0].value)

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    const vehicleId = fd.get('vehicleId') as string
    const driverId = fd.get('driverId') as string
    const mileageOutRaw = fd.get('mileageOut') as string
    const mileageInRaw = fd.get('mileageIn') as string
    const returnDtRaw = fd.get('returnDateTime') as string
    createMovement.mutate({
      movementType: fd.get('movementType') as string,
      movementTypeOther: fd.get('movementTypeOther') as string || undefined,
      passengers: fd.get('passengers') as string || undefined,
      vehicleId: vehicleId || undefined,
      driverId: driverId || undefined,
      purpose: fd.get('purpose') as string,
      origin: fd.get('origin') as string,
      destination: fd.get('destination') as string,
      movementDateTime: fd.get('movementDateTime') as string,
      mileageOut: mileageOutRaw ? Number(mileageOutRaw) : undefined,
      mileageIn: mileageInRaw ? Number(mileageInRaw) : undefined,
      returnDateTime: returnDtRaw || undefined,
    })
  }

  const openCount = movements.filter(m => m.status === 'Open').length

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Logistics Movement Register</h1>
          {openCount > 0 && (
            <p className="text-xs text-amber-600 font-medium mt-0.5">⚡ {openCount} open movement(s) pending closure</p>
          )}
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <select value={typeFilter} onChange={e => setTypeFilter(e.target.value)} className="input w-auto">
            <option value="">All Types</option>
            {MOVEMENT_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
          </select>
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
            <option value="">All Status</option>
            <option value="Open">Open</option>
            <option value="Closed">Closed</option>
          </select>
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ Log Movement</button>
        </div>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">Log New Movement</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="label">Movement Type</label>
              <select
                name="movementType"
                className="input"
                value={movementType}
                onChange={e => setMovementType(e.target.value)}
              >
                {MOVEMENT_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
              </select>
            </div>
            {/* Free-text detail — only shown when "Other" is chosen */}
            {movementType === 'Other' ? (
              <div>
                <label className="label">Specify Type <span className="text-red-500">*</span></label>
                <input
                  name="movementTypeOther"
                  className="input"
                  required
                  placeholder="e.g. Expatriate pick up, Courier run"
                />
              </div>
            ) : (
              <div className="hidden md:block" />
            )}
            <div>
              <label className="label">Vehicle (if applicable)</label>
              <select name="vehicleId" className="input">
                <option value="">— None —</option>
                {vehicles.map(v => <option key={v.id} value={v.id}>{v.registrationNo} — {v.make}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Driver (if applicable)</label>
              <select name="driverId" className="input">
                <option value="">— None —</option>
                {drivers.map(d => <option key={d.id} value={d.id}>{d.fullName}</option>)}
              </select>
            </div>
            <div className="md:col-span-3">
              <label className="label">Purpose</label>
              <input name="purpose" className="input" required placeholder="e.g. Material delivery to site, Staff pickup" />
            </div>
            <div><label className="label">Departure Location</label><input name="origin" className="input" required /></div>
            <div><label className="label">Destination</label><input name="destination" className="input" required /></div>
            <div><label className="label">Time Out <span className="text-red-500">*</span></label><input name="movementDateTime" type="datetime-local" className="input" required defaultValue={new Date().toISOString().slice(0, 16)} /></div>
            <div><label className="label">Time In <span className="text-gray-400 font-normal text-xs">(if already returned)</span></label><input name="returnDateTime" type="datetime-local" className="input" /></div>
            <div><label className="label">Mileage Out (km)</label><input name="mileageOut" type="number" className="input" placeholder="Odometer at departure" /></div>
            <div><label className="label">Mileage In (km)</label><input name="mileageIn" type="number" className="input" placeholder="Odometer at return" /></div>
            <div className="md:col-span-3">
              <label className="label">Passenger(s)</label>
              <input
                name="passengers"
                className="input"
                placeholder="Names of people carried — separate with commas"
              />
            </div>
            <div className="md:col-span-3 flex gap-3">
              <button type="submit" className="btn-primary" disabled={createMovement.isPending}>Log Movement</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {/* ── Correct an existing entry ─────────────────────────────────────────
          The register is filled at the gate under time pressure, so corrections
          are a normal part of the workflow rather than an exception. */}
      {editing && (
        <div className="card p-5 border-l-4 border-amber-500">
          <div className="flex items-center justify-between mb-1">
            <h2 className="text-base font-semibold text-gray-900">
              Edit Movement — {editing.vehicleReg || 'no vehicle'} · {format(new Date(editing.movementDateTime), 'dd MMM yyyy HH:mm')}
            </h2>
            <button onClick={() => setEditing(null)} className="text-gray-400 hover:text-gray-600 text-sm">✕ Close</button>
          </div>
          <p className="text-xs text-gray-500 mb-4">
            Change only what is wrong — leave a field as it is to keep its current value.
            Distance recalculates from Mileage In − Mileage Out. This correction is
            recorded in the audit trail with your name.
          </p>
          <form onSubmit={e => handleUpdate(e, editing.id)} className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <label className="label">Vehicle</label>
              <select name="vehicleId" className="input" defaultValue={
                vehicles.find(v => v.registrationNo === editing.vehicleReg)?.id ?? ''
              }>
                <option value="">— unchanged —</option>
                {vehicles.map(v => <option key={v.id} value={v.id}>{v.registrationNo}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Driver</label>
              <select name="driverId" className="input" defaultValue={
                drivers.find(d => d.fullName === editing.driverName)?.id ?? ''
              }>
                <option value="">— unchanged —</option>
                {drivers.map(d => <option key={d.id} value={d.id}>{d.fullName}</option>)}
              </select>
            </div>
            <div className="md:col-span-2">
              <label className="label">Passenger(s)</label>
              <input name="passengers" className="input" defaultValue={editing.passengers ?? ''} />
            </div>
            <div className="md:col-span-2">
              <label className="label">Purpose</label>
              <input name="purpose" className="input" defaultValue={editing.purpose} />
            </div>
            <div><label className="label">From</label><input name="origin" className="input" defaultValue={editing.origin ?? ''} /></div>
            <div><label className="label">To</label><input name="destination" className="input" defaultValue={editing.destination ?? ''} /></div>
            <div>
              <label className="label">Time Out</label>
              <input name="movementDateTime" type="datetime-local" className="input"
                     defaultValue={editing.movementDateTime?.slice(0, 16)} />
            </div>
            <div>
              <label className="label">Mileage Out (km)</label>
              <input name="mileageOut" type="number" min="0" className="input" defaultValue={editing.mileageOut ?? ''} />
            </div>
            <div>
              <label className="label">Time In</label>
              <input name="returnDateTime" type="datetime-local" className="input"
                     defaultValue={editing.returnDateTime?.slice(0, 16) ?? ''} />
            </div>
            <div>
              <label className="label">Mileage In (km)</label>
              <input name="mileageIn" type="number" min="0" className="input" defaultValue={editing.mileageIn ?? ''} />
            </div>
            <div><label className="label">Gate Pass No</label><input name="gatePassNo" className="input" defaultValue={editing.gatePassNo ?? ''} /></div>
            <div>
              <label className="label">Status</label>
              <select name="status" className="input" defaultValue={editing.status}>
                <option>Open</option><option>Closed</option>
              </select>
            </div>
            <div className="col-span-full">
              <label className="label">Reason for correction <span className="text-red-500">*</span></label>
              <input name="correctionReason" className="input" required
                     placeholder="e.g. Mileage In mistyped at the gate — corrected against the vehicle odometer" />
            </div>
            <div className="col-span-full flex gap-3">
              <button type="submit" className="btn-primary" disabled={updateMovement.isPending}>
                {updateMovement.isPending ? 'Saving…' : 'Save Changes'}
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
                {['Vehicle', 'Driver', 'Passenger(s)', 'Purpose', 'From', 'To', 'Time Out', 'Mileage Out', 'Time In', 'Mileage In', 'Distance', 'Status', 'Actions'].map(h => (
                  <th key={h} className="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {movements.map(m => (
                <tr key={m.id} className={`hover:bg-gray-50 ${m.status === 'Open' ? 'bg-amber-50' : ''}`}>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-700 font-medium">{m.vehicleReg || '—'}</td>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.driverName || '—'}</td>
                  <td className="px-3 py-2 max-w-[160px] truncate text-gray-600" title={m.passengers ?? ''}>{m.passengers || '—'}</td>
                  <td className="px-3 py-2 max-w-[180px] truncate text-gray-900" title={m.purpose}>{m.purpose}</td>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.origin}</td>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.destination}</td>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-500">
                    {format(new Date(m.movementDateTime), 'dd MMM HH:mm')}
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-500">
                    {m.mileageOut != null ? `${m.mileageOut.toLocaleString()} km` : '—'}
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-500">
                    {m.returnDateTime ? format(new Date(m.returnDateTime), 'dd MMM HH:mm') : '—'}
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-500">
                    {m.mileageIn != null ? `${m.mileageIn.toLocaleString()} km` : '—'}
                  </td>
                  {/* Auto-calculated: Mileage In − Mileage Out */}
                  <td className="px-3 py-2 whitespace-nowrap font-medium text-gray-900 tabular-nums">
                    {m.distanceKm != null ? `${m.distanceKm.toLocaleString()} km` : '—'}
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${m.status === 'Open' ? 'bg-amber-100 text-amber-800' : 'bg-green-100 text-green-800'}`}>
                      {m.status}
                    </span>
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap">
                    {hasRole('Coordinator', 'Manager', 'Admin') && m.status === 'Open' && (
                      closingId === m.id ? (
                        <div className="flex flex-col gap-1 min-w-[220px]">
                          <input type="datetime-local" id={`return-${m.id}`} className="input text-xs py-1" defaultValue={new Date().toISOString().slice(0, 16)} />
                          <input type="number" id={`mileage-in-${m.id}`} className="input text-xs py-1" placeholder="Mileage In (km)" />
                          <div className="flex gap-1">
                            <button
                              className="text-xs px-2 py-1 bg-green-600 text-white rounded"
                              onClick={() => {
                                const val = (document.getElementById(`return-${m.id}`) as HTMLInputElement).value
                                const mi = (document.getElementById(`mileage-in-${m.id}`) as HTMLInputElement).value
                                closeMovement.mutate({ id: m.id, returnDateTime: val, mileageIn: mi ? Number(mi) : undefined })
                              }}
                            >Save</button>
                            <button className="text-xs text-gray-400" onClick={() => setClosingId(null)}>✕</button>
                          </div>
                        </div>
                      ) : (
                        <button className="btn-secondary text-xs" onClick={() => setClosingId(m.id)}>Close</button>
                      )
                    )}
                    {canEdit && closingId !== m.id && (
                      <button
                        className="text-xs text-brand-600 hover:underline ml-2"
                        onClick={() => { setEditing(editing?.id === m.id ? null : m); setClosingId(null) }}
                      >
                        Edit
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {movements.length === 0 && (
                <tr><td colSpan={13} className="px-4 py-12 text-center text-gray-400">No movement records found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
