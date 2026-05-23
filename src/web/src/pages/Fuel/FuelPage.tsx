import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { fuelApi, vehiclesApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import toast from 'react-hot-toast'

export default function FuelPage() {
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)

  const { data: logs = [], isLoading } = useQuery({
    queryKey: ['fuel'],
    queryFn: () => fuelApi.getAll(),
  })

  const { data: vehicles = [] } = useQuery({
    queryKey: ['vehicles'],
    queryFn: () => vehiclesApi.getAll(),
    enabled: showForm,
  })

  const createLog = useMutation({
    mutationFn: fuelApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['fuel'] }); setShowForm(false); toast.success('Fuel log saved') },
    onError: () => toast.error('Failed to save fuel log'),
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    const litres = Number(fd.get('litresFilled'))
    const cost = Number(fd.get('costPerLitre'))
    createLog.mutate({
      vehicleId: fd.get('vehicleId'),
      fuelDate: fd.get('fuelDate'),
      litresFilled: litres,
      costPerLitre: cost,
      odometerAtFill: Number(fd.get('odometerAtFill')),
      stationName: fd.get('stationName') || undefined,
      notes: fd.get('notes') || undefined,
    })
  }

  const totalThisMonth = logs
    .filter(l => new Date(l.fuelDate).getMonth() === new Date().getMonth())
    .reduce((sum, l) => sum + l.totalCost, 0)

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Fuel Logs</h1>
          <p className="text-sm text-gray-500 mt-0.5">This month: R {totalThisMonth.toLocaleString(undefined, { minimumFractionDigits: 2 })}</p>
        </div>
        <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ Log Fuel</button>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">Log Fuel Purchase</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="label">Vehicle</label>
              <select name="vehicleId" className="input" required>
                <option value="">Select vehicle…</option>
                {vehicles.map(v => <option key={v.id} value={v.id}>{v.registrationNo} — {v.make} {v.model}</option>)}
              </select>
            </div>
            <div><label className="label">Date</label><input name="fuelDate" type="date" className="input" defaultValue={new Date().toISOString().split('T')[0]} required /></div>
            <div><label className="label">Litres Filled</label><input name="litresFilled" type="number" step="0.01" className="input" required /></div>
            <div><label className="label">Cost per Litre (R)</label><input name="costPerLitre" type="number" step="0.0001" className="input" required /></div>
            <div><label className="label">Odometer at Fill (km)</label><input name="odometerAtFill" type="number" className="input" required /></div>
            <div><label className="label">Station Name</label><input name="stationName" className="input" /></div>
            <div><label className="label">Notes</label><input name="notes" className="input" /></div>
            <div className="md:col-span-2 flex gap-3">
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
                {['Vehicle', 'Date', 'Station', 'Litres', 'R/Litre', 'Total', 'Odometer', 'Logged By'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {logs.map(l => (
                <tr key={l.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-semibold text-gray-900">{l.vehicleReg}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{l.fuelDate}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{l.stationName ?? '—'}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700">{Number(l.litresFilled).toFixed(1)} L</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700">R {Number(l.costPerLitre).toFixed(4)}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900">R {Number(l.totalCost).toFixed(2)}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{l.odometerAtFill.toLocaleString()} km</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{l.loggedByName}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {logs.length === 0 && <p className="text-center text-gray-400 py-12">No fuel logs</p>}
        </div>
      </div>
    </div>
  )
}
