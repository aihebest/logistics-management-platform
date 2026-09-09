import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { tripsApi, driversApi, vehiclesApi, apiErrorMessage } from '../../services/api'
import { useAuth } from '../../auth/useAuth'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const STATUS_FILTER = ['', 'Pending', 'Approved', 'Active', 'Ongoing', 'Unattended', 'Completed', 'Rejected', 'Cancelled']
const MOVEMENT_TYPES = ['IntraState', 'Interstate', 'International']

/**
 * Minimum notice per movement type, set by the Director of Logistics.
 * Mirrors MinimumNoticeHours in TripRequestsController — keep the two in step.
 */
const NOTICE_HOURS: Record<string, number> = {
  IntraState: 4,
  Interstate: 24,
  International: 24,
}

/** Staff grades, as set out by the HOD and Director of Logistics. */
const PERSONNEL_CATEGORIES = [
  { value: 'Director',       label: 'Director' },
  { value: 'Manager',        label: 'Manager' },
  { value: 'MidManagement',  label: 'Mid-Management Staff' },
  { value: 'SeniorStaff',    label: 'Senior Staff' },
  { value: 'JuniorStaff',    label: 'Junior Staff' },
]

const MOVEMENT_DURATIONS = ['Half Day', 'Full Day', '2-3 Days', '4-7 Days', 'Over a Week']

/** Turns the stored category value back into the label people recognise. */
function categoryLabel(value: string) {
  return PERSONNEL_CATEGORIES.find(c => c.value === value)?.label ?? value
}

const LOCATIONS = [
  'Desicon Engineering - Head Office, Lagos',
  'Desicon Engineering - Lekki Office',
  'Desicon Engineering - Port Harcourt Office',
  'Desicon Engineering - Abuja Office',
  'NLNG Bonny Island',
  'Chevron Escravos Terminal',
  'TotalEnergies OML 58',
  'Shell SPDC Warri',
  'Federal Airport Authority - MMA Lagos',
  'Port Harcourt International Airport',
  'Nnamdi Azikiwe Int\'l Airport, Abuja',
]

/** Local datetime string ("yyyy-MM-ddTHH:mm") N hours from now, for datetime-local inputs. */
function localDateTimeIn(hours: number) {
  const d = new Date(Date.now() + hours * 60 * 60 * 1000)
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset())
  return d.toISOString().slice(0, 16)
}

/** Local date string ("yyyy-MM-dd") N hours from now, for date inputs. */
function localDateIn(hours: number) {
  return localDateTimeIn(hours).slice(0, 10)
}

