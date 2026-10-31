import type { FullConfig } from '@playwright/test'
import { Api } from './api'

const warmUpPaths = [
  '/api/auth/me',
  '/api/roles',
  '/api/cabinets',
  '/api/items',
  '/api/users',
  '/api/devices',
  '/api/devices/events',
  '/api/transactions?page=1&pageSize=8',
  '/api/statistics/dashboard?days=14',
]

export default async function globalSetup(config: FullConfig) {
  const baseURL = config.projects[0].use.baseURL as string
  const api = await new Api(baseURL).signInAsAdmin()

  for (const path of warmUpPaths) await api.request('GET', path)
}
