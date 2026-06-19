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

// ── Core Types ────────────────────────────────────────────────────────────────

export interface Location {
  id: string
  name: string
  code: string
  isActive: boolean
}

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
  mileageAtPurchase?: number
  previousMileageAtPurchase?: number
  serviceIntervalKm: number
  lastServiceDate?: string
  nextServiceDate?: string
  assignedMechanicName?: string
  // Phase 1
  chassisNo?: string
  purchaseYear?: number
  colour?: string
  vehicleAge: number
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
  // Phase 1
  movementType: string
  departureDate?: string
  departureTime?: string
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
  category: string            // Routine | FaultRepair
  scheduledDate: string
  completedDate?: string
  cost?: number
  vendorName?: string
  vendorContact?: string
  notes?: string
  status: string
  attachmentBlobUrl?: string
  faultReported: boolean
  faultDescription?: string
  dateReported?: string
  partsReplaced?: string
  repairRemarks?: string
  createdAt: string
}

export interface FuelLog {
  id: string
  vehicleId: string
  vehicleReg: string
  loggedByName: string
  fuelDate: string
  productType: string         // PMS | AGO | DPK | CNG
  litresFilled: number
  costPerLitre: number
  totalCost: number
  isCashPayment: boolean
  odometerAtFill: number
  odometerFrom?: number
  odometerTo?: number
  mileageCovered?: number
  fuelGaugeBefore?: number
  fuelGaugeAfter?: number
  costCentre?: string
  stationName?: string
  receiptBlobUrl?: string
  notes?: string
  locationId?: string
  locationName?: string
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

// ── Phase 2 Types ─────────────────────────────────────────────────────────────

export interface MaterialTransportItem {
  id: string
  sNo: number
  material: string
  description?: string
  quantity: number
}

export interface MaterialTransportRequest {
  id: string
  formNumber: string
  requestedByName: string
  projectName: string
  purpose: string
  loadingPoint: string
  loadingContactPerson?: string
  loadingContactPhone?: string
  loadingDate?: string
  deliveryPoint: string
  deliveryContactPerson?: string
  deliveryContactPhone?: string
  deliveryDate?: string
  status: string
  hodApprovedByName?: string
  hodApprovedAt?: string
  hodRemarks?: string
  managerApprovedByName?: string
  managerApprovedAt?: string
  managerRemarks?: string
  assignedDriverName?: string
  assignedVehicleReg?: string
  items: MaterialTransportItem[]
  createdAt: string
}

export interface DriverSchedule {
  id: string
  driverId: string
  driverName: string
  scheduleDate: string
  workLocation: string
  locationId?: string
  locationName?: string
  shift: string
  notes?: string
  createdByName: string
  createdAt: string
}

export interface DriverIncident {
  id: string
  driverId: string
  driverName: string
  incidentDate: string
  type: string                // Accident | TrafficViolation | VehicleDamage | Other
  description: string
  severity: string            // Minor | Moderate | Major
  actionTaken?: string
  reportedByName: string
  createdAt: string
}

export interface DriverPerformance {
  driverId: string
  driverName: string
  currentStatus: string
  totalTrips: number
  completedTrips: number
  cancelledTrips: number
  totalIncidents: number
  majorIncidents: number
  accidentFreeStreak: number
  recentTrips: Assignment[]
  recentIncidents: DriverIncident[]
}

// ── Phase 3 Types ─────────────────────────────────────────────────────────────

export interface TravelRequest {
  id: string
  requestedByName: string
  travellerName: string
  travelType: string          // LocalFlight | InternationalFlight | Hotel | Guesthouse | Immigration
  purpose: string
  origin: string
  destination: string
  travelDate: string
  returnDate?: string
  flightPreference?: string
  hotelName?: string
  numberOfNights?: number
  passportNumber?: string
  status: string
  approvedByName?: string
  approvedAt?: string
  approvalNotes?: string
  createdAt: string
}

export interface ProjectMaterialTracking {
  id: string
  trackingYear: number
  poNumber?: string
  poLineItem?: string
  project?: string
  buyer?: string
  description: string
  quantity?: number
  supplier?: string
  freightForwarder?: string
  readinessDate?: string
  pickupAuthDate?: string
  pickupDate?: string
  modeOfTransport?: string
  formMNumber?: string
  blAwbNumber?: string
  vesselName?: string
  etd?: string
  eta?: string
  deliveryStatus: string
  actualDeliveryDate?: string
  remarks?: string
  updatedAt: string
}

export interface MovementRegister {
  id: string
  movementType: string        // VehicleOut | VehicleIn | MaterialOut | MaterialIn | GatePass | StaffMovement
  vehicleReg?: string
  driverName?: string
  relatedRefNo?: string
  purpose: string
  origin: string              // Departure location
  destination: string
  movementDateTime: string    // Time Out
  returnDateTime?: string     // Time In
  mileageOut?: number
  mileageIn?: number
  gatePassNo?: string
  status: string              // Open | Closed
  loggedByName: string
  createdAt: string
}

// ── API functions ─────────────────────────────────────────────────────────────

export const driversApi = {
  getAll: () => api.get<User[]>('/drivers').then(r => r.data),
  get: (id: string) => api.get<User>(`/drivers/${id}`).then(r => r.data),
  register: (data: {
    fullName: string
    email: string
    phoneNumber?: string
    licenceNo?: string
    licenceExpiry?: string
  }) => api.post<User>('/drivers', data).then(r => r.data),
  updateStatus: (id: string, status: string) =>
    api.patch(`/drivers/${id}/status`, { status }),
  getAssignments: (id: string) =>
    api.get<Assignment[]>(`/drivers/${id}/assignments`).then(r => r.data),
  getPerformance: (id: string) =>
    api.get<DriverPerformance>(`/drivers/${id}/performance`).then(r => r.data),
}

export const authApi = {
  /** Called once per login session to link/create the platform User record. */
  me: () => api.get<User>('/auth/me').then(r => r.data),
}

export const vehiclesApi = {
  getAll: (status?: string) =>
    api.get<Vehicle[]>('/vehicles', { params: { status } }).then(r => r.data),
  get: (id: string) => api.get<Vehicle>(`/vehicles/${id}`).then(r => r.data),
  create: (data: object) => api.post<Vehicle>('/vehicles', data).then(r => r.data),
  update: (id: string, data: object) => api.patch(`/vehicles/${id}`, data),
  getMaintenanceHistory: (id: string) =>
    api.get<MaintenanceRecord[]>(`/maintenance/vehicle/${id}/history`).then(r => r.data),
}

export const tripsApi = {
  getAll: (status?: string, movementType?: string) =>
    api.get<TripRequest[]>('/trips', { params: { status, movementType } }).then(r => r.data),
  get: (id: string) => api.get<TripRequest>(`/trips/${id}`).then(r => r.data),
  create: (data: object) => api.post<TripRequest>('/trips', data).then(r => r.data),
  cancel: (id: string) => api.patch(`/trips/${id}/cancel`),
}

export const assignmentsApi = {
  getAll: (params?: { status?: string; driverId?: string; vehicleId?: string }) =>
    api.get<Assignment[]>('/assignments', { params }).then(r => r.data),
  create: (data: object) => api.post<Assignment>('/assignments', data).then(r => r.data),
  override: (id: string, data: { driverId: string; vehicleId: string; notes?: string }) =>
    api.post(`/assignments/${id}/override`, data),
  updateStatus: (id: string, data: { status: string; notes?: string; actualEndTime?: string }) =>
    api.patch(`/assignments/${id}/status`, data),
  complete: (id: string) => api.patch(`/assignments/${id}/complete`),
}

export const maintenanceApi = {
  getAll: (params?: { status?: string; vehicleId?: string; category?: string }) =>
    api.get<MaintenanceRecord[]>('/maintenance', { params }).then(r => r.data),
  get: (id: string) => api.get<MaintenanceRecord>(`/maintenance/${id}`).then(r => r.data),
  create: (data: object) => api.post<MaintenanceRecord>('/maintenance', data).then(r => r.data),
  update: (id: string, data: object) => api.put(`/maintenance/${id}`, data),
  getVehicleHistory: (vehicleId: string) =>
    api.get<MaintenanceRecord[]>(`/maintenance/vehicle/${vehicleId}/history`).then(r => r.data),
}

export const fuelApi = {
  getAll: (params?: { vehicleId?: string; from?: string; to?: string; productType?: string; locationId?: string }) =>
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
  broadcast: (payload: { title: string; message: string; type: string }) =>
    api.post('/notifications/broadcast', payload).then(r => r.data),
}

// Phase 2
export const materialTransportApi = {
  getAll: (params?: { status?: string; year?: number }) =>
    api.get<MaterialTransportRequest[]>('/material-transport', { params }).then(r => r.data),
  get: (id: string) => api.get<MaterialTransportRequest>(`/material-transport/${id}`).then(r => r.data),
  create: (data: object) =>
    api.post<MaterialTransportRequest>('/material-transport', data).then(r => r.data),
  hodApproval: (id: string, action: string, remarks?: string) =>
    api.post(`/material-transport/${id}/hod-approval`, { action, remarks }),
  managerApproval: (id: string, action: string, remarks?: string) =>
    api.post(`/material-transport/${id}/manager-approval`, { action, remarks }),
  assign: (id: string, driverId: string, vehicleId: string) =>
    api.post(`/material-transport/${id}/assign`, { driverId, vehicleId }),
}

export const locationsApi = {
  getAll: () => api.get<Location[]>('/locations').then(r => r.data),
}

export const driverScheduleApi = {
  getAll: (params?: { driverId?: string; from?: string; to?: string }) =>
    api.get<DriverSchedule[]>('/driver-schedules', { params }).then(r => r.data),
  getWeek: (startDate?: string) =>
    api.get<DriverSchedule[]>('/driver-schedules/week', { params: { startDate } }).then(r => r.data),
  create: (data: object) => api.post<DriverSchedule>('/driver-schedules', data).then(r => r.data),
  delete: (id: string) => api.delete(`/driver-schedules/${id}`),
}

export const driverIncidentsApi = {
  getAll: (params?: { driverId?: string; type?: string; severity?: string }) =>
    api.get<DriverIncident[]>('/driver-incidents', { params }).then(r => r.data),
  create: (data: object) => api.post<DriverIncident>('/driver-incidents', data).then(r => r.data),
  delete: (id: string) => api.delete(`/driver-incidents/${id}`),
}

// Phase 3
export const travelApi = {
  getAll: (params?: { status?: string; travelType?: string }) =>
    api.get<TravelRequest[]>('/travel', { params }).then(r => r.data),
  get: (id: string) => api.get<TravelRequest>(`/travel/${id}`).then(r => r.data),
  create: (data: object) => api.post<TravelRequest>('/travel', data).then(r => r.data),
  approve: (id: string, action: string, notes?: string) =>
    api.post(`/travel/${id}/approve`, { action, notes }),
  markBooked: (id: string) => api.patch(`/travel/${id}/booked`),
}

export const projectMaterialsApi = {
  getAll: (params?: { year?: number; project?: string; status?: string }) =>
    api.get<ProjectMaterialTracking[]>('/project-materials', { params }).then(r => r.data),
  get: (id: string) => api.get<ProjectMaterialTracking>(`/project-materials/${id}`).then(r => r.data),
  create: (data: object) =>
    api.post<ProjectMaterialTracking>('/project-materials', data).then(r => r.data),
  update: (id: string, data: object) => api.patch(`/project-materials/${id}`, data),
  delete: (id: string) => api.delete(`/project-materials/${id}`),
  getProjects: (year?: number) =>
    api.get<string[]>('/project-materials/projects', { params: { year } }).then(r => r.data),
}

export const movementRegisterApi = {
  getAll: (params?: { status?: string; movementType?: string; date?: string }) =>
    api.get<MovementRegister[]>('/movement-register', { params }).then(r => r.data),
  get: (id: string) => api.get<MovementRegister>(`/movement-register/${id}`).then(r => r.data),
  create: (data: object) => api.post<MovementRegister>('/movement-register', data).then(r => r.data),
  close: (id: string, returnDateTime: string, mileageIn?: number) =>
    api.patch(`/movement-register/${id}/close`, { returnDateTime, mileageIn }),
}

export function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}
