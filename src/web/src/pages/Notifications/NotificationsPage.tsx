import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notificationsApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import toast from 'react-hot-toast'
import { formatDistanceToNow } from 'date-fns'

export default function NotificationsPage() {
  const qc = useQueryClient()

  const { data: notifications = [], isLoading } = useQuery({
    queryKey: ['notifications'],
    queryFn: notificationsApi.getMine,
  })

  const markRead = useMutation({
    mutationFn: notificationsApi.markRead,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications'] }),
  })

  const markAllRead = useMutation({
    mutationFn: notificationsApi.markAllRead,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['notifications'] }); toast.success('All marked as read') },
  })

  if (isLoading) return <PageLoader />

  const unread = notifications.filter(n => !n.isRead).length

  return (
    <div className="space-y-4 max-w-3xl">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">
          Notifications
          {unread > 0 && <span className="ml-2 badge bg-red-100 text-red-700">{unread} unread</span>}
        </h1>
        {unread > 0 && (
          <button className="btn-secondary text-sm" onClick={() => markAllRead.mutate()}>
            Mark all read
          </button>
        )}
      </div>

      {notifications.length === 0 ? (
        <div className="card p-12 text-center text-gray-400">No notifications</div>
      ) : (
        <div className="space-y-2">
          {notifications.map(n => (
            <div key={n.id}
              className={`card p-4 flex items-start gap-3 cursor-pointer transition-colors ${!n.isRead ? 'border-l-4 border-brand-500' : ''}`}
              onClick={() => !n.isRead && markRead.mutate(n.id)}>
              <div className={`w-2 h-2 rounded-full mt-2 flex-shrink-0 ${n.isRead ? 'bg-gray-300' : 'bg-brand-500'}`} />
              <div className="flex-1 min-w-0">
                <p className={`text-sm ${n.isRead ? 'text-gray-700' : 'text-gray-900 font-medium'}`}>{n.subject}</p>
                <p className="text-xs text-gray-500 mt-0.5 line-clamp-2">{n.body}</p>
                <p className="text-xs text-gray-400 mt-1">
                  {formatDistanceToNow(new Date(n.createdAt), { addSuffix: true })}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
