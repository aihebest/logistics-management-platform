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
            </Routes>
          </AppShell>
        </BrowserRouter>
      </AuthenticatedTemplate>
    </>
  )
}
