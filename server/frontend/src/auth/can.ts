import type { Permission, User } from '../api/types'

export function can(user: User | null, permission: Permission): boolean {
  return user?.permissions.includes(permission) ?? false
}
