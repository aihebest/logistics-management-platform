import { useAuth } from '../auth/useAuth'

export default function LoginPage() {
  const { login } = useAuth()

  return (
    <div className="min-h-screen flex">
      {/* Left panel — brand identity */}
      <div className="hidden lg:flex lg:w-1/2 bg-gradient-to-br from-indigo-950 via-indigo-900 to-blue-900 flex-col items-center justify-center p-12 relative overflow-hidden">
        {/* Background decoration */}
        <div className="absolute inset-0 opacity-10">
          <div className="absolute top-0 left-0 w-96 h-96 rounded-full bg-cyan-400 -translate-x-1/2 -translate-y-1/2" />
          <div className="absolute bottom-0 right-0 w-80 h-80 rounded-full bg-blue-400 translate-x-1/3 translate-y-1/3" />
        </div>

        <div className="relative z-10 text-center">
          {/* Logo */}
          <div className="flex justify-center mb-8">
            <img src="/desicon-logo.svg" alt="Desicon Engineering" className="w-28 h-28 drop-shadow-xl" />
          </div>

          <h1 className="text-4xl font-extrabold text-white tracking-tight">
            Desicon Engineering
          </h1>
          <p className="text-blue-200 text-lg mt-2 font-medium">Logistics & Fleet Management Platform</p>

          <div className="mt-12 space-y-4 text-left max-w-xs mx-auto">
            {[
              { icon: '🚛', text: 'Real-time fleet visibility' },
              { icon: '👷', text: 'Driver availability & assignment' },
              { icon: '🔧', text: 'Maintenance tracking & alerts' },
              { icon: '⛽', text: 'Fuel consumption reporting' },
            ].map(item => (
              <div key={item.text} className="flex items-center gap-3 text-blue-100">
                <span className="text-xl">{item.icon}</span>
                <span className="text-sm font-medium">{item.text}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Right panel — sign in */}
      <div className="flex-1 flex items-center justify-center bg-gray-50 p-8">
        <div className="w-full max-w-sm">
          {/* Mobile logo (only shows on small screens) */}
          <div className="lg:hidden flex flex-col items-center mb-8">
            <img src="/desicon-logo.svg" alt="Desicon Engineering" className="w-16 h-16 mb-3" />
            <h1 className="text-xl font-bold text-gray-900">Desicon Engineering</h1>
            <p className="text-sm text-gray-500">Logistics & Fleet Management</p>
          </div>

          <div className="bg-white rounded-2xl shadow-sm border border-gray-200 p-8">
            <h2 className="text-xl font-bold text-gray-900 mb-1">Welcome back</h2>
            <p className="text-sm text-gray-500 mb-6">
              Sign in with your Desicon Microsoft work account to continue.
            </p>

            <button
              onClick={login}
              className="w-full flex items-center justify-center gap-3 bg-indigo-700 hover:bg-indigo-800 text-white font-semibold py-3 px-4 rounded-xl transition-colors shadow-sm"
            >
              {/* Microsoft logo */}
              <svg width="20" height="20" viewBox="0 0 21 21" fill="none" xmlns="http://www.w3.org/2000/svg">
                <rect x="1" y="1" width="9" height="9" fill="#F25022"/>
                <rect x="11" y="1" width="9" height="9" fill="#7FBA00"/>
                <rect x="1" y="11" width="9" height="9" fill="#00A4EF"/>
                <rect x="11" y="11" width="9" height="9" fill="#FFB900"/>
              </svg>
              Sign in with Microsoft
            </button>

            <p className="text-xs text-gray-400 text-center mt-5 leading-relaxed">
              Secured by Microsoft Entra ID.<br />
              No separate password is required.
            </p>
          </div>

          <p className="text-center text-xs text-gray-400 mt-6">
            © {new Date().getFullYear()} Desicon Engineering. All rights reserved.
          </p>
        </div>
      </div>
    </div>
  )
}
