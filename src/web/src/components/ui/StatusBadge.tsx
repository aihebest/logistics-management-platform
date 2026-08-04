const driverStatusColors: Record<string, string> = {
  Available:    'bg-green-100 text-green-800',
  OnAssignment: 'bg-blue-100 text-blue-800',
  OnBreak:      'bg-yellow-100 text-yellow-800',
  OffDuty:      'bg-gray-100 text-gray-800',
}

const vehicleStatusColors: Record<string, string> = {
  Available:     'bg-green-100 text-green-800',
  Assigned:      'bg-blue-100 text-blue-800',
  InMaintenance: 'bg-orange-100 text-orange-800',
  OutOfService:  'bg-red-100 text-red-800',
}

const tripStatusColors: Record<string, string> = {
  Pending:   'bg-yellow-100 text-yellow-800',
  Approved:  'bg-indigo-100 text-indigo-800',
  Active:    'bg-blue-100 text-blue-800',
  Ongoing:   'bg-blue-100 text-blue-800',
  Unattended:'bg-orange-100 text-orange-800',
  Completed: 'bg-green-100 text-green-800',
  Rejected:  'bg-red-100 text-red-800',
  Cancelled: 'bg-gray-100 text-gray-800',
}

const maintenanceStatusColors: Record<string, string> = {
  Scheduled:  'bg-blue-100 text-blue-800',
  InProgress: 'bg-orange-100 text-orange-800',
  Completed:  'bg-green-100 text-green-800',
  Overdue:    'bg-red-100 text-red-800',
  Cancelled:  'bg-gray-100 text-gray-800',
}

const priorityColors: Record<string, string> = {
  Normal: 'bg-gray-100 text-gray-800',
  High:   'bg-orange-100 text-orange-800',
  Urgent: 'bg-red-100 text-red-800',
}

const all = { ...driverStatusColors, ...vehicleStatusColors, ...tripStatusColors,
               ...maintenanceStatusColors, ...priorityColors }

export function StatusBadge({ status }: { status: string }) {
  const color = all[status] ?? 'bg-gray-100 text-gray-700'
  return <span className={`badge ${color}`}>{status}</span>
}
