import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { fuelApi, vehiclesApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import toast from 'react-hot-toast'

const PRODUCT_TYPES = ['PMS', 'AGO', 'DPK', 'CNG']

export default function FuelPage() {
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [productFilter, setProductFilter] = useState('')

  const { data: logs = [], isLoading } = useQuery({
    queryKey: ['fuel', productFilter],
    queryFn: () => fuelApi.getAll({ productType: productFilter || undefined }),
  })

  const { data: vehicles = [] } = useQuery({
    queryKey: ['vehicles'],
    queryFn: () => vehiclesApi.getAll(),
  })

  const createLog = useMutation({
    mutationFn: fuelApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['fuel'] })
      setShowForm(false)
      toast.success('Fuel log recorded')
    },
    onError: () => toast.error('Failed to save fuel log'),
  })

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
      costCentre: fd.get('costCentre') as string || undefined,
      stationName: fd.get('stationName') as string || undefined,
      notes: fd.get('notes') as string || undefined,
    })
  }

  const totalCost = logs.reduce((s, l) => s + l.totalCost, 0)
  const totalLitres = logs.reduce((s, l) => s + l.litresFilled, 0)

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Fuel Logs</h1>
        <div className="flex items-center gap-2">
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
            ₦{logs.length ? (totalCost / totalLitres).toFixed(0) : '0'}
          </p>
        </div>
      </div>

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
            <div><label className="label">Odometer at Fill (km)</label><input name="odometerAtFill" type="number" className="input" required /></div>
            <div><label className="label">Odometer From (km)</label><input name="odometerFrom" type="number" className="input" placeholder="Previous reading" /></div>
            <div><label className="label">Odometer To (km)</label><input name="odometerTo" type="number" className="input" placeholder="Current reading" /></div>
            <div><label className="label">Fuel Gauge Before (%)</label><input name="fuelGaugeBefore" type="number" min={0} max={100} className="input" /></div>
            <div><label className="label">Fuel Gauge After (%)</label><input name="fuelGaugeAfter" type="number" min={0} max={100} className="input" /></div>
            <div><label className="label">Cost Centre</label><input name="costCentre" className="input" placeholder="e.g. Project Alpha" /></div>
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

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Date', 'Vehicle', 'Product', 'Litres', 'Rate (₦)', 'Total (₦)', 'Odometer', 'Mileage', 'Cost Centre', 'Payment', 'Logged By'].map(h => (
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
                    <span className="px-2 py-0.5 bg-blue-100 text-blue-800 rounded text-xs font-medium">{l.productType}</span>
                  </td>
                  <td className="px-3 py-3 text-sm text-gray-700">{l.litresFilled.toFixed(1)}L</td>
                  <td className="px-3 py-3 text-sm text-gray-700">₦{l.costPerLitre.toLocaleString()}</td>
                  <td className="px-3 py-3 text-sm font-medium text-gray-900">₦{l.totalCost.toLocaleString()}</td>
                  <td className="px-3 py-3 text-sm text-gray-500">{l.odometerAtFill.toLocaleString()} km</td>
                  <td className="px-3 py-3 text-sm text-gray-500">
                    {l.mileageCovered != null ? `${l.mileageCovered.toLocaleString()} km` : '—'}
                  </td>
                  <td className="px-3 py-3 text-sm text-gray-500">{l.costCentre || '—'}</td>
                  <td className="px-3 py-3 text-sm">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${l.isCashPayment ? 'bg-yellow-100 text-yellow-800' : 'bg-green-100 text-green-800'}`}>
                      {l.isCashPayment ? 'Cash' : 'Card/Transfer'}
                    </span>
                  </td>
                  <td className="px-3 py-3 text-sm text-gray-500 whitespace-nowrap">{l.loggedByName}</td>
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
