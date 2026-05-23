import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { reportsApi, fuelApi, maintenanceApi, downloadBlob } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer,
  LineChart, Line, CartesianGrid, PieChart, Pie, Cell,
} from 'recharts'
import toast from 'react-hot-toast'
import { format, subMonths, startOfMonth } from 'date-fns'

const COLORS = ['#3b82f6', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4']

export default function ReportsPage() {
  const [exporting, setExporting] = useState<string | null>(null)

  const { data: summary, isLoading } = useQuery({
    queryKey: ['dashboard'],
    queryFn: reportsApi.getDashboard,
  })

  const { data: fuelLogs = [] } = useQuery({
    queryKey: ['fuel'],
    queryFn: () => fuelApi.getAll(),
  })

  const { data: maintenance = [] } = useQuery({
    queryKey: ['maintenance'],
    queryFn: () => maintenanceApi.getAll(),
  })

  const handleExport = async (type: string) => {
    setExporting(type)
    try {
      const from = subMonths(new Date(), 1).toISOString()
      const to = new Date().toISOString()
      const fn = {
        vehicles: reportsApi.exportVehicles,
        drivers: reportsApi.exportDrivers,
        fuel: reportsApi.exportFuel,
        maintenance: reportsApi.exportMaintenance,
      }[type] as ((f: string, t: string) => Promise<{ data: Blob }>) | undefined

      if (!fn) return
      const res = await fn(from, to)
      downloadBlob(res.data, `${type}-report-${format(new Date(), 'yyyyMMdd')}.xlsx`)
      toast.success(`${type} report downloaded`)
    } catch {
      toast.error('Export failed')
    } finally {
      setExporting(null)
    }
  }

  // Build monthly fuel chart (last 6 months)
  const fuelByMonth = Array.from({ length: 6 }, (_, i) => {
    const d = startOfMonth(subMonths(new Date(), 5 - i))
    const label = format(d, 'MMM')
    const total = fuelLogs
      .filter(l => {
        const ld = new Date(l.fuelDate)
        return ld.getMonth() === d.getMonth() && ld.getFullYear() === d.getFullYear()
      })
      .reduce((sum, l) => sum + l.totalCost, 0)
    return { name: label, cost: Math.round(total) }
  })

  // Maintenance by status
  const maintenanceByStatus = ['Scheduled', 'InProgress', 'Completed', 'Overdue', 'Cancelled'].map(s => ({
    name: s, value: maintenance.filter(m => m.status === s).length,
  })).filter(x => x.value > 0)

  if (isLoading || !summary) return <PageLoader />

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Reports</h1>
      </div>

      {/* Export buttons */}
      <div className="card p-4">
        <h2 className="text-base font-semibold mb-3">Export Reports</h2>
        <div className="flex flex-wrap gap-3">
          {['vehicles', 'drivers', 'fuel', 'maintenance'].map(type => (
            <button key={type} className="btn-secondary capitalize"
              onClick={() => handleExport(type)}
              disabled={exporting === type}>
              {exporting === type ? 'Exporting…' : `${type} Report`}
            </button>
          ))}
        </div>
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">Fuel Spend (Last 6 Months)</h2>
          <ResponsiveContainer width="100%" height={220}>
            <LineChart data={fuelByMonth}>
              <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
              <XAxis dataKey="name" tick={{ fontSize: 12 }} />
              <YAxis tick={{ fontSize: 12 }} tickFormatter={v => `R${(v/1000).toFixed(0)}k`} />
              <Tooltip formatter={(v: number) => [`R ${v.toLocaleString()}`, 'Cost']} />
              <Line type="monotone" dataKey="cost" stroke="#3b82f6" strokeWidth={2} dot={{ r: 3 }} />
            </LineChart>
          </ResponsiveContainer>
        </div>

        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">Maintenance by Status</h2>
          <ResponsiveContainer width="100%" height={220}>
            <PieChart>
              <Pie data={maintenanceByStatus} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={80} label={({ name, value }) => `${name}: ${value}`}>
                {maintenanceByStatus.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </div>

        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">Fleet Status Overview</h2>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={[
              { name: 'Available', value: summary.availableVehicles, fill: '#22c55e' },
              { name: 'Assigned', value: summary.vehiclesAssigned, fill: '#3b82f6' },
              { name: 'Maintenance', value: summary.vehiclesInMaintenance, fill: '#f59e0b' },
            ]} barSize={50}>
              <XAxis dataKey="name" tick={{ fontSize: 12 }} />
              <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
              <Tooltip />
              <Bar dataKey="value" name="Vehicles" fill="#3b82f6" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        <div className="card p-5">
          <h2 className="text-base font-semibold mb-4">Driver Availability</h2>
          <ResponsiveContainer width="100%" height={220}>
            <BarChart data={[
              { name: 'Available', value: summary.availableDrivers, fill: '#22c55e' },
              { name: 'On Trip', value: summary.driversOnAssignment, fill: '#3b82f6' },
              { name: 'On Break', value: summary.driversOnBreak, fill: '#f59e0b' },
              { name: 'Off Duty', value: summary.driversOffDuty, fill: '#9ca3af' },
            ]} barSize={50}>
              <XAxis dataKey="name" tick={{ fontSize: 12 }} />
              <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
              <Tooltip />
              <Bar dataKey="value" name="Drivers" fill="#3b82f6" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  )
}
