import type { ItineraryDay, ItineraryItem, TripDetail } from '../lib/types'
import { formatMoney } from '../lib/types'

const typeBadge: Record<ItineraryItem['type'], { label: string; className: string }> = {
  activity: { label: 'Activity', className: 'bg-emerald-100 text-emerald-800' },
  dining: { label: 'Dining', className: 'bg-amber-100 text-amber-800' },
  lodging: { label: 'Lodging', className: 'bg-violet-100 text-violet-800' },
  transport: { label: 'Transport', className: 'bg-sky-100 text-sky-800' },
}

const blockLabel: Record<ItineraryItem['timeBlock'], string> = {
  morning: '🌅 Morning',
  afternoon: '🌞 Afternoon',
  evening: '🌙 Evening',
}

function DayCard({ day, currency }: { day: ItineraryDay; currency: string }) {
  const blocks = (['morning', 'afternoon', 'evening'] as const)
    .map((block) => ({ block, items: day.items.filter((i) => i.timeBlock === block) }))
    .filter(({ items }) => items.length > 0)

  return (
    <section
      aria-label={`Day ${day.dayNumber}`}
      className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm"
    >
      <div className="flex items-baseline justify-between gap-2">
        <h3 className="text-base font-semibold">
          Day {day.dayNumber}
          {day.date && <span className="ml-2 text-sm font-normal text-slate-500">{day.date}</span>}
        </h3>
        <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-sm font-medium text-slate-700">
          {formatMoney(day.estimatedDayCost, currency)}
        </span>
      </div>
      {day.summary && <p className="mt-1 text-sm text-slate-600">{day.summary}</p>}

      <div className="mt-3 space-y-3">
        {blocks.map(({ block, items }) => (
          <div key={block}>
            <h4 className="text-xs font-semibold tracking-wide text-slate-500 uppercase">
              {blockLabel[block]}
            </h4>
            <ul className="mt-1 space-y-2">
              {items.map((item) => (
                <li key={item.id} className="flex items-start justify-between gap-3 rounded-lg bg-slate-50 p-2.5">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-medium ${typeBadge[item.type].className}`}
                      >
                        {typeBadge[item.type].label}
                      </span>
                      <span className="font-medium">{item.title}</span>
                    </div>
                    {item.description && (
                      <p className="mt-0.5 text-sm text-slate-600">{item.description}</p>
                    )}
                    {item.locationName && (
                      <p className="mt-0.5 text-xs text-slate-500">📍 {item.locationName}</p>
                    )}
                  </div>
                  <span className="shrink-0 text-sm font-medium text-slate-700">
                    {item.estimatedCost > 0 ? formatMoney(item.estimatedCost, currency) : 'Free'}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
    </section>
  )
}

export default function ItineraryPanel({ trip }: { trip: TripDetail }) {
  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <h2 className="text-xl font-semibold">{trip.title}</h2>
        <p className="mt-0.5 text-sm text-slate-600">
          {trip.destination}
          {trip.startDate && trip.endDate && (
            <span>
              {' '}
              · {trip.startDate} → {trip.endDate}
            </span>
          )}
        </p>
        <p className="mt-2 text-sm">
          <span className="font-semibold text-slate-900">
            {formatMoney(trip.estimatedTotalCost, trip.currency)}
          </span>{' '}
          <span className="text-slate-500">estimated total · {trip.days.length} days</span>
        </p>
      </div>

      {trip.days.map((day) => (
        <DayCard key={day.id} day={day} currency={trip.currency} />
      ))}
    </div>
  )
}
