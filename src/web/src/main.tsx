import React from 'react'
import ReactDOM from 'react-dom/client'
import { PublicClientApplication } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from 'react-hot-toast'
import App from './App'
import { msalConfig } from './auth/msalConfig'
import './index.css'

export const msalInstance = new PublicClientApplication(msalConfig)

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <MsalProvider instance={msalInstance}>
      <QueryClientProvider client={queryClient}>
        <App />
        <Toaster position="top-right" />
      </QueryClientProvider>
    </MsalProvider>
  </React.StrictMode>,
)
