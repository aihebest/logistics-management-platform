import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import AppShell from './components/layout/AppShell'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/Dashboard/DashboardPage'
import DriversPage from './pages/Drivers/DriversPage'
import VehiclesPage from './pages/Vehicles/VehiclesPage'
import TripRequestsPage from './pages/TripRequests/TripRequestsPage'
import AssignmentsPage from './pages/Assignments/AssignmentsPage'
import MaintenancePage from './pages/Maintenance/MaintenancePage'
import FuelPage from './pages/Fuel/FuelPage'
import ReportsPage from './pages/Reports/ReportsPage'
import NotificationsPage from './pages/Notifications/NotificationsPage'
// Phase 2
import MaterialTransportPage from './pages/MaterialTransport/MaterialTransportPage'
import DriverPerformancePage from './pages/DriverPerformance/DriverPerformancePage'
import DriverSchedulePage from './pages/DriverSchedule/DriverSchedulePage'
// Phase 3
import TravelRequestPage from './pages/Travel/TravelRequestPage'
import ProjectMaterialsPage from './pages/ProjectMaterials/ProjectMaterialsPage'
import MovementRegisterPage from './pages/MovementRegister/MovementRegisterPage'

export default function App() {
  return (
    <>
      <UnauthenticatedTemplate>
        <LoginPage />
      </UnauthenticatedTemplate>
      <AuthenticatedTemplate>
        <BrowserRouter>
          <AppShell>
            <Routes>
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/drivers" element={<DriversPage />} />
              <Route path="/vehicles" element={<VehiclesPage />} />
              <Route path="/trips" element={<TripRequestsPage />} />
              <Route path="/assignments" element={<AssignmentsPage />} />
              <Route path="/maintenance" element={<MaintenancePage />} />
              <Route path="/fuel" element={<FuelPage />} />
              <Route path="/reports" element={<ReportsPage />} />
              <Route path="/notifications" element={<NotificationsPage />} />
              {/* Phase 2 */}
              <Route path="/material-transport" element={<MaterialTransportPage />} />
              <Route path="/driver-performance" element={<DriverPerformancePage />} />
              <Route path="/driver-schedule" element={<DriverSchedulePage />} />
              {/* Phase 3 */}
              <Route path="/travel" element={<TravelRequestPage />} />
              <Route path="/project-materials" element={<ProjectMaterialsPage />} />
              <Route path="/movement-register" element={<MovementRegisterPage />} />
            </Routes>
          </AppShell>
        </BrowserRouter>
      </AuthenticatedTemplate>
    </>
  )
}
