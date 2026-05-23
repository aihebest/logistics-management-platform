import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { vehiclesApi } from '../../services/api'
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

  const { data: vehicles = [], isLoading } = useQuery({
    queryKey: ['vehicles', statusFilter],
    queryFn: () => vehiclesApi.getAll(statusFilter || undefined),
  })

  const createVehicle = useMutation({
    mutationFn: vehiclesApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['vehicles'] }); setShowForm(false); toast.success('Vehicle added') },
    onError: () => toast.error('Failed to create vehicle'),
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    createVehicle.mutate({
      registrationNo: fd.get('registrationNo') as string,
      make: fd.get('make') as string,
      model: fd.get('model') as string,
      year: Number(fd.get('year')),
      fuelType: fd.get('fuelType') as string,
      odometerKm: Number(fd.get('odometerKm')),
      serviceIntervalKm: Number(fd.get('serviceIntervalKm')) || 10000,
    })
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Vehicles</h1>
        <div className="flex items-center gap-3">
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
            className="input w-auto">
            {STATUS_FILTER.map(s => <option key={s} value={s}>{s || 'All Status'}</option>)}
          </select>
          {hasRole('Manager', 'Admin') && (
            <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
              + Add Vehicle
            </button>
          )}
        </div>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">New Vehicle</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-2 md:grid-cols-3 gap-4">
            <div><label className="label">Registration No</label><input name="registrationNo" className="input" required /></div>
            <div><label className="label">Make</label><input name="make" className="input" required /></div>
            <div><label className="label">Model</label><input name="model" className="input" required /></div>
            <div><label className="label">Year</label><input name="year" type="number" className="input" min={2000} max={2030} required /></div>
            <div>
              <label className="label">Fuel Type</label>
              <select name="fuelType" className="input">
                <option>Diesel</option><option>Petrol</option><option>Electric</option><option>Hybrid</option>
              </select>
            </div>
            <div><label className="label">Odometer (km)</label><input name="odometerKm" type="number" className="input" defaultValue={0} /></div>
            <div><label className="label">Service Interval (km)</label><input name="serviceIntervalKm" type="number" className="input" defaultValue={10000} /></div>
            <div className="col-span-full flex gap-3">
              <button type="submit" className="btn-primary" disabled={createVehicle.isPending}>Save</button>
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
                {['Reg No', 'Make / Model', 'Year', 'Status', 'Fuel', 'Odometer', 'Next Service', 'Mechanic'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {vehicles.map(v => (
                <tr key={v.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-semibold text-gray-900">{v.registrationNo}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700">{v.make} {v.model}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{v.year}</td>
                  <td className="px-4 py-3 whitespace-nowrap"><StatusBadge status={v.status} /></td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{v.fuelType}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{v.odometerKm.toLocaleString()} km</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm">
                    {v.nextServiceDate ? (
                      <span className={new Date(v.nextServiceDate) < new Date() ? 'text-red-600 font-medium' : 'text-gray-500'}>
                        {v.nextServiceDate}
                      </span>
                    ) : '—'}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{v.assignedMechanicName ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
