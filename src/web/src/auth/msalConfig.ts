import { Configuration, LogLevel } from '@azure/msal-browser'

export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID ?? '',
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_ENTRA_TENANT_ID ?? 'common'}`,
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return
        if (level === LogLevel.Error) console.error('[MSAL]', message)
      },
    },
  },
}

export const loginRequest = {
  scopes: [import.meta.env.VITE_ENTRA_API_SCOPE ?? ''],
}

export const apiScopes = [import.meta.env.VITE_ENTRA_API_SCOPE ?? '']
