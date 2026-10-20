import type { ReactNode } from 'react'
import { Result, Spin } from 'antd'
import { Navigate } from 'react-router'
import { useAuth } from '../auth/AuthContext'

export function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()

  if (loading) return <Spin size="large" style={{ display: 'block', marginTop: 120 }} />
  if (!user) return <Navigate to="/login" replace />
  if (!user.permissions.includes('ViewJournal')) {
    return <Result status="403" title="Нет доступа" subTitle="Панель доступна кладовщику и администратору." />
  }

  return children
}
