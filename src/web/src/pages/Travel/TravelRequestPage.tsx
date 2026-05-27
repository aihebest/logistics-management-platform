import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { travelApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const TRAVEL_TYPES = ['LocalFlight', 'InternationalFlight', 'Hotel', 'Guesthouse', 'Immigration']
const TYPE_LABEL: Record<string, string> = {
  LocalFlight: '✈ Local Flight', InternationalFlight: '🌍 International Flight',
  Hotel: '🏨 Hotel', Guesthouse: '🏠 Guesthouse', Immigration: '📋 Immigration',
}
const STATUS_FILTER = ['', 'Pending', 'Approved', 'Rejected', 'Booked']

export default function TravelRequestPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [travelTypeFilter, setTravelTypeFilter] = useState('')
  const [showForm, setShowForm] = useState(false)
  const [travelType, setTravelType] = useState('LocalFlight')

  const { data: requests = [], isLoading } = useQuery({
    queryKey: ['travel', statusFilter, travelTypeFilter],
    queryFn: () => travelApi.getAll({ status: statusFilter || undefined, travelType: travelTypeFilter || undefined }),
  })

  const createRequest = useMutation({
    mutationFn: travelApi.create,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['travel'] }); setShowForm(false); toast.success('Travel request submitted') },
    onError: () => toast.error('Failed to submit request'),
  })

  const approveRequest = useMutation({
    mutationFn: ({ id, action }: { id: string; action: string }) => travelApi.approve(id, action),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['travel'] }); toast.success('Decision recorded') },
  })

  const markBooked = useMutation({
    mutationFn: travelApi.markBooked,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['travel'] }); toast.success('Marked as booked') },
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    createRequest.mutate({
      travellerName: fd.get('travellerName') as string,
      travelType: fd.get('travelType') as string,
      purpose: fd.get('purpose') as string,
      origin: fd.get('origin') as string,
      destination: fd.get('destination') as string,
      travelDate: fd.get('travelDate') as string,
      returnDate: fd.get('returnDate') as string || undefined,
      flightPreference: fd.get('flightPreference') as string || undefined,
      hotelName: fd.get('hotelName') as string || undefined,
      numberOfNights: fd.get('numberOfNights') ? Number(fd.get('numberOfNights')) : undefined,
      passportNumber: fd.get('passportNumber') as string || undefined,
    })
  }

  const isFlightType = travelType === 'LocalFlight' || travelType === 'InternationalFlight'
  const isHotelType = travelType === 'Hotel' || travelType === 'Guesthouse'

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Travel / Ticketing / Accommodation</h1>
        <div className="flex items-center gap-2 flex-wrap">
          <select value={travelTypeFilter} onChange={e => setTravelTypeFilter(e.target.value)} className="input w-auto">
            <option value="">All Types</option>
            {TRAVEL_TYPES.map(t => <option key={t} value={t}>{TYPE_LABEL[t]}</option>)}
          </select>
          <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
            {STATUS_FILTER.map(s => <option key={s} value={s}>{s || 'All Status'}</option>)}
          </select>
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ New Request</button>
        </div>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">New Travel / Accommodation Request</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="label">Request Type</label>
              <select name="travelType" className="input" value={travelType} onChange={e => setTravelType(e.target.value)}>
                {TRAVEL_TYPES.map(t => <option key={t} value={t}>{TYPE_LABEL[t]}</option>)}
              </select>
            </div>
            <div><label className="label">Traveller Name</label><input name="travellerName" className="input" required /></div>
            <div className="md:col-span-2"><label className="label">Purpose</label><input name="purpose" className="input" required /></div>
            <div><label className="label">Origin / From</label><input name="origin" className="input" required /></div>
            <div><label className="label">Destination / To</label><input name="destination" className="input" required /></div>
            <div><label className="label">Travel / Check-in Date</label><input name="travelDate" type="date" className="input" required /></div>
            <div><label className="label">Return / Check-out Date</label><input name="returnDate" type="date" className="input" /></div>
            {isFlightType && (
              <div className="md:col-span-2"><label className="label">Flight Preference</label><input name="flightPreference" className="input" placeholder="e.g. Dana Air, Arik Air, early morning" /></div>
            )}
            {isHotelType && (
              <>
                <div><label className="label">Hotel / Guesthouse Name</label><input name="hotelName" className="input" /></div>
                <div><label className="label">Number of Nights</label><input name="numberOfNights" type="number" min={1} className="input" /></div>
              </>
            )}
            {travelType === 'InternationalFlight' && (
              <div><label className="label">Passport Number</label><input name="passportNumber" className="input" /></div>
            )}
            <div className="md:col-span-2 flex gap-3">
              <button type="submit" className="btn-primary" disabled={createRequest.isPending}>Submit</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      <div className="space-y-3">
        {requests.map(r => (
          <div key={r.id} className="card p-4">
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="text-sm font-semibold text-gray-900">{r.travellerName}</span>
                  <span className="text-xs px-2 py-0.5 bg-gray-100 text-gray-700 rounded font-medium">
                    {TYPE_LABEL[r.travelType] ?? r.travelType}
                  </span>
                  <StatusBadge status={r.status} />
                </div>
                <p className="text-sm text-gray-600 mt-1">{r.purpose}</p>
                <p className="text-sm text-gray-500">
                  {r.origin} → {r.destination}
                  {r.travelDate && ` · ${format(new Date(r.travelDate), 'dd MMM yyyy')}`}
                  {r.returnDate && ` – ${format(new Date(r.returnDate), 'dd MMM yyyy')}`}
                </p>
                {r.hotelName && <p className="text-xs text-gray-400 mt-0.5">Hotel: {r.hotelName} · {r.numberOfNights} night(s)</p>}
                {r.flightPreference && <p className="text-xs text-gray-400 mt-0.5">Preference: {r.flightPreference}</p>}
                <p className="text-xs text-gray-400 mt-1">By {r.requestedByName} · {format(new Date(r.createdAt), 'dd MMM yyyy')}</p>
                {r.approvedByName && <p className="text-xs text-gray-500 mt-0.5">{r.status} by {r.approvedByName}</p>}
              </div>
              <div className="flex flex-col gap-2">
                {hasRole('Manager', 'Admin') && r.status === 'Pending' && (
                  <>
                    <button onClick={() => approveRequest.mutate({ id: r.id, action: 'Approve' })}
                      className="text-xs px-3 py-1 bg-green-600 text-white rounded-lg">Approve</button>
                    <button onClick={() => approveRequest.mutate({ id: r.id, action: 'Reject' })}
                      className="text-xs px-3 py-1 bg-red-600 text-white rounded-lg">Reject</button>
                  </>
                )}
                {hasRole('Coordinator', 'Manager', 'Admin') && r.status === 'Approved' && (
                  <button onClick={() => markBooked.mutate(r.id)}
                    className="btn-secondary text-xs">Mark Booked</button>
                )}
              </div>
            </div>
          </div>
        ))}
        {requests.length === 0 && (
          <div className="card p-12 text-center text-gray-400">No travel requests found</div>
        )}
      </div>
    </div>
  )
}
