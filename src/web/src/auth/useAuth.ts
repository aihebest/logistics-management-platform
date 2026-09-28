import { useMsal } from '@azure/msal-react'
import { useEffect, useState } from 'react'
import { apiScopes, loginRequest } from './msalConfig'

/**
 * "Management" is the DMD/MD, who gives final approval on travel requests.
 * Kept distinct from "Manager", which means the Logistics Manager.
 * Must stay in step with ValidRoles in PlatformUsersController.
 */
export type AppRole =
  | 'Driver' | 'Coordinator' | 'Manager' | 'Mechanic'
  | 'HOD' | 'Management' | 'Staff' | 'Admin'

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

  // Roles from the access token — the same token the API checks, so it is the
  // one to believe. Fetched in the background, hence null until it arrives.
  const [accessTokenRoles, setAccessTokenRoles] = useState<AppRole[] | null>(null)
  const [tokenCheckDone, setTokenCheckDone] = useState(false)

  useEffect(() => {
    if (!account) return
    instance
      .acquireTokenSilent({ scopes: apiScopes, account })
      .then(result => {
        const payload = decodeJwtPayload(result.accessToken)
        setAccessTokenRoles((payload.roles as AppRole[] | undefined) ?? [])
      })
      .catch(() => { /* Silent refresh failed — fall back to the cached ID token */ })
      .finally(() => setTokenCheckDone(true))
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [account?.homeAccountId])

  // Once the access token is in, it wins. The cached ID token can be days old
  // and still carry roles someone has since gained or lost — preferring it is
  // why a role changed in Entra took so long to show up.
  const roles: AppRole[] = accessTokenRoles ?? idTokenRoles

  // Nothing should decide access before the roles are known. Route guards
  // checked too early, saw no roles, and bounced everyone — Admin included —
  // back to Trip Requests, even though the menu then showed the right items.
  const rolesReady = !account || tokenCheckDone

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
    rolesReady,
    hasRole,
    getToken,
    login,
    logout,
    isAuthenticated: !!account,
    displayName: account?.name ?? account?.username ?? '',
  }
}
