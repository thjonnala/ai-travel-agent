import type { ReactNode } from 'react'
import { Link, NavLink } from 'react-router-dom'

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-2 text-sm font-medium transition-colors ${
    isActive ? 'bg-sky-100 text-sky-900' : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
  }`

export default function Layout({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <Link to="/" className="block">
            <span className="text-lg font-semibold tracking-tight text-sky-700">
              ✈️ Smart AI Travel Agent
            </span>
            <span className="block text-xs text-slate-500">
              AI-powered planning, human-approved memories.
            </span>
          </Link>
          <nav aria-label="Main" className="flex gap-1">
            <NavLink to="/" end className={navLinkClass}>
              My trips
            </NavLink>
            <NavLink to="/plan" className={navLinkClass}>
              Plan a trip
            </NavLink>
            <NavLink to="/preferences" className={navLinkClass}>
              Preferences
            </NavLink>
          </nav>
        </div>
      </header>
      <div className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">{children}</div>
      <footer className="border-t border-slate-200 bg-white py-3 text-center text-xs text-slate-500">
        Itineraries are AI-generated suggestions — verify details (hours, prices, availability) before booking.
      </footer>
    </div>
  )
}
