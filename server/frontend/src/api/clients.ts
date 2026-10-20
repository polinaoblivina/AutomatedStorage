import type { LoginResponse } from './types'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

let accessToken: string | null = null
let expiresAt = 0
let refreshing: Promise<LoginResponse | null> | null = null

export function saveSession(response: LoginResponse | null) {
  accessToken = response?.token ?? null
  expiresAt = response ? new Date(response.expiresAtUtc).getTime() : 0
}

async function postRefresh(): Promise<Response> {
  const response = await fetch('/api/auth/refresh', { method: 'POST' })
  if (response.status !== 409) return response

  await new Promise((resolve) => setTimeout(resolve, 500))
  return fetch('/api/auth/refresh', { method: 'POST' })
}

export function refreshSession(): Promise<LoginResponse | null> {
  refreshing ??= postRefresh()
    .then(async (response) => (response.ok ? ((await response.json()) as LoginResponse) : null))
    .catch(() => null)
    .then((session) => {
      saveSession(session)
      return session
    })
    .finally(() => {
      refreshing = null
    })

  return refreshing
}

export async function getFreshAccessToken(): Promise<string> {
  if (accessToken && expiresAt - Date.now() > 60_000) return accessToken
  const session = await refreshSession()
  return session?.token ?? ''
}

async function readError(response: Response): Promise<string> {
  try {
    const problem = await response.json()
    return problem.detail ?? problem.title ?? `Ошибка ${response.status}`
  } catch {
    return `Ошибка ${response.status}`
  }
}

export async function api<T>(path: string, init: { method?: string; body?: unknown } = {}, retry = true): Promise<T> {
  const headers: Record<string, string> = {}
  if (accessToken) headers.Authorization = `Bearer ${accessToken}`
  if (init.body !== undefined) headers['Content-Type'] = 'application/json'

  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      method: init.method ?? 'GET',
      headers,
      body: init.body !== undefined ? JSON.stringify(init.body) : undefined,
    })
  } catch {
    throw new ApiError(0, 'Сервер недоступен. Проверьте, что он запущен.')
  }

  if (response.status === 401 && retry && !path.startsWith('/auth/') && (await refreshSession())) {
    return api<T>(path, init, false)
  }

  if (!response.ok) throw new ApiError(response.status, await readError(response))
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}
