import type { ReactNode } from 'react'
import { Result } from 'antd'
import type { Permission } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { can } from '../auth/can'

export function RequirePermission({ permission, children }: { permission: Permission; children: ReactNode }) {
  const { user } = useAuth()

  if (!can(user, permission)) {
    return <Result status="403" title="Нет доступа" subTitle="У вашей роли нет права на этот раздел." />
  }

  return children
}
