import { Navigate } from 'react-router-dom'
import { useAuth } from '../../auth/useAuth'

/**
 * Where signing in lands you.
 *
 * Operations staff get the dashboard. Everyone else goes to Trip Requests,
 * because the dashboard is no longer theirs to see and dropping them on a page
 * that immediately redirects looks like a fault.
 */
export default function LandingRedirect() {
  const { hasRole } = useAuth()
  const seesDashboard = hasRole('Coordinator', 'Manager', 'Mechanic', 'HOD', 'Management', 'Admin')
  return <Navigate to={seesDashboard ? '/dashboard' : '/trips'} replace />
}
