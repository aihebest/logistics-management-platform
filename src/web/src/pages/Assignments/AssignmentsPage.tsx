import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { assignmentsApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'
import { format } from 'date-fns'
import { useState } from 'react'

const STATUS_FILTER = ['', 'Active', 'Completed', 'Cancelled']

export default function AssignmentsPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('Active')

  const { data: assignments = [], isLoading } = useQuery({
    queryKey: ['assignments', statusFilter],
    queryFn: () => assignmentsApi.getAll({ status: statusFilter || undefined }),
    refetchInterval: 30_000,
  })

  const complete = useMutation({
    mutationFn: assignmentsApi.complete,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['assignments'] }); qc.invalidateQueries({ queryKey: ['drivers'] }); toast.success('Assignment completed') },
    onError: () => toast.error('Failed to complete'),
  })

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Assignments</h1>
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
          {STATUS_FILTER.map(s => <option key={s} value={s}>{s || 'All'}</option>)}
        </select>
      </div>

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Purpose', 'Driver', 'Vehicle', 'Type', 'Status', 'Start', 'Est. End', ''].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {assignments.map(a => (
                <tr key={a.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-sm text-gray-900 max-w-xs truncate">{a.tripPurpose}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700">{a.driverName}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900">{a.vehicleReg}</td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    <span className={`badge ${a.assignmentType === 'Auto' ? 'bg-purple-100 text-purple-800' : 'bg-indigo-100 text-indigo-800'}`}>
                      {a.assignmentType}
                    </span>
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap"><StatusBadge status={a.status} /></td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
                    {format(new Date(a.startTime), 'dd MMM HH:mm')}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
                    {a.estimatedEndTime ? format(new Date(a.estimatedEndTime), 'dd MMM HH:mm') : '—'}
                  </td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    {a.status === 'Active' && hasRole('Coordinator', 'Manager', 'Admin') && (
                      <button className="btn-secondary text-xs" onClick={() => complete.mutate(a.id)}>
                        Complete
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {assignments.length === 0 && (
            <p className="text-center text-gray-400 py-12">No assignments found</p>
          )}
        </div>
      </div>
    </div>
  )
}
