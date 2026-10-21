import type { CSSProperties, ReactNode } from 'react'
import { Card } from 'antd'
import { colors } from '../theme'

interface StatCardProps {
  label: string
  value: ReactNode
  hint?: ReactNode
  accent?: string
}

export function StatCard({ label, value, hint, accent = colors.primary }: StatCardProps) {
  return (
    <Card className="stat-card" style={{ '--accent': accent } as CSSProperties}>
      <div className="stat-label">{label}</div>
      <div className="stat-value">{value}</div>
      {hint && <div className="stat-hint">{hint}</div>}
    </Card>
  )
}
