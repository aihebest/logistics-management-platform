import { useMsal } from '@azure/msal-react'
import { useEffect, useState } from 'react'
import { apiScopes, loginRequest } from './msalConfig'

export type AppRole = 'Driver' | 'Coordinator' | 'Manager' | 'Mechanic' | 'HOD' | 'Admin'

/** Safely base64-decode a JWT payload segment to extract claims */
function decodeJwtPayload(token: string): Record<string, unknown> {
  try {
    const segment = token.split('.')[1]
    const base64 = segment.replace(/-/g, '+').replace(/_/g, '/')
    return JSON.parse(atob(base64))
  } catch {
    return {}
  }
}

export function useAuth() {
  const { instance, accounts } = useMsal()
  const account = accounts[0]

  // Roles from the cached ID token (available immediately at page load)
  const idTokenRoles: AppRole[] = account
    ? ((account.idTokenClaims as Record<string, unknown>)?.roles as AppRole[] | undefined) ?? []
    : []

  // Fallback: decode roles from the access token.
  // This handles the case where a role was assigned AFTER the user first logged in
  // (the cached ID token won't include the newly-assigned role, but the access token will).
  const [accessTokenRoles, setAccessTokenRoles] = useState<AppRole[]>([])

  useEffect(() => {
    if (!account) return
    instance
      .acquireTokenSilent({ scopes: apiScopes, account })
      .then(result => {
        const payload = decodeJwtPayload(result.accessToken)
        const tokenRoles = (payload.roles as AppRole[] | undefined) ?? []
        setAccessTokenRoles(tokenRoles)
      })
      .catch(() => { /* Silent refresh failed — user will need to log in again */ })
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [account?.homeAccountId])

  // Prefer ID token roles; fall back to decoded access token roles
  const roles: AppRole[] = idTokenRoles.length > 0 ? idTokenRoles : accessTokenRoles

  const hasRole = (...check: AppRole[]) => check.some(r => roles.includes(r))

  const getToken = async () => {
    if (!account) throw new Error('Not authenticated')
    const result = await instance.acquireTokenSilent({ scopes: apiScopes, account })
    return result.accessToken
  }

  const login = () => instance.loginRedirect(loginRequest)
  const logout = () => instance.logoutRedirect({ account })

  return {
    account,
    roles,
    hasRole,
    getToken,
    login,
    logout,
    isAuthenticated: !!account,
    displayName: account?.name ?? account?.username ?? '',
  }
}
