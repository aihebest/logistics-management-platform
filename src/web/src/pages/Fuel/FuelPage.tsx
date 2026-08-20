import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { fuelApi, vehiclesApi, locationsApi, apiErrorMessage, type FuelLog } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'

const PRODUCT_TYPES = ['Petrol', 'Diesel']

export default function FuelPage() {
  const qc = useQueryClient()
  const { hasRole } = useAuth()
  // Entry currently open for correction. Fuel figures reconcile against vendor
  // invoices, so edits are restricted and written to the audit trail.
  const [editing, setEditing] = useState<FuelLog | null>(null)
  const canEdit = hasRole('Coordinator', 'Manager', 'Admin')
  const [showForm, setShowForm] = useState(false)
  const [productFilter, setProductFilter] = useState('')
  const [locationFilter, setLocationFilter] = useState('')

  const { data: logs = [], isLoading } = useQuery({
    queryKey: ['fuel', productFilter, locationFilter],
    queryFn: () => fuelApi.getAll({
      productType: productFilter || undefined,
      locationId: locationFilter || undefined,
    }),
  })

  const { data: vehicles = [] } = useQuery({
    queryKey: ['vehicles'],
    queryFn: () => vehiclesApi.getAll(),
  })

  const { data: locations = [] } = useQuery({
    queryKey: ['locations'],
    queryFn: locationsApi.getAll,
  })

  const createLog = useMutation({
    mutationFn: fuelApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['fuel'] })
      setShowForm(false)
      toast.success('Fuel log recorded')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to save fuel log'), { duration: 6000 }),
  })

  const correctLog = useMutation({
    mutationFn: ({ id, data }: { id: string; data: object }) => fuelApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['fuel'] })
      setEditing(null)
      toast.success('Correction saved and recorded in the audit trail')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to save correction'), { duration: 6000 }),
  })

  /** Sends only the fields that were filled, so blanks never wipe existing data. */
  const handleCorrect = (e: React.FormEvent<HTMLFormElement>, id: string) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    const str = (k: string) => { const v = (fd.get(k) as string | null)?.trim(); return v ? v : undefined }
    const num = (k: string) => { const v = (fd.get(k) as string | null)?.trim(); return v ? Number(v) : undefined }
    correctLog.mutate({
      id,
      data: {
        fuelDate: str('fuelDate'),
        productType: str('productType'),
        litresFilled: num('litresFilled'),
        costPerLitre: num('costPerLitre'),
        odometerFrom: num('odometerFrom'),
        odometerTo: num('odometerTo'),
        stationName: str('stationName'),
        costCentre: str('costCentre'),
        isCashPayment: str('isCashPayment') === 'Cash' ? true
                     : str('isCashPayment') === 'Card/Transfer' ? false : undefined,
        notes: str('notes'),
        correctionReason: str('correctionReason'),
      },
    })
  }

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    const odomFrom = fd.get('odometerFrom') ? Number(fd.get('odometerFrom')) : undefined
    const odomTo = fd.get('odometerTo') ? Number(fd.get('odometerTo')) : undefined
    createLog.mutate({
      vehicleId: fd.get('vehicleId') as string,
      fuelDate: fd.get('fuelDate') as string,
      productType: fd.get('productType') as string,
      litresFilled: Number(fd.get('litresFilled')),
      costPerLitre: Number(fd.get('costPerLitre')),
      odometerAtFill: Number(fd.get('odometerAtFill')),
      isCashPayment: fd.get('isCashPayment') === 'on',
      odometerFrom: odomFrom,
      odometerTo: odomTo,
      fuelGaugeBefore: fd.get('fuelGaugeBefore') ? Number(fd.get('fuelGaugeBefore')) : undefined,
      fuelGaugeAfter: fd.get('fuelGaugeAfter') ? Number(fd.get('fuelGaugeAfter')) : undefined,
      stationName: fd.get('stationName') as string || undefined,
      notes: fd.get('notes') as string || undefined,
      locationId: fd.get('locationId') as string || undefined,
    })
  }

  const totalCost = logs.reduce((s, l) => s + l.totalCost, 0)
  const totalLitres = logs.reduce((s, l) => s + l.litresFilled, 0)

  // Per-location summary
  const byLocation = locations.map(loc => ({
    name: loc.name,
    code: loc.code,
    litres: logs.filter(l => l.locationId === loc.id).reduce((s, l) => s + l.litresFilled, 0),
    cost: logs.filter(l => l.locationId === loc.id).reduce((s, l) => s + l.totalCost, 0),
    count: logs.filter(l => l.locationId === loc.id).length,
  })).filter(l => l.count > 0)

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Fuel Logs</h1>
        <div className="flex items-center gap-2 flex-wrap">
          <select value={locationFilter} onChange={e => setLocationFilter(e.target.value)} className="input w-auto">
            <option value="">All Locations</option>
            {locations.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}
          </select>
          <select value={productFilter} onChange={e => setProductFilter(e.target.value)} className="input w-auto">
            <option value="">All Products</option>
            {PRODUCT_TYPES.map(p => <option key={p} value={p}>{p}</option>)}
          </select>
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ Log Fuel</button>
        </div>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="card p-4 text-center">
          <p className="text-xs text-gray-500 uppercase">Total Entries</p>
          <p className="text-2xl font-bold text-gray-900">{logs.length}</p>
        </div>
        <div className="card p-4 text-center">
          <p className="text-xs text-gray-500 uppercase">Total Litres</p>
          <p className="text-2xl font-bold text-blue-700">{totalLitres.toLocaleString()}L</p>
        </div>
        <div className="card p-4 text-center">
          <p className="text-xs text-gray-500 uppercase">Total Cost (₦)</p>
          <p className="text-2xl font-bold text-green-700">₦{totalCost.toLocaleString()}</p>
        </div>
        <div className="card p-4 text-center">
          <p className="text-xs text-gray-500 uppercase">Avg Cost/Litre</p>
          <p className="text-2xl font-bold text-gray-700">
            ₦{logs.length && totalLitres > 0 ? (totalCost / totalLitres).toFixed(0) : '0'}
          </p>
        </div>
      </div>

      {/* Per-location breakdown — shown only when viewing all locations */}
      {!locationFilter && byLocation.length > 1 && (
        <div className="card p-4">
          <h2 className="text-sm font-semibold text-gray-700 mb-3">Consumption by Location</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
            {byLocation.map(loc => (
              <div key={loc.code} className="bg-gray-50 rounded-lg p-3 text-center">
                <p className="text-xs font-bold text-brand-600 uppercase">{loc.code}</p>
                <p className="text-sm font-semibold text-gray-900 mt-1">{loc.litres.toFixed(0)}L</p>
                <p className="text-xs text-gray-500">₦{loc.cost.toLocaleString()}</p>
                <p className="text-xs text-gray-400">{loc.count} entries</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">New Fuel Log Entry</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-4">
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
              <label className="label">Location <span className="text-red-500">*</span></label>
              <select name="locationId" className="input" required>
                <option value="">Select location…</option>
                {locations.map(l => <option key={l.id} value={l.id}>{l.name}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Fuel Date</label>
              <input name="fuelDate" type="date" className="input" required defaultValue={new Date().toISOString().split('T')[0]} />
            </div>
            <div>
              <label className="label">Product Type</label>
              <select name="productType" className="input">
                {PRODUCT_TYPES.map(p => <option key={p}>{p}</option>)}
              </select>
            </div>
            <div><label className="label">Litres Filled</label><input name="litresFilled" type="number" step="0.01" className="input" required /></div>
            <div><label className="label">Cost per Litre (₦)</label><input name="costPerLitre" type="number" step="0.01" className="input" required /></div>
            <div><label className="label">Mileage Before Fuel Purchase (km)</label><input name="odometerAtFill" type="number" className="input" required /></div>
            <div><label className="label">Mileage From (km)</label><input name="odometerFrom" type="number" className="input" placeholder="Previous reading" /></div>
            <div><label className="label">Mileage To (km)</label><input name="odometerTo" type="number" className="input" placeholder="Current reading" /></div>
            <div><label className="label">Fuel Gauge Before (%)</label><input name="fuelGaugeBefore" type="number" min={0} max={100} className="input" /></div>
            <div><label className="label">Fuel Gauge After (%)</label><input name="fuelGaugeAfter" type="number" min={0} max={100} className="input" /></div>
            <div><label className="label">Station Name</label><input name="stationName" className="input" /></div>
            <div className="md:col-span-3 flex items-center gap-2">
              <input type="checkbox" name="isCashPayment" id="cash" className="h-4 w-4" />
              <label htmlFor="cash" className="text-sm text-gray-700">Cash Payment</label>
            </div>
            <div className="md:col-span-3"><label className="label">Notes</label><textarea name="notes" className="input" rows={2} /></div>
            <div className="md:col-span-3 flex gap-3">
              <button type="submit" className="btn-primary" disabled={createLog.isPending}>Save</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {/* ── Correct a fuel entry ─────────────────────────────────────────── */}
      {editing && (
        <div className="card p-5 border-l-4 border-amber-500">
          <div className="flex items-center justify-between mb-1">
            <h2 className="text-base font-semibold text-gray-900">
              Correct Entry — {editing.vehicleReg} · {editing.fuelDate}
            </h2>
            <button onClick={() => setEditing(null)} className="text-gray-400 hover:text-gray-600 text-sm">✕ Close</button>
          </div>
          <p className="text-xs text-gray-500 mb-4">
            Change only what is wrong — leave a field blank to keep its current value.
            The total recalculates from litres × rate. This correction is recorded in
            the audit trail with your name.
          </p>
          <form onSubmit={e => handleCorrect(e, editing.id)} className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div><label className="label">Date</label><input name="fuelDate" type="date" className="input" defaultValue={editing.fuelDate} /></div>
            <div>
              <label className="label">Product</label>
              <select name="productType" className="input" defaultValue={editing.productType}>
                {PRODUCT_TYPES.map(p => <option key={p}>{p}</option>)}
              </select>
            </div>
            <div><label className="label">Litres</label><input name="litresFilled" type="number" step="0.01" min="0" className="input" defaultValue={editing.litresFilled} /></div>
            <div><label className="label">Rate (₦/litre)</label><input name="costPerLitre" type="number" step="0.01" min="0" className="input" defaultValue={editing.costPerLitre} /></div>
            <div><label className="label">Odometer From</label><input name="odometerFrom" type="number" min="0" className="input" defaultValue={editing.odometerFrom ?? ''} /></div>
            <div><label className="label">Odometer To</label><input name="odometerTo" type="number" min="0" className="input" defaultValue={editing.odometerTo ?? ''} /></div>
            <div><label className="label">Station</label><input name="stationName" className="input" defaultValue={editing.stationName ?? ''} /></div>
            <div>
              <label className="label">Payment</label>
              <select name="isCashPayment" className="input" defaultValue={editing.isCashPayment ? 'Cash' : 'Card/Transfer'}>
                <option>Card/Transfer</option><option>Cash</option>
              </select>
            </div>
            <div><label className="label">Cost Centre</label><input name="costCentre" className="input" defaultValue={editing.costCentre ?? ''} /></div>
            <div className="md:col-span-3"><label className="label">Notes</label><input name="notes" className="input" defaultValue={editing.notes ?? ''} /></div>
            <div className="col-span-full">
              <label className="label">Reason for correction <span className="text-red-500">*</span></label>
              <input name="correctionReason" className="input" required placeholder="e.g. Litres mistyped on original entry — corrected against receipt" />
            </div>
            <div className="col-span-full flex gap-3">
              <button type="submit" className="btn-primary" disabled={correctLog.isPending}>
                {correctLog.isPending ? 'Saving…' : 'Save Correction'}
              </button>
              <button type="button" className="btn-secondary" onClick={() => setEditing(null)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Date', 'Vehicle', 'Location', 'Product', 'Litres', 'Rate (₦)', 'Total (₦)', 'Mileage', 'Payment', 'Logged By', ''].map(h => (
                  <th key={h} className="px-3 py-3 text-left text-xs font-medium text-gray-500 uppercase">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {logs.map(l => (
                <tr key={l.id} className="hover:bg-gray-50">
                  <td className="px-3 py-3 text-sm text-gray-700 whitespace-nowrap">{l.fuelDate}</td>
                  <td className="px-3 py-3 text-sm font-medium text-gray-900 whitespace-nowrap">{l.vehicleReg}</td>
                  <td className="px-3 py-3 text-sm">
                    {l.locationName
                      ? <span className="px-2 py-0.5 bg-indigo-50 text-indigo-700 rounded text-xs font-medium">{l.locationName}</span>
                      : <span className="text-gray-400">—</span>}
                  </td>
                  <td className="px-3 py-3 text-sm">
                    <span className="px-2 py-0.5 bg-blue-100 text-blue-800 rounded text-xs font-medium">{l.productType}</span>
                  </td>
                  <td className="px-3 py-3 text-sm text-gray-700">{l.litresFilled.toFixed(1)}L</td>
                  <td className="px-3 py-3 text-sm text-gray-700">₦{l.costPerLitre.toLocaleString()}</td>
                  <td className="px-3 py-3 text-sm font-medium text-gray-900">₦{l.totalCost.toLocaleString()}</td>
                  <td className="px-3 py-3 text-sm text-gray-500">
                    {l.mileageCovered != null ? `${l.mileageCovered.toLocaleString()} km` : l.odometerAtFill ? `${l.odometerAtFill.toLocaleString()} km` : '—'}
                  </td>
                  <td className="px-3 py-3 text-sm">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${l.isCashPayment ? 'bg-yellow-100 text-yellow-800' : 'bg-green-100 text-green-800'}`}>
                      {l.isCashPayment ? 'Cash' : 'Card/Transfer'}
                    </span>
                  </td>
                  <td className="px-3 py-3 text-sm text-gray-500 whitespace-nowrap">{l.loggedByName}</td>
                  <td className="px-3 py-3 text-sm whitespace-nowrap text-right">
                    {canEdit && (
                      <button
                        onClick={() => setEditing(editing?.id === l.id ? null : l)}
                        className="text-xs text-brand-600 hover:underline"
                      >
                        Correct
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {logs.length === 0 && (
                <tr><td colSpan={11} className="px-4 py-12 text-center text-gray-400">No fuel logs found</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
