import { useState, useEffect } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { useAuth } from '../../auth/useAuth'
import { useQuery } from '@tanstack/react-query'
import { authApi, notificationsApi } from '../../services/api'

const navGroups = [
  {
    group: 'Overview',
    items: [
      { to: '/dashboard', label: 'Dashboard', icon: 'M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6', roles: [] as string[] },
    ],
  },
  {
    group: 'Operations',
    items: [
      { to: '/trips', label: 'Trip Requests', icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2', roles: [] as string[] },
      { to: '/assignments', label: 'Assignments', icon: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z', roles: ['Coordinator', 'Manager', 'Admin'] },
      { to: '/movement-register', label: 'Movement Register', icon: 'M9 5l7 7-7 7', roles: [] as string[] },
      { to: '/material-transport', label: 'Material Transport', icon: 'M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4', roles: [] as string[] },
    ],
  },
  {
    group: 'Fleet',
    items: [
      { to: '/vehicles', label: 'Vehicles', icon: 'M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0M3 7h18M3 7l2-4h14l2 4M3 7v7a2 2 0 002 2h1m12 0h1a2 2 0 002-2V7', roles: [] as string[] },
      { to: '/maintenance', label: 'Maintenance', icon: 'M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z', roles: ['Coordinator', 'Manager', 'Mechanic', 'Admin'] },
      { to: '/fuel', label: 'Fuel Logs', icon: 'M13 10V3L4 14h7v7l9-11h-7z', roles: [] as string[] },
    ],
  },
  {
    group: 'Drivers',
    items: [
      { to: '/drivers', label: 'Drivers', icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z', roles: ['Coordinator', 'Manager', 'Admin'] },
      { to: '/driver-schedule', label: 'Driver Schedule', icon: 'M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z', roles: ['Coordinator', 'Manager', 'Admin'] },
      { to: '/driver-performance', label: 'Performance', icon: 'M13 7h8m0 0v8m0-8l-8 8-4-4-6 6', roles: ['Coordinator', 'Manager', 'Admin'] },
    ],
  },
  {
    group: 'Projects & Reports',
    items: [
      { to: '/project-materials', label: 'Materials Status', icon: 'M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z', roles: [] as string[] },
      { to: '/reports', label: 'Reports', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z', roles: ['Manager', 'Admin'] },
    ],
  },
]

type AppRole = import('../../auth/useAuth').AppRole

export default function AppShell({ children }: { children: React.ReactNode }) {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const { pathname } = useLocation()
  const { hasRole, displayName, logout, roles, account } = useAuth()

  // Provision the user in the platform DB on every login session.
  // auth/me creates or links the user record so that protected write
  // endpoints (trips, assignments, etc.) can resolve the caller by OID.
  useEffect(() => {
    if (!account) return
    authApi.me().catch(err => {
      console.warn('[AppShell] User provisioning via auth/me failed:', err)
    })
  // Re-run if the signed-in account changes (e.g. different user logs in)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [account?.homeAccountId])

  const { data: notifications = [] } = useQuery({
    queryKey: ['notifications'],
    queryFn: notificationsApi.getMine,
    refetchInterval: 30_000,
  })
  const unreadCount = notifications.filter(n => !n.isRead).length

  const isVisible = (itemRoles: string[]) =>
    itemRoles.length === 0 || hasRole(...(itemRoles as AppRole[]))

  const Sidebar = () => (
    <div className="flex flex-col h-full bg-[#1a2744]">
      {/* Logo / Brand */}
      <div className="flex items-center gap-3 px-5 py-5 border-b border-white/10">
        <div className="w-9 h-9 rounded-lg bg-brand-500 flex items-center justify-center flex-shrink-0">
          <svg className="w-5 h-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
              d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0M3 7h18M3 7l2-4h14l2 4M3 7v7a2 2 0 002 2h1m12 0h1a2 2 0 002-2V7" />
          </svg>
        </div>
        <div className="leading-tight">
          <span className="font-bold text-white text-sm block">Desicon Engineering</span>
          <span className="text-xs text-blue-300/80">Logistics Platform</span>
        </div>
      </div>

      <nav className="flex-1 px-3 py-4 space-y-5 overflow-y-auto">
        {navGroups.map(group => {
          const visibleItems = group.items.filter(item => isVisible(item.roles))
          if (visibleItems.length === 0) return null
          return (
            <div key={group.group}>
              <p className="px-3 mb-1.5 text-[10px] font-bold text-blue-300/50 uppercase tracking-widest">
                {group.group}
              </p>
              <div className="space-y-0.5">
                {visibleItems.map(item => {
                  const active = pathname.startsWith(item.to)
                  return (
                    <Link key={item.to} to={item.to}
                      onClick={() => setSidebarOpen(false)}
                      className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all ${
                        active
                          ? 'bg-brand-600 text-white shadow-lg shadow-brand-900/40'
                          : 'text-blue-100/70 hover:bg-white/10 hover:text-white'
                      }`}>
                      <svg className={`w-4 h-4 flex-shrink-0 ${active ? 'text-white' : 'text-blue-300/60'}`}
                        fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d={item.icon} />
                      </svg>
                      {item.label}
                    </Link>
                  )
                })}
              </div>
            </div>
          )
        })}
      </nav>

      {/* User footer */}
      <div className="px-3 py-4 border-t border-white/10">
        <div className="flex items-center gap-3 px-2 py-2 rounded-lg hover:bg-white/5 transition-colors">
          <div className="w-8 h-8 bg-brand-500 rounded-full flex items-center justify-center text-white font-semibold text-sm flex-shrink-0">
            {displayName.charAt(0).toUpperCase()}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-white truncate">{displayName}</p>
            <p className="text-xs text-blue-300/70">
              {roles.length > 0
                ? roles[0]
                : <span className="italic text-blue-300/40">Loading…</span>}
            </p>
          </div>
          <button onClick={logout} title="Sign out"
            className="text-blue-300/50 hover:text-red-400 p-1 rounded transition-colors">
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  )

  return (
    <div className="flex h-screen bg-slate-100">
      {/* Desktop sidebar */}
      <div className="hidden lg:flex lg:flex-col lg:w-64 lg:fixed lg:inset-y-0 shadow-xl">
        <Sidebar />
      </div>

      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div className="lg:hidden fixed inset-0 z-50 flex">
          <div className="fixed inset-0 bg-black/60" onClick={() => setSidebarOpen(false)} />
          <div className="relative flex flex-col w-64 shadow-2xl">
            <Sidebar />
          </div>
        </div>
      )}

      {/* Main content */}
      <div className="flex-1 flex flex-col lg:pl-64">
        {/* Top bar */}
        <header className="bg-white border-b border-gray-200 px-4 py-3 flex items-center justify-between sticky top-0 z-10 shadow-sm">
          <button className="lg:hidden p-2 rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100"
            onClick={() => setSidebarOpen(true)}>
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>

          {/* Breadcrumb / page title area */}
          <div className="flex-1 lg:flex items-center hidden">
            <span className="text-xs text-gray-400">
              Desicon Engineering Logistics Platform
            </span>
          </div>

          {/* Right: notifications bell */}
          <Link to="/notifications" className="relative p-2 text-gray-500 hover:text-brand-600 hover:bg-brand-50 rounded-lg transition-colors">
            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
            </svg>
            {unreadCount > 0 && (
              <span className="absolute top-1 right-1 w-4 h-4 bg-red-500 text-white text-[10px] font-bold rounded-full flex items-center justify-center">
                {unreadCount > 9 ? '9+' : unreadCount}
              </span>
            )}
          </Link>
        </header>

        <main className="flex-1 overflow-y-auto p-5 lg:p-6">
          {children}
        </main>
      </div>
    </div>
  )
}
