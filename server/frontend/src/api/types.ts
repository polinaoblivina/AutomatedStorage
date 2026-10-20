export interface User {
  id: string
  fullName: string
  email: string | null
  cardUid: string | null
  isActive: boolean
  roleId: string
  roleName: string
  permissions: string[]
}

export interface LoginResponse {
  token: string
  expiresAtUtc: string
  user: User
}

export interface ScaleReading {
  cellId: string | null
  weightGrams: number
  ok: boolean
}

export interface DeviceEvent {
  occurredAtUtc: string
  deviceName: string
  type: 'CardScan' | 'Session'
  cardUid: string | null
  userName: string | null
  success: boolean
  message: string
}

export interface DeviceStatus {
  name: string
  isOnline: boolean
  lastSeenUtc: string | null
  weightGrams: number | null
  rfidOk: boolean | null
  scaleOk: boolean | null
  state: 'Idle' | 'SessionOpen' | null
  firmwareVersion: string | null
  remoteAddress: string | null
  lastEvent: DeviceEvent | null
  scales: ScaleReading[] | null
}

export interface Transaction {
  id: string
  createdAtUtc: string
  userId: string
  userFullName: string | null
  itemId: string
  itemName: string | null
  cellName: string | null
  cabinetName: string | null
  type: 'Withdrawal' | 'Replenishment' | 'Adjustment'
  quantity: number
  weightBefore: number
  weightAfter: number
}

export interface PagedList<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type Permission = 'OpenCell' | 'ManageStock' | 'ViewJournal' | 'ManageUsers' | 'ManageSystem'

export interface CellItem {
  id: string
  name: string
  unitWeightGrams: number
  stockQuantity: number
  minQuantity: number
  isLowStock: boolean
}

export interface Cell {
  id: string
  name: string
  item: CellItem | null
}

export interface Cabinet {
  id: string
  name: string
  location: string
  cells: Cell[]
}

export interface Role {
  id: string
  name: string
  permissions: Permission[]
}
