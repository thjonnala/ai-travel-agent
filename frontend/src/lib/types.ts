// API contract types — mirror the backend DTOs (camelCase JSON, enums as strings).

export type TimeBlock = 'morning' | 'afternoon' | 'evening'
export type ItemType = 'activity' | 'dining' | 'lodging' | 'transport'
export type ChatRole = 'user' | 'assistant' | 'system'
export type TripStatus = 'draft' | 'planned' | 'archived'
export type BudgetBand = 'budget' | 'midRange' | 'luxury'
export type TripPace = 'relaxed' | 'moderate' | 'packed'

export interface TripSummary {
  id: string
  title: string
  destination: string
  startDate: string | null
  endDate: string | null
  status: TripStatus
  estimatedTotalCost: number
  currency: string
  createdAt: string
  updatedAt: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface ItineraryItem {
  id: string
  timeBlock: TimeBlock
  type: ItemType
  sortOrder: number
  title: string
  description: string | null
  estimatedCost: number
  locationName: string | null
  lat: number | null
  lng: number | null
}

export interface ItineraryDay {
  id: string
  dayNumber: number
  date: string | null
  summary: string | null
  estimatedDayCost: number
  items: ItineraryItem[]
}

export interface ChatMessage {
  id: string
  role: ChatRole
  content: string
  createdAt: string
}

export interface TripDetail extends TripSummary {
  days: ItineraryDay[]
  chatMessages: ChatMessage[]
}

export interface Preferences {
  budgetBand: BudgetBand
  pace: TripPace
  interests: string[]
  dietaryNeeds: string | null
  accessibility: string | null
  updatedAt: string | null
}

export function formatMoney(amount: number, currency: string): string {
  try {
    // US-English representation regardless of browser locale.
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      maximumFractionDigits: 0,
    }).format(amount)
  } catch {
    return `${amount.toFixed(0)} ${currency}`
  }
}
