import { credentials } from './env'

interface Created {
  id: string
  name: string
}

interface CreatedUser {
  id: string
  fullName: string
  email: string | null
  cardUid: string | null
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly body: string,
  ) {
    super(`HTTP ${status}: ${body}`)
  }
}

export class Api {
  private token: string | null = null
  private roleIds: Record<string, string> | null = null

  constructor(private readonly baseUrl: string) {}

  async request<T>(method: string, path: string, body?: unknown, headers: Record<string, string> = {}): Promise<T> {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method,
      headers: {
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(this.token ? { Authorization: `Bearer ${this.token}` } : {}),
        ...headers,
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    })

    const text = await response.text()
    if (!response.ok) throw new ApiError(response.status, text)

    return (text ? JSON.parse(text) : undefined) as T
  }

  async signIn(email: string, password: string) {
    const result = await this.request<{ token: string }>('POST', '/api/auth/login', { email, password })
    this.token = result.token
    return this
  }

  async signInAsAdmin() {
    const { adminEmail, adminPassword } = credentials()
    return this.signIn(adminEmail, adminPassword)
  }

  private async roleId(name: string) {
    if (!this.roleIds) {
      const roles = await this.request<{ id: string; name: string }[]>('GET', '/api/roles')
      this.roleIds = Object.fromEntries(roles.map((role) => [role.name, role.id]))
    }

    return this.roleIds[name]
  }

  createCabinet(name: string, location = 'Лаборатория') {
    return this.request<Created>('POST', '/api/cabinets', { name, location })
  }

  createCell(cabinetId: string, name: string) {
    return this.request<Created>('POST', `/api/cabinets/${cabinetId}/cells`, { name })
  }

  async createItem(cellId: string, name: string, options: { unitWeightGrams?: number; minQuantity?: number; stock?: number } = {}) {
    const item = await this.request<Created>('POST', '/api/items', {
      cellId,
      name,
      unitWeightGrams: options.unitWeightGrams ?? 20,
      minQuantity: options.minQuantity ?? 0,
    })

    if (options.stock) await this.replenish(item.id, options.stock)

    return item
  }

  replenish(itemId: string, quantity: number) {
    return this.request<void>('POST', `/api/items/${itemId}/replenish`, { quantity })
  }

  adjust(itemId: string, actualQuantity: number) {
    return this.request<void>('POST', `/api/items/${itemId}/adjust`, { actualQuantity })
  }

  async createUser(options: { fullName: string; role: string; cardUid?: string; email?: string; password?: string }) {
    return this.request<CreatedUser>('POST', '/api/users', {
      fullName: options.fullName,
      roleId: await this.roleId(options.role),
      cardUid: options.cardUid ?? null,
      email: options.email ?? null,
      password: options.password ?? null,
    })
  }

  deactivateUser(userId: string) {
    return this.request<void>('POST', `/api/users/${userId}/deactivate`)
  }

  device() {
    const { deviceKey } = credentials()
    const headers = { 'X-Device-Key': deviceKey }

    return {
      heartbeat: () =>
        this.request<void>('POST', '/api/device/heartbeat', { weightGrams: 100.5, rfidOk: true, scaleOk: true }, headers),
      checkCard: (cardUid: string) =>
        this.request<{ accessGranted: boolean }>('POST', '/api/device/check-card', { cardUid }, headers),
      session: (sessionId: string, cardUid: string, cellId: string, weightBefore: number, weightAfter: number) =>
        this.request<{ success: boolean }>(
          'POST',
          '/api/device/sessions',
          { sessionId, cardUid, ageSeconds: 0, cells: [{ cellId, weightBefore, weightAfter }] },
          headers,
        ),
    }
  }
}
