/**
 * Centralized API client. All requests go through here so auth headers,
 * error handling, and 401 redirects live in one place.
 */

// Empty in local dev (Vite proxies /api to the backend); set to the App
// Service origin in deployed builds.
const API_BASE: string = import.meta.env.VITE_API_BASE_URL ?? ''

let getAccessToken: (() => Promise<string | null>) | null = null

/** Called once at startup (milestone 2) to plug in MSAL token acquisition. */
export function setTokenProvider(provider: () => Promise<string | null>) {
  getAccessToken = provider
}

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Content-Type', 'application/json')

  const token = getAccessToken ? await getAccessToken() : null
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`${API_BASE}${path}`, { ...init, headers })

  if (response.status === 401) {
    window.location.assign('/login')
    throw new ApiError(401, 'Not authenticated')
  }

  if (!response.ok) {
    // The API returns RFC 7807 problem details on errors.
    const problem = await response.json().catch(() => null)
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? response.statusText)
  }

  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
