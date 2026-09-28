import { Navigate } from 'react-router-dom'
import { useAuth } from '../../auth/useAuth'
import { PageLoader } from '../ui/LoadingSpinner'

/**
 * Where signing in lands you.
 *
 * Operations staff get the dashboard. Everyone else goes to Trip Requests,
 * because the dashboard is no longer theirs to see and dropping them on a page
 * that immediately redirects looks like a fault.
 *
 * Waits for roles first — deciding before they arrive sent everyone to Trip
 * Requests, managers included.
 */
export default function LandingRedirect() {
  const { hasRole, rolesReady } = useAuth()
  if (!rolesReady) return <PageLoader />
  const seesDashboard = hasRole('Coordinator', 'Manager', 'Mechanic', 'HOD', 'Management', 'Admin')
  return <Navigate to={seesDashboard ? '/dashboard' : '/trips'} replace />
}
