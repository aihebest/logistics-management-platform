import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { driversApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'

const STATUS_OPTIONS = ['Available', 'OnAssignment', 'OffDuty', 'OnBreak']

export default function DriversPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()

  const { data: drivers = [], isLoading } = useQuery({
    queryKey: ['drivers'],
    queryFn: driversApi.getAll,
    refetchInterval: 30_000,
  })

  const updateStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      driversApi.updateStatus(id, status),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['drivers'] })
      qc.invalidateQueries({ queryKey: ['dashboard'] })
      toast.success('Status updated')
    },
    onError: () => toast.error('Failed to update status'),
  })

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Drivers</h1>
        <span className="text-sm text-gray-500">{drivers.length} drivers</span>
      </div>

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Name', 'Email', 'Phone', 'Status', 'Licence No', 'Licence Expiry', ''].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-100">
              {drivers.map(driver => (
                <tr key={driver.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 bg-brand-100 rounded-full flex items-center justify-center text-brand-700 font-semibold text-sm flex-shrink-0">
                        {driver.fullName.charAt(0)}
                      </div>
                      <span className="text-sm font-medium text-gray-900">{driver.fullName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{driver.email}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{driver.phoneNumber ?? '—'}</td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    <StatusBadge status={driver.driverStatus ?? 'Unknown'} />
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">{driver.licenceNo ?? '—'}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm">
                    {driver.licenceExpiry ? (
                      <span className={
                        new Date(driver.licenceExpiry) < new Date()
                          ? 'text-red-600 font-medium'
                          : 'text-gray-500'
                      }>
                        {driver.licenceExpiry}
                      </span>
                    ) : '—'}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    {hasRole('Coordinator', 'Manager', 'Admin') && (
                      <select
                        value={driver.driverStatus ?? ''}
                        onChange={e => updateStatus.mutate({ id: driver.id, status: e.target.value })}
                        className="text-sm border border-gray-300 rounded px-2 py-1 focus:outline-none focus:ring-1 focus:ring-brand-500"
                      >
                        {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s}</option>)}
                      </select>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
