import { useEffect, useRef, useState } from 'react'
import type { ChatMessage } from '../lib/types'

interface ChatPanelProps {
  messages: ChatMessage[]
  onSend: (message: string) => void
  busy: boolean
  error?: string | null
  placeholder?: string
}

export default function ChatPanel({ messages, onSend, busy, error, placeholder }: ChatPanelProps) {
  const [input, setInput] = useState('')
  const scrollRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages.length, busy])

  const submit = (e: React.FormEvent) => {
    e.preventDefault()
    const trimmed = input.trim()
    if (!trimmed || busy) return
    onSend(trimmed)
    setInput('')
  }

  return (
    <div className="flex h-full flex-col rounded-xl border border-slate-200 bg-white shadow-sm">
      <div ref={scrollRef} className="flex-1 space-y-3 overflow-y-auto p-4" aria-live="polite">
        {messages.length === 0 && !busy && (
          <p className="text-sm text-slate-500">
            Describe your trip — destination, length, budget, interests — and I'll draft an
            itinerary. Example: <em>"5 relaxed days in Lisbon in October, mid-range budget, into
            food and history."</em>
          </p>
        )}
        {messages
          .filter((m) => m.role !== 'system')
          .map((m) => (
            <div
              key={m.id}
              className={`max-w-[85%] rounded-2xl px-3.5 py-2 text-sm whitespace-pre-wrap ${
                m.role === 'user'
                  ? 'ml-auto rounded-br-sm bg-sky-600 text-white'
                  : 'mr-auto rounded-bl-sm bg-slate-100 text-slate-800'
              }`}
            >
              {m.content}
            </div>
          ))}
        {busy && (
          <div className="mr-auto flex items-center gap-2 rounded-2xl rounded-bl-sm bg-slate-100 px-3.5 py-2 text-sm text-slate-500">
            <span className="inline-block h-2 w-2 animate-pulse rounded-full bg-sky-500" />
            Planning your trip…
          </div>
        )}
        {error && (
          <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-2.5 text-sm text-red-700">
            {error}
          </div>
        )}
      </div>

      <form onSubmit={submit} className="flex gap-2 border-t border-slate-200 p-3">
        <label htmlFor="chat-input" className="sr-only">
          Trip request
        </label>
        <input
          id="chat-input"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder={placeholder ?? 'Refine your plan: "make day 2 cheaper", "add a kid-friendly activity"…'}
          disabled={busy}
          className="flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-sky-500 focus:ring-2 focus:ring-sky-200 focus:outline-none disabled:bg-slate-50"
        />
        <button
          type="submit"
          disabled={busy || input.trim().length < 3}
          className="rounded-lg bg-sky-600 px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-sky-700 focus:ring-2 focus:ring-sky-300 focus:outline-none disabled:cursor-not-allowed disabled:bg-slate-300"
        >
          Send
        </button>
      </form>
    </div>
  )
}
