import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { driversApi, driverIncidentsApi, apiErrorMessage } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'
import { format } from 'date-fns'

const SEVERITY_COLOR: Record<string, string> = {
  Minor: 'bg-yellow-100 text-yellow-800',
  Moderate: 'bg-orange-100 text-orange-800',
  Major: 'bg-red-100 text-red-800',
}

export default function DriverPerformancePage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [selectedDriverId, setSelectedDriverId] = useState<string | null>(null)
  const [showIncidentForm, setShowIncidentForm] = useState(false)

  const { data: drivers = [], isLoading } = useQuery({
    queryKey: ['drivers'],
    queryFn: driversApi.getAll,
  })

  const { data: performance, isLoading: perfLoading } = useQuery({
    queryKey: ['driver-performance', selectedDriverId],
    queryFn: () => driversApi.getPerformance(selectedDriverId!),
    enabled: !!selectedDriverId,
  })

  const logIncident = useMutation({
    mutationFn: driverIncidentsApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['driver-performance', selectedDriverId] })
      setShowIncidentForm(false)
      toast.success('Incident recorded')
    },
    onError: err => toast.error(apiErrorMessage(err, 'Failed to record incident'), { duration: 6000 }),
  })

  const handleIncident = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    logIncident.mutate({
      driverId: selectedDriverId!,
      incidentDate: fd.get('incidentDate') as string,
      type: fd.get('type') as string,
      description: fd.get('description') as string,
      severity: fd.get('severity') as string,
      actionTaken: fd.get('actionTaken') as string || undefined,
    })
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-gray-900">Driver Performance</h1>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Driver list */}
        <div className="card overflow-hidden">
          <div className="px-4 py-3 border-b border-gray-100 bg-gray-50">
            <h2 className="text-sm font-semibold text-gray-700">Select Driver</h2>
          </div>
          <div className="divide-y divide-gray-100">
            {drivers.map(d => (
              <button key={d.id} onClick={() => setSelectedDriverId(d.id)}
                className={`w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-gray-50 transition-colors ${selectedDriverId === d.id ? 'bg-brand-50' : ''}`}>
                <div className="w-8 h-8 rounded-full bg-brand-100 text-brand-700 flex items-center justify-center text-sm font-bold flex-shrink-0">
                  {d.fullName.charAt(0)}
                </div>
                <div className="min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">{d.fullName}</p>
                  <StatusBadge status={d.driverStatus ?? 'Unknown'} />
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Performance panel */}
        <div className="lg:col-span-2 space-y-4">
          {!selectedDriverId && (
            <div className="card p-12 text-center text-gray-400">Select a driver to view their performance</div>
          )}

          {selectedDriverId && perfLoading && <PageLoader />}

          {performance && (
            <>
              {/* KPI cards */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <div className="card p-4 text-center">
                  <p className="text-xs text-gray-500">Total Trips</p>
                  <p className="text-2xl font-bold text-gray-900">{performance.totalTrips}</p>
                </div>
                <div className="card p-4 text-center">
                  <p className="text-xs text-gray-500">Completed</p>
                  <p className="text-2xl font-bold text-green-600">{performance.completedTrips}</p>
                </div>
                <div className="card p-4 text-center">
                  <p className="text-xs text-gray-500">Incidents</p>
                  <p className="text-2xl font-bold text-red-600">{performance.totalIncidents}</p>
                </div>
                <div className="card p-4 text-center">
                  <p className="text-xs text-gray-500">Accident-free</p>
                  <p className="text-2xl font-bold text-blue-600">{performance.accidentFreeStreak}d</p>
                </div>
              </div>

              {/* Recent incidents */}
              <div className="card p-4">
                <div className="flex items-center justify-between mb-3">
                  <h3 className="text-sm font-semibold text-gray-900">Incident History</h3>
                  {hasRole('Coordinator', 'Manager', 'Admin') && (
                    <button onClick={() => setShowIncidentForm(!showIncidentForm)}
                      className="text-xs text-brand-600 hover:underline">+ Log Incident</button>
                  )}
                </div>

                {showIncidentForm && (
                  <form onSubmit={handleIncident} className="grid grid-cols-2 gap-3 mb-4 p-3 bg-gray-50 rounded-lg">
                    <div>
                      <label className="label">Date</label>
                      <input name="incidentDate" type="date" className="input" required defaultValue={new Date().toISOString().split('T')[0]} />
                    </div>
                    <div>
                      <label className="label">Type</label>
                      <select name="type" className="input">
                        <option>Accident</option>
                        <option>TrafficViolation</option>
                        <option>VehicleDamage</option>
                        <option>Other</option>
                      </select>
                    </div>
                    <div>
                      <label className="label">Severity</label>
                      <select name="severity" className="input">
                        <option>Minor</option><option>Moderate</option><option>Major</option>
                      </select>
                    </div>
                    <div>
                      <label className="label">Action Taken</label>
                      <input name="actionTaken" className="input" placeholder="e.g. Warning issued" />
                    </div>
                    <div className="col-span-2">
                      <label className="label">Description</label>
                      <textarea name="description" className="input" rows={2} required />
                    </div>
                    <div className="col-span-2 flex gap-2">
                      <button type="submit" className="btn-primary text-xs" disabled={logIncident.isPending}>Save</button>
                      <button type="button" className="btn-secondary text-xs" onClick={() => setShowIncidentForm(false)}>Cancel</button>
                    </div>
                  </form>
                )}

                {performance.recentIncidents.length === 0 ? (
                  <p className="text-sm text-green-600 py-4 text-center">✓ No incidents recorded — clean record</p>
                ) : (
                  <div className="space-y-2">
                    {performance.recentIncidents.map(i => (
                      <div key={i.id} className="flex items-start justify-between p-3 bg-gray-50 rounded-lg">
                        <div>
                          <p className="text-sm font-medium text-gray-900">{i.type}</p>
                          <p className="text-xs text-gray-500">{i.description}</p>
                          {i.actionTaken && <p className="text-xs text-gray-400">Action: {i.actionTaken}</p>}
                        </div>
                        <div className="text-right ml-4 flex-shrink-0">
                          <span className={`px-2 py-0.5 rounded text-xs font-medium ${SEVERITY_COLOR[i.severity]}`}>
                            {i.severity}
                          </span>
                          <p className="text-xs text-gray-400 mt-1">{format(new Date(i.incidentDate), 'dd MMM yyyy')}</p>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Recent trips */}
              <div className="card p-4">
                <h3 className="text-sm font-semibold text-gray-900 mb-3">Recent Trips</h3>
                {performance.recentTrips.length === 0 ? (
                  <p className="text-sm text-gray-400 py-4 text-center">No trips recorded</p>
                ) : (
                  <div className="space-y-2">
                    {performance.recentTrips.map(t => (
                      <div key={t.id} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg text-sm">
                        <div>
                          <p className="font-medium text-gray-900">{t.tripPurpose}</p>
                          <p className="text-xs text-gray-400">{t.vehicleReg} · {format(new Date(t.startTime), 'dd MMM yyyy')}</p>
                        </div>
                        <StatusBadge status={t.status} />
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
