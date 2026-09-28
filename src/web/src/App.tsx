import { AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import AppShell from './components/layout/AppShell'
import RequireRole from './components/layout/RequireRole'
import LandingRedirect from './components/layout/LandingRedirect'
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
import MovementSummaryPage from './pages/MovementRegister/MovementSummaryPage'
import PlatformUsersPage from './pages/Users/PlatformUsersPage'
import DepartmentsPage from './pages/Users/DepartmentsPage'
import type { AppRole } from './auth/useAuth'

/** Anyone who runs logistics operations — sees fleet and movement data. */
const OPS: AppRole[] = ['Coordinator', 'Manager', 'Mechanic', 'HOD', 'Management', 'Admin']
/** Day-to-day operations only — drivers, assignments, workshop. */
const OPS_ADMIN: AppRole[] = ['Coordinator', 'Manager', 'Mechanic', 'Admin']
/** Administration — reports, users, departments. */
const MANAGE: AppRole[] = ['Manager', 'Admin']

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
              {/* Ordinary staff raise trip and travel requests and nothing
                  else, so those two are open and the rest are scoped. The API
                  enforces this independently — these guards are so nobody lands
                  on a page whose data will be refused. */}
              <Route path="/" element={<LandingRedirect />} />
              <Route path="/trips" element={<TripRequestsPage />} />
              <Route path="/travel" element={<TravelRequestPage />} />
              <Route path="/notifications" element={<NotificationsPage />} />

              <Route path="/dashboard" element={<RequireRole roles={OPS}><DashboardPage /></RequireRole>} />
              <Route path="/vehicles" element={<RequireRole roles={OPS}><VehiclesPage /></RequireRole>} />
              <Route path="/fuel" element={<RequireRole roles={OPS}><FuelPage /></RequireRole>} />
              <Route path="/movement-register" element={<RequireRole roles={OPS}><MovementRegisterPage /></RequireRole>} />
              <Route path="/material-transport" element={<RequireRole roles={OPS}><MaterialTransportPage /></RequireRole>} />
              <Route path="/project-materials" element={<RequireRole roles={OPS}><ProjectMaterialsPage /></RequireRole>} />

              <Route path="/drivers" element={<RequireRole roles={OPS_ADMIN}><DriversPage /></RequireRole>} />
              <Route path="/assignments" element={<RequireRole roles={OPS_ADMIN}><AssignmentsPage /></RequireRole>} />
              <Route path="/maintenance" element={<RequireRole roles={OPS_ADMIN}><MaintenancePage /></RequireRole>} />
              <Route path="/movement-summary" element={<RequireRole roles={OPS_ADMIN}><MovementSummaryPage /></RequireRole>} />
              <Route path="/driver-performance" element={<RequireRole roles={OPS_ADMIN}><DriverPerformancePage /></RequireRole>} />
              <Route path="/driver-schedule" element={<RequireRole roles={OPS_ADMIN}><DriverSchedulePage /></RequireRole>} />

              <Route path="/reports" element={<RequireRole roles={MANAGE}><ReportsPage /></RequireRole>} />
              <Route path="/platform-users" element={<RequireRole roles={MANAGE}><PlatformUsersPage /></RequireRole>} />
              <Route path="/departments" element={<RequireRole roles={MANAGE}><DepartmentsPage /></RequireRole>} />
            </Routes>
          </AppShell>
        </BrowserRouter>
      </AuthenticatedTemplate>
    </>
  )
}
