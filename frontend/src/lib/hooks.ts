import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from './api'
import type { PagedResult, Preferences, TripDetail, TripSummary } from './types'

export function useTrips() {
  return useQuery({
    queryKey: ['trips'],
    queryFn: () => apiFetch<PagedResult<TripSummary>>('/api/trips?pageSize=50'),
  })
}

export function useTrip(tripId: string | undefined) {
  return useQuery({
    queryKey: ['trips', tripId],
    queryFn: () => apiFetch<TripDetail>(`/api/trips/${tripId}`),
    enabled: !!tripId,
  })
}

/** Plans a new trip (no tripId) or refines an existing one. */
export function usePlanTrip() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ request, tripId }: { request: string; tripId?: string }) =>
      apiFetch<TripDetail>('/api/trips/plan', {
        method: 'POST',
        body: JSON.stringify({ request, tripId }),
      }),
    onSuccess: (trip) => {
      queryClient.setQueryData(['trips', trip.id], trip)
      void queryClient.invalidateQueries({ queryKey: ['trips'], exact: true })
    },
  })
}

export function useDeleteTrip() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (tripId: string) => apiFetch<void>(`/api/trips/${tripId}`, { method: 'DELETE' }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['trips'] }),
  })
}

export function useDuplicateTrip() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (tripId: string) =>
      apiFetch<TripDetail>(`/api/trips/${tripId}/duplicate`, { method: 'POST' }),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['trips'] }),
  })
}

export function usePreferences() {
  return useQuery({
    queryKey: ['preferences'],
    queryFn: () => apiFetch<Preferences>('/api/preferences'),
  })
}

export function useUpdatePreferences() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (preferences: Omit<Preferences, 'updatedAt'>) =>
      apiFetch<Preferences>('/api/preferences', {
        method: 'PUT',
        body: JSON.stringify(preferences),
      }),
    onSuccess: (data) => queryClient.setQueryData(['preferences'], data),
  })
}
