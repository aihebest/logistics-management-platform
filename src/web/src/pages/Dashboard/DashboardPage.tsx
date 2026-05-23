import { useQuery } from '@tanstack/react-query'
import { reportsApi, tripsApi, maintenanceApi, assignmentsApi, type DashboardSummary } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts'

function KpiCard({ label, value, sub, color = 'brand' }: {
  label: string; value: number; sub?: string; color?: string
}) {
  const colors: Record<string, string> = {
    brand: 'bg-brand-50 text-brand-700',
    green: 'bg-green-50 text-green-700',
    orange: 'bg-orange-50 text-orange-700',
    red: 'bg-red-50 text-red-700',
    gray: 'bg-gray-50 text-gray-700',
  }
  return (
    <div className="card p-5">
      <p className="text-sm text-gray-500">{label}</p>
      <p className={`text-3xl font-bold mt-1 ${colors[color]?.split(' ')[1] ?? 'text-gray-900'}`}>{value}</p>
      {sub && <p className="text-xs text-gray-400 mt-1">{sub}</p>}
    </div>
  )
}

export default function DashboardPage() {
  const { data: summary, isLoading } = useQuery<DashboardSummary>({
    queryKey: ['dashboard'],
    queryFn: reportsApi.getDashboard,
    refetchInterval: 30_000,
  })

  const { data: pendingTrips = [] } = useQuery({
    queryKey: ['trips', 'Pending'],
    queryFn: () => tripsApi.getAll('Pending'),
  })

  const { data: overdueM = [] } = useQuery({
    queryKey: ['maintenance', 'Overdue'],
    queryFn: () => maintenanceApi.getAll({ status: 'Overdue' }),
  })

  const { data: activeAssignments = [] } = useQuery({
    queryKey: ['assignments', 'Active'],
    queryFn: () => assignmentsApi.getAll({ status: 'Active' }),
    refetchInterval: 30_000,
  })

  if (isLoading || !summary) return <PageLoader />

  const driverData = [
    { name: 'Available',    value: summary.availableDrivers,    fill: '#22c55e' },
    { name: 'On Trip',      value: summary.driversOnAssignment, fill: '#3b82f6' },
    { name: 'On Break',     value: summary.driversOnBreak,      fill: '#f59e0b' },
    { name: 'Off Duty',     value: summary.driversOffDuty,      fill: '#9ca3af' },
  ]

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>

      {/* KPI grid */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <KpiCard label="Available Drivers" value={summary.availableDrivers} color="green" />
        <KpiCard label="Drivers on Trip"   value={summary.driversOnAssignment} color="brand" />
        <KpiCard label="Available Vehicles" value={summary.availableVehicles} color="green" />
        <KpiCard label="Pending Requests"  value={summary.pendingTripRequests} color="orange" />
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <KpiCard label="Active Assignments"  value={summary.activeAssignments} color="brand" />
        <KpiCard label="In Maintenance"      value={summary.vehiclesInMaintenance} color="orange" />
        <KpiCard label="Overdue Maintenance" value={summary.overdueMaintenanceCount} color="red" />
        <KpiCard label="Due in 14 Days"      value={summary.upcomingMaintenanceCount} color="orange" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Driver status chart */}
        <div className="card p-5">
          <h2 className="text-base font-semibold text-gray-900 mb-4">Driver Status</h2>
          <ResponsiveContainer width="100%" height={200}>
            <BarChart data={driverData} barSize={40}>
              <XAxis dataKey="name" tick={{ fontSize: 12 }} />
              <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
              <Tooltip />
              <Bar dataKey="value" name="Drivers">
                {driverData.map((entry, i) => (
                  <rect key={i} fill={entry.fill} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* Pending trips */}
        <div className="card p-5">
          <h2 className="text-base font-semibold text-gray-900 mb-4">
            Pending Trip Requests
            {pendingTrips.length > 0 && (
              <span className="ml-2 badge bg-yellow-100 text-yellow-800">{pendingTrips.length}</span>
            )}
          </h2>
          {pendingTrips.length === 0 ? (
            <p className="text-sm text-gray-400 py-8 text-center">No pending requests</p>
          ) : (
            <div className="space-y-2">
              {pendingTrips.slice(0, 5).map(t => (
                <div key={t.id} className="flex items-start justify-between p-3 bg-gray-50 rounded-lg">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-gray-900 truncate">{t.purpose}</p>
                    <p className="text-xs text-gray-500">{t.pickupLocation} → {t.destinationLocation}</p>
                  </div>
                  <StatusBadge status={t.priority} />
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Today's Active Assignments — Driver ↔ Vehicle */}
      <div className="card p-5">
        <h2 className="text-base font-semibold text-gray-900 mb-4">
          Today's Active Assignments
          {activeAssignments.length > 0 && (
            <span className="ml-2 badge bg-blue-100 text-blue-800">{activeAssignments.length}</span>
          )}
        </h2>
        {activeAssignments.length === 0 ? (
          <p className="text-sm text-gray-400 py-4 text-center">No active assignments right now</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100">
                  <th className="text-left py-2 px-3 text-xs font-medium text-gray-500 uppercase">Driver</th>
                  <th className="text-left py-2 px-3 text-xs font-medium text-gray-500 uppercase">Vehicle</th>
                  <th className="text-left py-2 px-3 text-xs font-medium text-gray-500 uppercase">Trip</th>
                  <th className="text-left py-2 px-3 text-xs font-medium text-gray-500 uppercase">Started</th>
                  <th className="text-left py-2 px-3 text-xs font-medium text-gray-500 uppercase">Est. Return</th>
                  <th className="text-left py-2 px-3 text-xs font-medium text-gray-500 uppercase">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {activeAssignments.map(a => (
                  <tr key={a.id} className="hover:bg-gray-50">
                    <td className="py-3 px-3">
                      <div className="flex items-center gap-2">
                        <div className="w-7 h-7 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center text-xs font-bold flex-shrink-0">
                          {a.driverName.charAt(0)}
                        </div>
                        <span className="font-medium text-gray-900">{a.driverName}</span>
                      </div>
                    </td>
                    <td className="py-3 px-3">
                      <span className="inline-flex items-center gap-1 bg-gray-100 text-gray-700 text-xs font-semibold px-2 py-1 rounded">
                        <svg className="w-3 h-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0M3 7h18M3 7l2-4h14l2 4M3 7v7a2 2 0 002 2h1m12 0h1a2 2 0 002-2V7" />
                        </svg>
                        {a.vehicleReg}
                      </span>
                    </td>
                    <td className="py-3 px-3 text-gray-600 max-w-xs truncate">{a.tripPurpose}</td>
                    <td className="py-3 px-3 text-gray-500">
                      {new Date(a.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </td>
                    <td className="py-3 px-3 text-gray-500">
                      {a.estimatedEndTime
                        ? new Date(a.estimatedEndTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
                        : '—'}
                    </td>
                    <td className="py-3 px-3"><StatusBadge status={a.status} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Overdue maintenance */}
      {overdueM.length > 0 && (
        <div className="card p-5 border-l-4 border-red-500">
          <h2 className="text-base font-semibold text-red-700 mb-3">
            Overdue Maintenance ({overdueM.length})
          </h2>
          <div className="space-y-2">
            {overdueM.map(m => (
              <div key={m.id} className="flex items-center justify-between text-sm">
                <span className="font-medium text-gray-900">{m.vehicleReg}</span>
                <span className="text-gray-500">{m.type}</span>
                <span className="text-red-600">Due {m.scheduledDate}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
