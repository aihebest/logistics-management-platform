import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { driverScheduleApi, driversApi } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { useAuth } from '../../auth/useAuth'
import toast from 'react-hot-toast'
import { format, addDays } from 'date-fns'

const SHIFTS = ['Day', 'Night', 'Off', 'Leave']
const SHIFT_COLOR: Record<string, string> = {
  Day: 'bg-green-100 text-green-800',
  Night: 'bg-indigo-100 text-indigo-800',
  Off: 'bg-gray-100 text-gray-600',
  Leave: 'bg-yellow-100 text-yellow-800',
}

function getMonday(d: Date) {
  const day = d.getDay()
  const diff = d.getDate() - day + (day === 0 ? -6 : 1)
  return new Date(d.setDate(diff))
}

export default function DriverSchedulePage() {
  const { hasRole } = useAuth()
  const qc = useQueryClient()
  const [weekStart, setWeekStart] = useState(() => getMonday(new Date()))
  const [showForm, setShowForm] = useState(false)

  const weekDates = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i))
  const startDateStr = format(weekStart, 'yyyy-MM-dd')

  const { data: schedules = [], isLoading } = useQuery({
    queryKey: ['driver-schedules', startDateStr],
    queryFn: () => driverScheduleApi.getWeek(startDateStr),
  })

  const { data: drivers = [] } = useQuery({
    queryKey: ['drivers'],
    queryFn: driversApi.getAll,
  })

  const createSchedule = useMutation({
    mutationFn: driverScheduleApi.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['driver-schedules'] })
      setShowForm(false)
      toast.success('Schedule saved')
    },
    onError: () => toast.error('Failed to save schedule'),
  })

  const deleteSchedule = useMutation({
    mutationFn: driverScheduleApi.delete,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['driver-schedules'] }); toast.success('Schedule removed') },
  })

  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const fd = new FormData(e.currentTarget)
    createSchedule.mutate({
      driverId: fd.get('driverId') as string,
      scheduleDate: fd.get('scheduleDate') as string,
      location: fd.get('location') as string,
      shift: fd.get('shift') as string,
      notes: fd.get('notes') as string || undefined,
    })
  }

  // Build schedule map: driverId -> date -> schedule
  const scheduleMap = new Map<string, Map<string, typeof schedules[0]>>()
  schedules.forEach(s => {
    if (!scheduleMap.has(s.driverId)) scheduleMap.set(s.driverId, new Map())
    scheduleMap.get(s.driverId)!.set(s.scheduleDate, s)
  })

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Driver Schedule</h1>
        <div className="flex items-center gap-2">
          <button onClick={() => setWeekStart(d => addDays(d, -7))} className="btn-secondary text-xs">← Prev Week</button>
          <span className="text-sm font-medium text-gray-700">
            {format(weekStart, 'dd MMM')} – {format(addDays(weekStart, 6), 'dd MMM yyyy')}
          </span>
          <button onClick={() => setWeekStart(d => addDays(d, 7))} className="btn-secondary text-xs">Next Week →</button>
          {hasRole('Coordinator', 'Manager', 'Admin') && (
            <button className="btn-primary" onClick={() => setShowForm(!showForm)}>+ Add Schedule</button>
          )}
        </div>
      </div>

      {showForm && (
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">Add Driver Schedule</h2>
          <form onSubmit={handleSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="label">Driver</label>
              <select name="driverId" className="input" required>
                <option value="">Select driver…</option>
                {drivers.map(d => <option key={d.id} value={d.id}>{d.fullName}</option>)}
              </select>
            </div>
            <div>
              <label className="label">Date</label>
              <input name="scheduleDate" type="date" className="input" required defaultValue={format(weekStart, 'yyyy-MM-dd')} />
            </div>
            <div>
              <label className="label">Shift</label>
              <select name="shift" className="input">
                {SHIFTS.map(s => <option key={s}>{s}</option>)}
              </select>
            </div>
            <div className="md:col-span-2">
              <label className="label">Location / Assignment</label>
              <input name="location" className="input" required placeholder="e.g. Lagos Office, Port Harcourt Site" />
            </div>
            <div>
              <label className="label">Notes</label>
              <input name="notes" className="input" placeholder="Optional" />
            </div>
            <div className="md:col-span-3 flex gap-3">
              <button type="submit" className="btn-primary" disabled={createSchedule.isPending}>Save</button>
              <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </div>
          </form>
        </div>
      )}

      {/* Weekly grid */}
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase sticky left-0 bg-gray-50 z-10">Driver</th>
                {weekDates.map(d => (
                  <th key={d.toISOString()} className="px-3 py-3 text-center text-xs font-medium text-gray-500 uppercase min-w-[120px]">
                    <div>{format(d, 'EEE')}</div>
                    <div className={`text-lg font-bold ${format(d, 'yyyy-MM-dd') === format(new Date(), 'yyyy-MM-dd') ? 'text-brand-600' : 'text-gray-700'}`}>
                      {format(d, 'd')}
                    </div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {drivers.map(driver => (
                <tr key={driver.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap sticky left-0 bg-white z-10">
                    <div className="flex items-center gap-2">
                      <div className="w-7 h-7 rounded-full bg-brand-100 text-brand-700 flex items-center justify-center text-xs font-bold flex-shrink-0">
                        {driver.fullName.charAt(0)}
                      </div>
                      <span className="text-sm font-medium text-gray-900">{driver.fullName}</span>
                    </div>
                  </td>
                  {weekDates.map(d => {
                    const dateStr = format(d, 'yyyy-MM-dd')
                    const sched = scheduleMap.get(driver.id)?.get(dateStr)
                    return (
                      <td key={dateStr} className="px-3 py-2 text-center">
                        {sched ? (
                          <div className="group relative">
                            <span className={`px-2 py-1 rounded text-xs font-medium ${SHIFT_COLOR[sched.shift] ?? 'bg-gray-100 text-gray-700'}`}>
                              {sched.shift}
                            </span>
                            <p className="text-xs text-gray-400 mt-0.5 truncate max-w-[100px] mx-auto">{sched.location}</p>
                            {hasRole('Coordinator', 'Manager', 'Admin') && (
                              <button onClick={() => deleteSchedule.mutate(sched.id)}
                                className="hidden group-hover:block absolute -top-1 -right-1 w-4 h-4 bg-red-500 text-white rounded-full text-xs leading-4">
                                ✕
                              </button>
                            )}
                          </div>
                        ) : (
                          <span className="text-gray-200 text-xs">—</span>
                        )}
                      </td>
                    )
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
