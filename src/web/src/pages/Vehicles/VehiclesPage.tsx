import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { vehiclesApi, maintenanceApi, type Vehicle, apiErrorMessage } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'

const STATUS_FILTER = ['', 'Available', 'Assigned', 'InMaintenance', 'OutOfService']

export default function VehiclesPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [selectedVehicle, setSelectedVehicle] = useState<Vehicle | null>(null)
  // Vehicle currently open in the edit form — used to complete records imported
  // from the asset register that have missing year / fuel type / asset tag.
  const [editVehicle, setEditVehicle] = useState<Vehicle | null>(null)

  const { data: vehicles = [], isLoading } = useQuery({
    queryKey: ['vehicles', statusFilter],
    queryFn: () => vehiclesApi.getAll(statusFilter || undefined),
  })

  const { data: history = [] } = useQuery({
    queryKey: ['maintenance', 'history', selectedVehicle?.id],
    queryFn: () => maintenanceApi.getVehicleHistory(selectedVehicle!.id),
    enabled: !!selectedVehicle,
  })

  const createVehicle = useMutation({
    mutationFn: vehiclesApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['vehicles'] })
      setShowForm(false)
      toast.success('Vehicle added')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to create vehicle'), { duration: 6000 }),
  })

  const updateVehicle = useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => vehiclesApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['vehicles'] })
      setEditVehicle(null)
      toast.success('Vehicle updated')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to update vehicle'), { duration: 6000 }),
  })

  /** Only send fields the user actually filled, so blanks don't overwrite data. */
  const handleEditSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    if (!editVehicle) return
    const fd = new FormData(e.currentTarget)
    const str = (k: string) => {
      const v = (fd.get(k) as string | null)?.trim()
      return v ? v : undefined
    }
    const num = (k: string) => {
      const v = (fd.get(k) as string | null)?.trim()
      return v ? Number(v) : undefined
    }
    updateVehicle.mutate({
      id: editVehicle.id,
      data: {
        registrationNo: str('registrationNo'),
        make: str('make'),
        model: str('model'),
        year: num('year'),
        fuelType: str('fuelType'),
        status: str('status'),
        assetTagNo: str('assetTagNo'),
        chassisNo: str('chassisNo'),
        colour: str('colour'),
        purchaseYear: num('purchaseYear'),
        odometerKm: num('odometerKm'),
        serviceIntervalKm: num('serviceIntervalKm'),
        mileageAtPurchase: num('mileageAtPurchase'),
      },
    })
  }

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    createVehicle.mutate({
      registrationNo: fd.get('registrationNo') as string,
      make: fd.get('make') as string,
      model: fd.get('model') as string,
      year: Number(fd.get('year')),
      fuelType: fd.get('fuelType') as string,
      odometerKm: fd.get('mileageAtPurchase') ? Number(fd.get('mileageAtPurchase')) : 0,
      mileageAtPurchase: fd.get('mileageAtPurchase') ? Number(fd.get('mileageAtPurchase')) : undefined,
      previousMileageAtPurchase: fd.get('previousMileageAtPurchase') ? Number(fd.get('previousMileageAtPurchase')) : undefined,
      serviceIntervalKm: Number(fd.get('serviceIntervalKm')) || 10000,
      chassisNo: fd.get('chassisNo') as string || undefined,
      purchaseYear: fd.get('purchaseYear') ? Number(fd.get('purchaseYear')) : undefined,
      colour: fd.get('colour') as string || undefined,
    })
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Vehicles</h1>
        <div className="flex items-center gap-3">
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
            {STATUS_FILTER.map(s => <option key={s} value={s}>{s || 'All Status'}</option>)}
          </select>
          {hasRole('Manager', 'Admin') && (
            <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ Add Vehicle</button>
          )}
        </div>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">New Vehicle</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <div><label className="label">Registration No</label><input name="registrationNo" className="input" placeholder="e.g. ABJ-123-DE" required /></div>
            <div><label className="label">Make</label><input name="make" className="input" placeholder="e.g. Toyota" required /></div>
            <div><label className="label">Model</label><input name="model" className="input" placeholder="e.g. Land Cruiser" required /></div>
            <div><label className="label">Year (Manufacture)</label><input name="year" type="number" className="input" min={1990} max={2030} required /></div>
            <div>
              <label className="label">Fuel Type</label>
              <select name="fuelType" className="input">
                <option>Diesel (AGO)</option><option>Petrol (PMS)</option><option>Electric</option><option>Hybrid</option>
              </select>
            </div>
            <div><label className="label">Mileage at Purchase (km)</label><input name="mileageAtPurchase" type="number" className="input" placeholder="Odometer when purchased" /></div>
            <div><label className="label">Previous Mileage at Purchase (km)</label><input name="previousMileageAtPurchase" type="number" className="input" placeholder="Prior odometer (2nd-hand vehicles)" /></div>
            <div><label className="label">Service Interval (km)</label><input name="serviceIntervalKm" type="number" className="input" defaultValue={10000} /></div>
            {/* Phase 1 lifecycle fields */}
            <div><label className="label">Chassis No</label><input name="chassisNo" className="input" placeholder="Optional" /></div>
            <div><label className="label">Purchase Year</label><input name="purchaseYear" type="number" className="input" min={1990} max={2030} placeholder="Optional" /></div>
            <div><label className="label">Colour</label><input name="colour" className="input" placeholder="e.g. White" /></div>
            <div className="col-span-full flex gap-3">
              <button type="submit" className="btn-primary" disabled={createVehicle.isPending}>Save Vehicle</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {/* Edit vehicle — complete records imported from the asset register */}
      {editVehicle && (
        <div className="card p-5 border-l-4 border-amber-500">
          <div className="flex items-center justify-between mb-1">
            <h2 className="text-base font-semibold text-gray-900">
              Edit Vehicle — {editVehicle.registrationNo}
            </h2>
            <button onClick={() => setEditVehicle(null)} className="text-gray-400 hover:text-gray-600 text-sm">✕ Close</button>
          </div>
          <p className="text-xs text-gray-500 mb-4">
            Fill in any missing details. Leave a field blank to keep its current value.
          </p>
          <form onSubmit={handleEditSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="label">Registration No</label>
              <input name="registrationNo" className="input" defaultValue={editVehicle.registrationNo} />
            </div>
            <div>
              <label className="label">Asset Tag No</label>
              <input name="assetTagNo" className="input" defaultValue={editVehicle.assetTagNo ?? ''} placeholder="e.g. 5550000190" />
            </div>
            <div>
              <label className="label">Status</label>
              <select name="status" className="input" defaultValue={editVehicle.status}>
                {['Available', 'Assigned', 'InMaintenance', 'OutOfService'].map(s => (
                  <option key={s} value={s}>{s}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="label">Make</label>
              <input name="make" className="input" defaultValue={editVehicle.make} />
            </div>
            <div>
              <label className="label">Model</label>
              <input name="model" className="input" defaultValue={editVehicle.model} />
            </div>
            <div>
              <label className="label">
                Year {editVehicle.year === 0 && <span className="text-amber-600">(not set)</span>}
              </label>
              <input
                name="year" type="number" className="input" min={1980} max={2030}
                defaultValue={editVehicle.year > 0 ? editVehicle.year : ''}
                placeholder="e.g. 2018"
              />
            </div>
            <div>
              <label className="label">Fuel Type</label>
              <select name="fuelType" className="input" defaultValue={editVehicle.fuelType}>
                {['Diesel', 'Petrol', 'CNG', 'Electric', 'Hybrid'].map(f => (
                  <option key={f} value={f}>{f}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="label">Colour</label>
              <input name="colour" className="input" defaultValue={editVehicle.colour ?? ''} placeholder="e.g. White" />
            </div>
            <div>
              <label className="label">Chassis No</label>
              <input name="chassisNo" className="input" defaultValue={editVehicle.chassisNo ?? ''} />
            </div>
            <div>
              <label className="label">Odometer (km)</label>
              <input name="odometerKm" type="number" min={0} className="input" defaultValue={editVehicle.odometerKm} />
            </div>
            <div>
              <label className="label">Service Interval (km)</label>
              <input name="serviceIntervalKm" type="number" min={1000} className="input" defaultValue={editVehicle.serviceIntervalKm} />
            </div>
            <div>
              <label className="label">Purchase Year</label>
              <input
                name="purchaseYear" type="number" className="input" min={1980} max={2030}
                defaultValue={editVehicle.purchaseYear ?? ''} placeholder="Optional"
              />
            </div>
            <div className="col-span-full flex gap-3">
              <button type="submit" className="btn-primary" disabled={updateVehicle.isPending}>
                {updateVehicle.isPending ? 'Saving…' : 'Save Changes'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setEditVehicle(null)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {/* Vehicle detail / repair history panel */}
      {selectedVehicle && (
        <div className="card p-5 border-l-4 border-brand-500">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-semibold text-gray-900">
              {selectedVehicle.registrationNo} — Repair & Maintenance History
            </h2>
            <button onClick={() => setSelectedVehicle(null)} className="text-gray-400 hover:text-gray-600 text-sm">✕ Close</button>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4 text-sm">
            <div><span className="text-gray-500">Make / Model</span><p className="font-medium">{selectedVehicle.make} {selectedVehicle.model}</p></div>
            <div><span className="text-gray-500">Chassis No</span><p className="font-medium">{selectedVehicle.chassisNo || '—'}</p></div>
            <div><span className="text-gray-500">Colour</span><p className="font-medium">{selectedVehicle.colour || '—'}</p></div>
            <div><span className="text-gray-500">Vehicle Age</span><p className="font-medium">{selectedVehicle.vehicleAge} year(s)</p></div>
            {selectedVehicle.mileageAtPurchase != null && (
              <div><span className="text-gray-500">Mileage at Purchase</span><p className="font-medium">{selectedVehicle.mileageAtPurchase.toLocaleString()} km</p></div>
            )}
            {selectedVehicle.previousMileageAtPurchase != null && (
              <div><span className="text-gray-500">Previous Mileage at Purchase</span><p className="font-medium">{selectedVehicle.previousMileageAtPurchase.toLocaleString()} km</p></div>
            )}
          </div>
          {history.length === 0 ? (
            <p className="text-sm text-gray-400 py-4 text-center">No maintenance records</p>
          ) : (
            <div className="space-y-2 max-h-64 overflow-y-auto">
              {history.map(h => (
                <div key={h.id} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg text-sm">
                  <div>
                    <span className={`font-medium ${h.category === 'FaultRepair' ? 'text-red-700' : 'text-gray-900'}`}>
                      {h.category === 'FaultRepair' ? '🔧 ' : '⚙️ '}{h.type}
                    </span>
                    {h.faultDescription && <p className="text-xs text-gray-500 mt-0.5">{h.faultDescription}</p>}
                    {h.partsReplaced && <p className="text-xs text-gray-500">Parts: {h.partsReplaced}</p>}
                  </div>
                  <div className="text-right">
                    <StatusBadge status={h.status} />
                    <p className="text-xs text-gray-400 mt-1">{h.scheduledDate}</p>
                    {h.cost && <p className="text-xs text-gray-500">₦{h.cost.toLocaleString()}</p>}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Reg No', 'Make / Model', 'Year', 'Colour', 'Status', 'Odometer', 'Next Service', 'Age'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{h}</th>
                ))}
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {vehicles.map(v => (
                <tr key={v.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-semibold text-gray-900">{v.registrationNo}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700">{v.make} {v.model}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
                    {v.year > 0 ? v.year : <span className="text-amber-600 italic">not set</span>}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{v.colour || '—'}</td>
                  <td className="px-4 py-3 whitespace-nowrap"><StatusBadge status={v.status} /></td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{v.odometerKm.toLocaleString()} km</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm">
                    {v.nextServiceDate ? (
                      <span className={new Date(v.nextServiceDate) < new Date() ? 'text-red-600 font-medium' : 'text-gray-500'}>
                        {v.nextServiceDate}
                      </span>
                    ) : '—'}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
                    {v.year > 0 ? `${v.vehicleAge}yr` : '—'}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-right space-x-3">
                    {hasRole('Manager', 'Admin', 'Mechanic') && (
                      <button
                        onClick={() => { setEditVehicle(v); setShowForm(false) }}
                        className="text-xs text-brand-600 hover:underline"
                      >
                        Edit
                      </button>
                    )}
                    <button
                      onClick={() => setSelectedVehicle(selectedVehicle?.id === v.id ? null : v)}
                      className="text-xs text-brand-600 hover:underline"
                    >
                      History
                    </button>
                  </td>
                </tr>
              ))}
              {vehicles.length === 0 && (
                <tr><td colSpan={9} className="px-4 py-12 text-center text-gray-400">No vehicles found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
