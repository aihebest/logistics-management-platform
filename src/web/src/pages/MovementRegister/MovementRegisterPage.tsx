import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { movementRegisterApi, vehiclesApi, driversApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const MOVEMENT_TYPES = [
  { value: 'VehicleOut', label: '🚗 Vehicle Out' },
  { value: 'VehicleIn', label: '🏁 Vehicle In' },
  { value: 'MaterialOut', label: '📦 Material Out' },
  { value: 'MaterialIn', label: '📥 Material In' },
  { value: 'GatePass', label: '🎫 Gate Pass' },
  { value: 'StaffMovement', label: '👤 Staff Movement' },
]

export default function MovementRegisterPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [closingId, setClosingId] = useState<string | null>(null)

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
    onError: () => toast.error('Failed to log movement'),
  })

  const closeMovement = useMutation({
    mutationFn: ({ id, returnDateTime, mileageIn }: { id: string; returnDateTime: string; mileageIn?: number }) =>
      movementRegisterApi.close(id, returnDateTime, mileageIn),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['movement-register'] }); setClosingId(null); toast.success('Movement closed') },
  })

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
      vehicleId: vehicleId || undefined,
      driverId: driverId || undefined,
      relatedRefNo: fd.get('relatedRefNo') as string || undefined,
      purpose: fd.get('purpose') as string,
      origin: fd.get('origin') as string,
      destination: fd.get('destination') as string,
      movementDateTime: fd.get('movementDateTime') as string,
      mileageOut: mileageOutRaw ? Number(mileageOutRaw) : undefined,
      mileageIn: mileageInRaw ? Number(mileageInRaw) : undefined,
      returnDateTime: returnDtRaw || undefined,
      gatePassNo: fd.get('gatePassNo') as string || undefined,
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
              <select name="movementType" className="input">
                {MOVEMENT_TYPES.map(t => <option key={t.value} value={t.value}>{t.label}</option>)}
              </select>
            </div>
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
            <div><label className="label">Gate Pass No</label><input name="gatePassNo" className="input" /></div>
            <div><label className="label">Related Ref No</label><input name="relatedRefNo" className="input" placeholder="Trip / Form no" /></div>
            <div className="md:col-span-3 flex gap-3">
              <button type="submit" className="btn-primary" disabled={createMovement.isPending}>Log Movement</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 text-sm">
            <thead className="bg-gray-50">
              <tr>
                {['Type', 'Vehicle', 'Driver', 'Purpose', 'From', 'To', 'Time Out', 'Mileage Out', 'Time In', 'Mileage In', 'Status', 'Actions'].map(h => (
                  <th key={h} className="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {movements.map(m => (
                <tr key={m.id} className={`hover:bg-gray-50 ${m.status === 'Open' ? 'bg-amber-50' : ''}`}>
                  <td className="px-3 py-2 whitespace-nowrap">
                    <span className="text-xs font-medium text-gray-700">
                      {MOVEMENT_TYPES.find(t => t.value === m.movementType)?.label ?? m.movementType}
                    </span>
                  </td>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-700 font-medium">{m.vehicleReg || '—'}</td>
                  <td className="px-3 py-2 whitespace-nowrap text-gray-600">{m.driverName || '—'}</td>
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
                  </td>
                </tr>
              ))}
              {movements.length === 0 && (
                <tr><td colSpan={12} className="px-4 py-12 text-center text-gray-400">No movement records found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
