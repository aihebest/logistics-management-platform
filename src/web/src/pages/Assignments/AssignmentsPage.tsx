import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { assignmentsApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'
import { format } from 'date-fns'
import { useState } from 'react'

const STATUS_FILTER = ['', 'Active', 'Ongoing', 'Unattended', 'Completed', 'Cancelled']

export default function AssignmentsPage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('Active')
  const [updatingId, setUpdatingId] = useState<string | null>(null)

  const { data: assignments = [], isLoading } = useQuery({
    queryKey: ['assignments', statusFilter],
    queryFn: () => assignmentsApi.getAll({ status: statusFilter || undefined }),
    refetchInterval: 30_000,
  })

  const updateStatus = useMutation({
    mutationFn: ({ id, status, notes }: { id: string; status: string; notes?: string }) =>
      assignmentsApi.updateStatus(id, { status, notes }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['assignments'] })
      qc.invalidateQueries({ queryKey: ['drivers'] })
      setUpdatingId(null)
      toast.success('Assignment status updated')
    },
    onError: () => toast.error('Failed to update status'),
  })

  if (isLoading) return <PageLoader />

  const isCoord = hasRole('Coordinator', 'Manager', 'Admin')

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Assignments</h1>
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)} className="input w-auto">
          {STATUS_FILTER.map(s => <option key={s} value={s}>{s || 'All Status'}</option>)}
        </select>
      </div>

      {/* Legend */}
      <div className="flex gap-4 text-xs text-gray-500 flex-wrap">
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-blue-500 inline-block" /> Active — trip in progress</span>
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-indigo-500 inline-block" /> Ongoing — extended / overnight</span>
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-yellow-500 inline-block" /> Unattended — vehicle parked, driver away</span>
        <span className="flex items-center gap-1"><span className="w-2 h-2 rounded-full bg-green-500 inline-block" /> Completed</span>
      </div>

      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                {['Purpose', 'Driver', 'Vehicle', 'Type', 'Status', 'Start', 'Est. End', 'Actions'].map(h => (
                  <th key={h} className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {assignments.map(a => (
                <tr key={a.id} className={`hover:bg-gray-50 ${a.status === 'Unattended' ? 'bg-yellow-50' : a.status === 'Ongoing' ? 'bg-indigo-50' : ''}`}>
                  <td className="px-4 py-3 text-sm text-gray-900 max-w-xs truncate">{a.tripPurpose}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-700">{a.driverName}</td>
                  <td className="px-4 py-3 whitespace-nowrap text-sm font-semibold text-gray-900">{a.vehicleReg}</td>
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
                    {isCoord && (a.status === 'Active' || a.status === 'Ongoing' || a.status === 'Unattended') && (
                      updatingId === a.id ? (
                        <div className="flex gap-1">
                          <button className="text-xs px-2 py-1 bg-green-600 text-white rounded"
                            onClick={() => updateStatus.mutate({ id: a.id, status: 'Completed' })}>
                            Complete
                          </button>
                          <button className="text-xs px-2 py-1 bg-indigo-600 text-white rounded"
                            onClick={() => updateStatus.mutate({ id: a.id, status: 'Ongoing' })}>
                            Ongoing
                          </button>
                          <button className="text-xs px-2 py-1 bg-yellow-500 text-white rounded"
                            onClick={() => updateStatus.mutate({ id: a.id, status: 'Unattended' })}>
                            Unattended
                          </button>
                          <button className="text-xs px-2 py-1 bg-gray-200 text-gray-700 rounded"
                            onClick={() => setUpdatingId(null)}>
                            ✕
                          </button>
                        </div>
                      ) : (
                        <button className="btn-secondary text-xs" onClick={() => setUpdatingId(a.id)}>
                          Update Status
                        </button>
                      )
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
