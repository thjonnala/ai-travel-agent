import { useState } from 'react'
import Layout from '../components/Layout'
import { usePreferences, useUpdatePreferences } from '../lib/hooks'
import type { BudgetBand, Preferences, TripPace } from '../lib/types'

const inputClass =
  'mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-sky-500 focus:ring-2 focus:ring-sky-200 focus:outline-none'

export default function PreferencesPage() {
  const { data, isLoading } = usePreferences()

  return (
    <Layout>
      <h1 className="text-2xl font-semibold">Traveler preferences</h1>
      <p className="mt-1 text-slate-600">
        These are applied automatically to every itinerary the AI plans for you.
      </p>

      {isLoading ? (
        <p className="mt-6 text-slate-500">Loading…</p>
      ) : (
        <PreferencesForm initial={data} />
      )}
    </Layout>
  )
}

// Mounted only once preferences have loaded, so initial form state can come
// straight from props — no state-syncing effect needed.
function PreferencesForm({ initial }: { initial: Preferences | undefined }) {
  const update = useUpdatePreferences()

  const [budgetBand, setBudgetBand] = useState<BudgetBand>(initial?.budgetBand ?? 'midRange')
  const [pace, setPace] = useState<TripPace>(initial?.pace ?? 'moderate')
  const [interests, setInterests] = useState(initial?.interests.join(', ') ?? '')
  const [dietaryNeeds, setDietaryNeeds] = useState(initial?.dietaryNeeds ?? '')
  const [accessibility, setAccessibility] = useState(initial?.accessibility ?? '')

  const submit = (e: React.FormEvent) => {
    e.preventDefault()
    update.mutate({
      budgetBand,
      pace,
      interests: interests.split(',').map((i) => i.trim()).filter(Boolean),
      dietaryNeeds: dietaryNeeds.trim() || null,
      accessibility: accessibility.trim() || null,
    })
  }

  return (
    <form onSubmit={submit} className="mt-6 max-w-xl space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <div>
          <label htmlFor="budget" className="text-sm font-medium">
            Budget
          </label>
          <select
            id="budget"
            value={budgetBand}
            onChange={(e) => setBudgetBand(e.target.value as BudgetBand)}
            className={inputClass}
          >
            <option value="budget">Budget</option>
            <option value="midRange">Mid-range</option>
            <option value="luxury">Luxury</option>
          </select>
        </div>
        <div>
          <label htmlFor="pace" className="text-sm font-medium">
            Pace
          </label>
          <select
            id="pace"
            value={pace}
            onChange={(e) => setPace(e.target.value as TripPace)}
            className={inputClass}
          >
            <option value="relaxed">Relaxed</option>
            <option value="moderate">Moderate</option>
            <option value="packed">Packed</option>
          </select>
        </div>
      </div>

      <div>
        <label htmlFor="interests" className="text-sm font-medium">
          Interests <span className="font-normal text-slate-500">(comma-separated)</span>
        </label>
        <input
          id="interests"
          value={interests}
          onChange={(e) => setInterests(e.target.value)}
          placeholder="food, history, hiking, art"
          className={inputClass}
        />
      </div>

      <div>
        <label htmlFor="dietary" className="text-sm font-medium">
          Dietary needs
        </label>
        <input
          id="dietary"
          value={dietaryNeeds}
          onChange={(e) => setDietaryNeeds(e.target.value)}
          placeholder="vegetarian, gluten-free…"
          className={inputClass}
        />
      </div>

      <div>
        <label htmlFor="accessibility" className="text-sm font-medium">
          Accessibility
        </label>
        <input
          id="accessibility"
          value={accessibility}
          onChange={(e) => setAccessibility(e.target.value)}
          placeholder="step-free access, limited walking…"
          className={inputClass}
        />
      </div>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={update.isPending}
          className="rounded-lg bg-sky-600 px-4 py-2 text-sm font-medium text-white hover:bg-sky-700 disabled:bg-slate-300"
        >
          {update.isPending ? 'Saving…' : 'Save preferences'}
        </button>
        {update.isSuccess && <span className="text-sm text-emerald-600">Saved ✓</span>}
        {update.isError && (
          <span role="alert" className="text-sm text-red-600">
            Couldn't save — try again.
          </span>
        )}
      </div>
    </form>
  )
}
