import { Navigate } from 'react-router-dom'
import { useAuth, type AppRole } from '../../auth/useAuth'

/**
 * Keeps a route to the roles that should have it.
 *
 * Hiding a menu item only stops people stumbling into a page — the URL still
 * works if typed. This sends anyone without the role back to Trip Requests,
 * which every signed-in user can use, rather than leaving them on a page whose
 * data the API will refuse to return.
 *
 * This is convenience and tidiness, not the security boundary. The API decides
 * what anyone may actually read; if this and the server ever disagree, the
 * server wins.
 */
export default function RequireRole({
  roles,
  children,
}: {
  roles: AppRole[]
  children: React.ReactNode
}) {
  const { hasRole } = useAuth()
  return hasRole(...roles) ? <>{children}</> : <Navigate to="/trips" replace />
}
