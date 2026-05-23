import axios from 'axios'
import { msalInstance } from '../main'
import { apiScopes } from '../auth/msalConfig'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

export const api = axios.create({ baseURL: BASE_URL })

// Attach Bearer token to every request
api.interceptors.request.use(async config => {
  const accounts = msalInstance.getAllAccounts()
  if (accounts.length > 0) {
    try {
      const result = await msalInstance.acquireTokenSilent({
        scopes: apiScopes,
        account: accounts[0],
      })
      config.headers.Authorization = `Bearer ${result.accessToken}`
    } catch {
      // Token refresh failed — let the request proceed without auth (will get 401)
    }
  }
  return config
})

// ── Types ─────────────────────────────────────────────────────────────────────

export interface User {
  id: string
  fullName: string
  email: string
  phoneNumber?: string
  role: string
  driverStatus?: string
  licenceNo?: string
  licenceExpiry?: string
  isActive: boolean
  lastStatusChange?: string
}

export interface Vehicle {
  id: string
  registrationNo: string
  make: string
  model: string
  year: number
  status: string
  fuelType: string
  odometerKm: number
  serviceIntervalKm: number
  lastServiceDate?: string
  nextServiceDate?: string
  assignedMechanicName?: string
}

export interface TripRequest {
  id: string
  requestedById: string
  requestedByName: string
  purpose: string
  pickupLocation: string
  destinationLocation: string
  requestedDateTime: string
  status: string
  priority: string
  notes?: string
  createdAt: string
  assignment?: AssignmentSummary
}

export interface AssignmentSummary {
  id: string
  driverName: string
  vehicleReg: string
  status: string
  startTime: string
  estimatedEndTime?: string
}

export interface Assignment {
  id: string
  tripRequestId: string
  tripPurpose: string
  driverId: string
  driverName: string
  vehicleId: string
  vehicleReg: string
  assignmentType: string
  status: string
  startTime: string
  estimatedEndTime?: string
  actualEndTime?: string
  notes?: string
  createdAt: string
}

export interface MaintenanceRecord {
  id: string
  vehicleId: string
  vehicleReg: string
  type: string
  scheduledDate: string
  completedDate?: string
  cost?: number
  vendorName?: string
  vendorContact?: string
  notes?: string
  status: string
  attachmentBlobUrl?: string
  createdAt: string
}

export interface FuelLog {
  id: string
  vehicleId: string
  vehicleReg: string
  loggedByName: string
  fuelDate: string
  litresFilled: number
  costPerLitre: number
  totalCost: number
  odometerAtFill: number
  stationName?: string
  receiptBlobUrl?: string
  notes?: string
  createdAt: string
}

export interface Notification {
  id: string
  type: string
  subject: string
  body: string
  isRead: boolean
  status: string
  sentAt?: string
  relatedEntityType?: string
  relatedEntityId?: string
  createdAt: string
}

export interface DashboardSummary {
  availableDrivers: number
  driversOnAssignment: number
  driversOffDuty: number
  driversOnBreak: number
  availableVehicles: number
  vehiclesAssigned: number
  vehiclesInMaintenance: number
  pendingTripRequests: number
  activeAssignments: number
  overdueMaintenanceCount: number
  upcomingMaintenanceCount: number
}

// ── API functions ─────────────────────────────────────────────────────────────

export const driversApi = {
  getAll: () => api.get<User[]>('/drivers').then(r => r.data),
  get: (id: string) => api.get<User>(`/drivers/${id}`).then(r => r.data),
  updateStatus: (id: string, status: string) =>
    api.patch(`/drivers/${id}/status`, { status }),
  getAssignments: (id: string) =>
    api.get<Assignment[]>(`/drivers/${id}/assignments`).then(r => r.data),
}

export const vehiclesApi = {
  getAll: (status?: string) =>
    api.get<Vehicle[]>('/vehicles', { params: { status } }).then(r => r.data),
  get: (id: string) => api.get<Vehicle>(`/vehicles/${id}`).then(r => r.data),
  create: (data: Partial<Vehicle>) => api.post<Vehicle>('/vehicles', data).then(r => r.data),
  update: (id: string, data: Partial<Vehicle>) => api.patch(`/vehicles/${id}`, data),
}

export const tripsApi = {
  getAll: (status?: string) =>
    api.get<TripRequest[]>('/trips', { params: { status } }).then(r => r.data),
  get: (id: string) => api.get<TripRequest>(`/trips/${id}`).then(r => r.data),
  create: (data: Omit<TripRequest, 'id' | 'requestedById' | 'requestedByName' | 'createdAt' | 'assignment'>) =>
    api.post<TripRequest>('/trips', data).then(r => r.data),
  cancel: (id: string) => api.patch(`/trips/${id}/cancel`),
}

export const assignmentsApi = {
  getAll: (params?: { status?: string; driverId?: string; vehicleId?: string }) =>
    api.get<Assignment[]>('/assignments', { params }).then(r => r.data),
  create: (data: object) => api.post<Assignment>('/assignments', data).then(r => r.data),
  override: (id: string, data: { driverId: string; vehicleId: string; notes?: string }) =>
    api.post(`/assignments/${id}/override`, data),
  complete: (id: string) => api.patch(`/assignments/${id}/complete`),
}

export const maintenanceApi = {
  getAll: (params?: { status?: string; vehicleId?: string }) =>
    api.get<MaintenanceRecord[]>('/maintenance', { params }).then(r => r.data),
  get: (id: string) => api.get<MaintenanceRecord>(`/maintenance/${id}`).then(r => r.data),
  create: (data: object) => api.post<MaintenanceRecord>('/maintenance', data).then(r => r.data),
  update: (id: string, data: object) => api.put(`/maintenance/${id}`, data),
}

export const fuelApi = {
  getAll: (params?: { vehicleId?: string; from?: string; to?: string }) =>
    api.get<FuelLog[]>('/fuel', { params }).then(r => r.data),
  create: (data: object) => api.post<FuelLog>('/fuel', data).then(r => r.data),
}

export const reportsApi = {
  getDashboard: () => api.get<DashboardSummary>('/reports/dashboard').then(r => r.data),
  exportVehicles: (from?: string, to?: string) =>
    api.get('/reports/vehicles/export', { params: { from, to }, responseType: 'blob' }),
  exportDrivers: (from?: string, to?: string) =>
    api.get('/reports/drivers/export', { params: { from, to }, responseType: 'blob' }),
  exportFuel: (from?: string, to?: string) =>
    api.get('/reports/fuel/export', { params: { from, to }, responseType: 'blob' }),
  exportMaintenance: (from?: string, to?: string) =>
    api.get('/reports/maintenance/export', { params: { from, to }, responseType: 'blob' }),
}

export const notificationsApi = {
  getMine: () => api.get<Notification[]>('/notifications').then(r => r.data),
  markRead: (id: string) => api.patch(`/notifications/${id}/read`),
  markAllRead: () => api.patch('/notifications/read-all'),
}

export function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}
