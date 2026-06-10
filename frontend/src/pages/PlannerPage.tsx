import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import ChatPanel from '../components/ChatPanel'
import ItineraryPanel from '../components/ItineraryPanel'
import Layout from '../components/Layout'
import { usePlanTrip, useTrip } from '../lib/hooks'
import type { ChatMessage } from '../lib/types'

export default function PlannerPage() {
  const { tripId } = useParams<{ tripId: string }>()
  const navigate = useNavigate()
  const { data: trip, isLoading } = useTrip(tripId)
  const planTrip = usePlanTrip()

  // Echo the user's message immediately; the server copy replaces it on success.
  const [pendingMessage, setPendingMessage] = useState<ChatMessage | null>(null)

  const send = (message: string) => {
    setPendingMessage({
      id: 'pending',
      role: 'user',
      content: message,
      createdAt: new Date().toISOString(),
    })
    planTrip.mutate(
      { request: message, tripId },
      {
        onSuccess: (planned) => {
          setPendingMessage(null)
          if (!tripId) navigate(`/trips/${planned.id}`, { replace: true })
        },
      },
    )
  }

  const messages: ChatMessage[] = [
    ...(trip?.chatMessages ?? []),
    ...(pendingMessage ? [pendingMessage] : []),
  ]

  return (
    <Layout>
      <h1 className="sr-only">Trip planner</h1>
      {tripId && isLoading ? (
        <p className="text-slate-500">Loading trip…</p>
      ) : (
        <div className="grid gap-4 lg:grid-cols-[2fr_3fr]">
          <div className="lg:h-[calc(100vh-10rem)] lg:sticky lg:top-4 h-[28rem]">
            <ChatPanel
              messages={messages}
              onSend={send}
              busy={planTrip.isPending}
              error={planTrip.isError ? planTrip.error.message : null}
              placeholder={
                trip
                  ? 'Refine your plan: "make day 2 cheaper", "I\'m vegetarian"…'
                  : 'Describe your trip: "5 relaxed days in Lisbon in October…"'
              }
            />
          </div>
          <div>
            {trip ? (
              <ItineraryPanel trip={trip} />
            ) : (
              <div className="flex h-full min-h-[16rem] items-center justify-center rounded-xl border border-dashed border-slate-300 bg-white p-8 text-center text-slate-500">
                Your itinerary will appear here as soon as the first plan is ready.
              </div>
            )}
          </div>
        </div>
      )}
    </Layout>
  )
}