export default function TripRequestsPage() {
  const qc = useQueryClient()
  const { hasRole } = useAuth()
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  // Request currently open in the approve panel (choose driver/vehicle or auto-assign)
  const [approvingId, setApprovingId] = useState<string | null>(null)

  const canApprove = hasRole('Coordinator', 'Manager', 'Admin')

  // Only loaded for approvers — drivers/vehicles endpoints are role-restricted.
  const { data: drivers = [] } = useQuery({
    queryKey: ['drivers'],
    queryFn: () => driversApi.getAll(),
    enabled: canApprove,
  })
  const { data: vehicles = [] } = useQuery({
    queryKey: ['vehicles', 'Available'],
    queryFn: () => vehiclesApi.getAll('Available'),
    enabled: canApprove,
  })

  const availableDrivers = drivers.filter(d => d.driverStatus === 'Available')

  // Personnel count drives whether names are required; materials drives the
  // description field. Controlled so the form can react as they're changed.
  const [personnelCount, setPersonnelCount] = useState(1)
  const [hasMaterials, setHasMaterials] = useState(false)
  // Movement type sets how much notice is required, so the date picker has to
  // react to it: intrastate needs 4 hours, interstate/international need 24.
  const [movementType, setMovementType] = useState(MOVEMENT_TYPES[0])

  const noticeHours = NOTICE_HOURS[movementType] ?? 24
  const minDate = localDateIn(noticeHours)
  const defaultDate = localDateIn(noticeHours + 1)

  const { data: trips = [], isLoading } = useQuery({
    queryKey: ['trips', statusFilter],
    queryFn: () => tripsApi.getAll(statusFilter || undefined),
  })

  const createTrip = useMutation({
    mutationFn: tripsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['trips'] })
      setShowForm(false)
      toast.success('Trip request submitted')
    },
    // Surface the API's actual reason (e.g. the 24-hour advance rule) instead
    // of a generic failure message the user can't act on.
    onError: err => toast.error(apiErrorMessage(err, 'Failed to create trip request'), { duration: 6000 }),
  })

  const refreshAll = () => {
    qc.invalidateQueries({ queryKey: ['trips'] })
    qc.invalidateQueries({ queryKey: ['dashboard'] })
    qc.invalidateQueries({ queryKey: ['assignments'] })
    qc.invalidateQueries({ queryKey: ['drivers'] })
    qc.invalidateQueries({ queryKey: ['vehicles'] })
  }

  const approveTrip = useMutation({
    mutationFn: ({ id, driverId, vehicleId }: { id: string; driverId?: string; vehicleId?: string }) =>
      tripsApi.approve(id, { driverId, vehicleId }),
    onSuccess: () => {
      refreshAll()
      setApprovingId(null)
      toast.success('Request approved')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to approve request'), { duration: 7000 }),
  })

  const rejectTrip = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => tripsApi.reject(id, reason),
    onSuccess: () => {
      refreshAll()
      toast.success('Request rejected — requester notified')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to reject request'), { duration: 6000 }),
  })

  const handleApprove = (e: React.FormEvent<HTMLFormElement>, id: string) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    const driverId = (fd.get('driverId') as string) || undefined
    const vehicleId = (fd.get('vehicleId') as string) || undefined
    // Both or neither — a half-filled pair falls back to auto-assignment.
    approveTrip.mutate({
      id,
      driverId: driverId && vehicleId ? driverId : undefined,
      vehicleId: driverId && vehicleId ? vehicleId : undefined,
    })
  }

  const handleReject = (id: string) => {
    const reason = window.prompt('Reason for rejecting this request?')
    if (reason === null) return          // cancelled
    rejectTrip.mutate({ id, reason: reason.trim() || 'No reason provided' })
  }

  const cancelTrip = useMutation({
    mutationFn: tripsApi.cancel,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['trips'] }); toast.success('Trip cancelled') },
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    // The request date is stamped by the server, so it isn't sent from here —
    // that's what stops a request being back- or forward-dated.
    createTrip.mutate({
      purpose: fd.get('purpose') as string,
      pickupLocation: fd.get('pickupLocation') as string,
      destinationLocation: fd.get('destinationLocation') as string,
      priority: fd.get('priority') as string,
      notes: fd.get('notes') as string || undefined,
      movementType: fd.get('movementType') as string,
      departureDate: fd.get('departureDate') as string || undefined,
      departureTime: fd.get('departureTime') as string || undefined,
      personnelCount: Number(fd.get('personnelCount')) || 1,
      personnelNames: (fd.get('personnelNames') as string)?.trim() || undefined,
      personnelCategory: fd.get('personnelCategory') as string || undefined,
      movementDuration: fd.get('movementDuration') as string || undefined,
      isDropOff: fd.get('isDropOff') === 'on',
      hasMaterials: fd.get('hasMaterials') === 'on',
      materialDescription: (fd.get('materialDescription') as string)?.trim() || undefined,
    })
  }

  const resetForm = () => {
    setPersonnelCount(1)
    setHasMaterials(false)
    setMovementType(MOVEMENT_TYPES[0])
    setShowForm(false)
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      {/* SOP Notice */}
      <div className="rounded-lg bg-blue-50 border border-blue-200 px-4 py-3 text-sm text-blue-800">
        <strong>Notice:</strong> Minimum notice before departure — <strong>Intrastate 4 hours</strong>,
        <strong> Interstate and International 24 hours</strong>. Interstate and International movements
        also require manager approval before a driver is assigned. The date and time of the request are
        recorded automatically when you submit.
      </div>

      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Trip Requests</h1>
        <div className="flex items-center gap-3">
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
            {STATUS_FILTER.map(s => <option key={s} value={s}>{s || 'All Status'}</option>)}
          </select>
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ New Request</button>
        </div>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">New Trip Request</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="md:col-span-2">
              <label className="label">Purpose / Description</label>
              <input name="purpose" className="input" required placeholder="e.g. Material delivery to site" />
            </div>
            <div>
              <label className="label">Pickup Location</label>
              <input name="pickupLocation" className="input" list="locations" required placeholder="Start typing or select…" />
              <datalist id="locations">
                {LOCATIONS.map(l => <option key={l} value={l} />)}
              </datalist>
            </div>
            <div>
              <label className="label">Destination</label>
              <input name="destinationLocation" className="input" list="locations" required placeholder="Start typing or select…" />
            </div>
            <div>
              <label className="label">Movement Type</label>
              <select
                name="movementType"
                className="input"
                value={movementType}
                onChange={e => setMovementType(e.target.value)}
              >
                {MOVEMENT_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Priority</label>
              <select name="priority" className="input">
                <option>Normal</option><option>High</option><option>Urgent</option>
              </select>
            </div>
            <div>
              <label className="label">Departure Date</label>
              <input
                name="departureDate"
                type="date"
                className="input"
                // Keyed on the notice window so changing movement type resets the
                // date to one that is actually allowed, rather than leaving a stale
                // value the server will reject.
                key={`departure-${noticeHours}`}
                defaultValue={defaultDate}
                min={minDate}
                required
              />
              <p className="text-xs text-gray-500 mt-1">
                {movementType} movements need at least <strong>{noticeHours} hours</strong> notice.
                For shorter notice, set Priority to Urgent.
              </p>
            </div>
            <div>
              <label className="label">Departure Time</label>
              <input name="departureTime" type="time" className="input" />
            </div>

            {/* ── Personnel travelling ─────────────────────────────────────── */}
            <div className="md:col-span-2 pt-2 border-t border-gray-100">
              <h3 className="text-sm font-semibold text-gray-700">Personnel</h3>
            </div>
            <div>
              <label className="label">Number of Personnel</label>
              <input
                name="personnelCount"
                type="number"
                min={1}
                max={50}
                className="input"
                value={personnelCount}
                onChange={e => setPersonnelCount(Math.max(1, Number(e.target.value) || 1))}
                required
              />
            </div>
            <div>
              <label className="label">Category of Personnel</label>
              <select name="personnelCategory" className="input" defaultValue="">
                <option value="">Select…</option>
                {PERSONNEL_CATEGORIES.map(c => <option key={c.value} value={c.value}>{c.label}</option>)}
              </select>
            </div>
            {personnelCount > 1 && (
              <div className="md:col-span-2">
                <label className="label">
                  Names of Personnel Travelling <span className="text-red-500">*</span>
                </label>
                <textarea
                  name="personnelNames"
                  className="input"
                  rows={2}
                  required
                  placeholder="One name per line, or separated by commas"
                />
                <p className="text-xs text-gray-500 mt-1">
                  Required when more than one person is travelling.
                </p>
              </div>
            )}

            {/* ── Movement detail ──────────────────────────────────────────── */}
            <div>
              <label className="label">Duration of Movement</label>
              <select name="movementDuration" className="input" defaultValue="">
                <option value="">Select…</option>
                {MOVEMENT_DURATIONS.map(d => <option key={d} value={d}>{d}</option>)}
              </select>
            </div>
            <div className="flex items-end pb-2">
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input type="checkbox" name="isDropOff" className="h-4 w-4" />
                Drop Off and Pick Up
              </label>
            </div>

            {/* ── Materials ────────────────────────────────────────────────── */}
            <div className="md:col-span-2">
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input
                  type="checkbox"
                  name="hasMaterials"
                  className="h-4 w-4"
                  checked={hasMaterials}
                  onChange={e => setHasMaterials(e.target.checked)}
                />
                Materials are travelling with this movement
              </label>
            </div>
            {hasMaterials && (
              <div className="md:col-span-2">
                <label className="label">
                  What materials? <span className="text-red-500">*</span>
                </label>
                <input
                  name="materialDescription"
                  className="input"
                  required
                  placeholder="e.g. 4 drums of lubricant, 2 valve assemblies"
                />
              </div>
            )}

            <div className="md:col-span-2">
              <label className="label">Additional Notes</label>
              <textarea name="notes" className="input" rows={2} />
            </div>
            <div className="md:col-span-2 flex gap-3">
              <button type="submit" className="btn-primary" disabled={createTrip.isPending}>Submit Request</button>
              <button type="button" className="btn-secondary" onClick={resetForm}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      <div className="space-y-3">
        {trips.map(t => (
          <div key={t.id} className="card p-4">
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <h3 className="text-sm font-semibold text-gray-900">{t.purpose}</h3>
                  <StatusBadge status={t.status} />
                  <StatusBadge status={t.priority} />
                  <span className="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded font-medium">
                    {t.movementType}
                  </span>
                </div>
                <p className="text-sm text-gray-500 mt-1">
                  {t.pickupLocation} → {t.destinationLocation}
                </p>
                <p className="text-xs text-gray-400 mt-1">
                  Requested by {t.requestedByName} · {format(new Date(t.requestedDateTime), 'PPp')}
                  {t.departureDate && ` · Departs: ${t.departureDate}${t.departureTime ? ` at ${t.departureTime}` : ''}`}
                </p>
                {/* Who and what is travelling — approvers need this before deciding. */}
                <p className="text-xs text-gray-500 mt-1">
                  {t.personnelCount ?? 1} personnel
                  {t.personnelCategory && ` · ${categoryLabel(t.personnelCategory)}`}
                  {t.movementDuration && ` · ${t.movementDuration}`}
                  {t.isDropOff && ' · Drop off and pick up'}
                  {t.hasMaterials && ` · Materials: ${t.materialDescription ?? 'yes'}`}
                </p>
                {t.personnelNames && (
                  <p className="text-xs text-gray-500 mt-0.5">Travelling: {t.personnelNames}</p>
                )}
                {t.assignment && (
                  <p className="text-xs text-blue-600 mt-1">
                    Assigned: {t.assignment.driverName} · {t.assignment.vehicleReg}
                  </p>
                )}
              </div>
              <div className="flex items-center gap-2 flex-shrink-0">
                {/* Approvers act on anything still awaiting a decision */}
                {canApprove && t.status === 'Pending' && (
                  <>
                    <button
                      className="btn-primary text-xs"
                      onClick={() => setApprovingId(approvingId === t.id ? null : t.id)}
                    >
                      Approve
                    </button>
                    <button
                      className="btn-secondary text-xs text-red-600"
                      onClick={() => handleReject(t.id)}
                      disabled={rejectTrip.isPending}
                    >
                      Reject
                    </button>
                  </>
                )}
                {(t.status === 'Pending' || t.status === 'Approved' || t.status === 'Active') && (
                  <button
                    className="btn-secondary text-xs"
                    onClick={() => cancelTrip.mutate(t.id)}
                    disabled={cancelTrip.isPending}
                  >
                    Cancel
                  </button>
                )}
              </div>
            </div>

            {/* ── Approve panel: pick a driver & vehicle, or let the system choose ── */}
            {approvingId === t.id && (
              <form
                onSubmit={e => handleApprove(e, t.id)}
                className="mt-4 pt-4 border-t border-gray-200 grid grid-cols-1 md:grid-cols-3 gap-3"
              >
                <div>
                  <label className="label">Driver</label>
                  <select name="driverId" className="input" defaultValue="">
                    <option value="">Auto-assign (best available)</option>
                    {availableDrivers.map(d => (
                      <option key={d.id} value={d.id}>{d.fullName}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="label">Vehicle</label>
                  <select name="vehicleId" className="input" defaultValue="">
                    <option value="">Auto-assign (best available)</option>
                    {vehicles.map(v => (
                      <option key={v.id} value={v.id}>
                        {v.registrationNo} — {v.make} {v.model}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="flex items-end gap-2">
                  <button type="submit" className="btn-primary text-sm" disabled={approveTrip.isPending}>
                    {approveTrip.isPending ? 'Approving…' : 'Confirm Approval'}
                  </button>
                  <button type="button" className="btn-secondary text-sm" onClick={() => setApprovingId(null)}>
                    Cancel
                  </button>
                </div>
                <p className="md:col-span-3 text-xs text-gray-500 -mt-1">
                  Leave both as auto-assign and the system picks the least-loaded available
                  driver and vehicle. {t.movementType !== 'IntraState' && (
                    <span className="text-amber-600 font-medium">
                      {t.movementType} movements require Manager or Admin approval.
                    </span>
                  )}
                  {availableDrivers.length === 0 && (
                    <span className="text-amber-600 font-medium"> No drivers are currently Available —
                      the request will be approved but stay unassigned.</span>
                  )}
                </p>
              </form>
            )}
          </div>
        ))}
        {trips.length === 0 && (
          <div className="card p-12 text-center text-gray-400">No trip requests found</div>
        )}
      </div>
    </div>
  )
}
