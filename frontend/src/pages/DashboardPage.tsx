import { Link, useNavigate } from 'react-router-dom'
import Layout from '../components/Layout'
import { useDeleteTrip, useDuplicateTrip, useTrips } from '../lib/hooks'
import { formatMoney } from '../lib/types'

export default function DashboardPage() {
  const { data, isLoading, isError } = useTrips()
  const deleteTrip = useDeleteTrip()
  const duplicateTrip = useDuplicateTrip()
  const navigate = useNavigate()

  return (
    <Layout>
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Your trips</h1>
        <Link
          to="/plan"
          className="rounded-lg bg-sky-600 px-4 py-2 text-sm font-medium text-white hover:bg-sky-700"
        >
          + Plan a new trip
        </Link>
      </div>

      {isLoading && <p className="mt-6 text-slate-500">Loading trips…</p>}
      {isError && (
        <p role="alert" className="mt-6 text-red-600">
          Couldn't load your trips. Is the API running?
        </p>
      )}

      {data && data.items.length === 0 && (
        <div className="mt-10 rounded-xl border border-dashed border-slate-300 bg-white p-10 text-center">
          <p className="text-lg font-medium">No trips yet</p>
          <p className="mt-1 text-slate-500">
            Describe your dream trip in plain words and get a full itinerary in seconds.
          </p>
          <Link
            to="/plan"
            className="mt-4 inline-block rounded-lg bg-sky-600 px-4 py-2 text-sm font-medium text-white hover:bg-sky-700"
          >
            Start planning
          </Link>
        </div>
      )}

      <ul className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {data?.items.map((trip) => (
          <li key={trip.id} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
            <Link to={`/trips/${trip.id}`} className="block hover:underline">
              <h2 className="font-semibold">{trip.title}</h2>
            </Link>
            <p className="mt-0.5 text-sm text-slate-600">{trip.destination}</p>
            <p className="mt-2 text-sm">
              <span className="font-medium">{formatMoney(trip.estimatedTotalCost, trip.currency)}</span>
              {trip.startDate && <span className="text-slate-500"> · from {trip.startDate}</span>}
            </p>
            <div className="mt-3 flex gap-2">
              <button
                type="button"
                onClick={() => navigate(`/trips/${trip.id}`)}
                className="rounded-md border border-slate-300 px-2.5 py-1 text-xs font-medium hover:bg-slate-50"
              >
                Open
              </button>
              <button
                type="button"
                onClick={() => duplicateTrip.mutate(trip.id)}
                disabled={duplicateTrip.isPending}
                className="rounded-md border border-slate-300 px-2.5 py-1 text-xs font-medium hover:bg-slate-50 disabled:opacity-50"
              >
                Duplicate
              </button>
              <button
                type="button"
                onClick={() => {
                  if (window.confirm(`Delete "${trip.title}"? This can't be undone.`))
                    deleteTrip.mutate(trip.id)
                }}
                disabled={deleteTrip.isPending}
                className="rounded-md border border-red-200 px-2.5 py-1 text-xs font-medium text-red-600 hover:bg-red-50 disabled:opacity-50"
              >
                Delete
              </button>
            </div>
          </li>
        ))}
      </ul>
    </Layout>
  )
}
