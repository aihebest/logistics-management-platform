import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { tripsApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const STATUS_FILTER = ['', 'Pending', 'Active', 'Completed', 'Cancelled']

export default function TripRequestsPage() {
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [showForm, setShowForm] = useState(false)

  const { data: trips = [], isLoading } = useQuery({
    queryKey: ['trips', statusFilter],
    queryFn: () => tripsApi.getAll(statusFilter || undefined),
  })

  const createTrip = useMutation({
    mutationFn: tripsApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['trips'] }); setShowForm(false); toast.success('Trip request submitted') },
    onError: () => toast.error('Failed to create trip request'),
  })

  const cancelTrip = useMutation({
    mutationFn: tripsApi.cancel,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['trips'] }); toast.success('Trip cancelled') },
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    createTrip.mutate({
      purpose: fd.get('purpose') as string,
      pickupLocation: fd.get('pickupLocation') as string,
      destinationLocation: fd.get('destinationLocation') as string,
      requestedDateTime: fd.get('requestedDateTime') as string,
      priority: fd.get('priority') as string,
      notes: fd.get('notes') as string || undefined,
      status: 'Pending',
    } as Parameters<typeof tripsApi.create>[0])
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
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
            <div className="md:col-span-2"><label className="label">Purpose / Description</label><input name="purpose" className="input" required /></div>
            <div><label className="label">Pickup Location</label><input name="pickupLocation" className="input" required /></div>
            <div><label className="label">Destination</label><input name="destinationLocation" className="input" required /></div>
            <div><label className="label">Date & Time</label><input name="requestedDateTime" type="datetime-local" className="input" required /></div>
            <div>
              <label className="label">Priority</label>
              <select name="priority" className="input"><option>Normal</option><option>High</option><option>Urgent</option></select>
            </div>
            <div className="md:col-span-2"><label className="label">Notes</label><textarea name="notes" className="input" rows={2} /></div>
            <div className="md:col-span-2 flex gap-3">
              <button type="submit" className="btn-primary" disabled={createTrip.isPending}>Submit</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
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
                </div>
                <p className="text-sm text-gray-500 mt-1">
                  {t.pickupLocation} → {t.destinationLocation}
                </p>
                <p className="text-xs text-gray-400 mt-1">
                  Requested by {t.requestedByName} · {format(new Date(t.requestedDateTime), 'PPp')}
                </p>
                {t.assignment && (
                  <p className="text-xs text-blue-600 mt-1">
                    Assigned: {t.assignment.driverName} · {t.assignment.vehicleReg}
                  </p>
                )}
              </div>
              {t.status === 'Pending' || t.status === 'Active' ? (
                <button
                  className="btn-secondary text-xs"
                  onClick={() => cancelTrip.mutate(t.id)}
                  disabled={cancelTrip.isPending}
                >
                  Cancel
                </button>
              ) : null}
            </div>
          </div>
        ))}
        {trips.length === 0 && (
          <div className="card p-12 text-center text-gray-400">No trip requests found</div>
        )}
      </div>
    </div>
  )
}
