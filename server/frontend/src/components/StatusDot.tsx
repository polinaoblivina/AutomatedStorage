import type { ReactNode } from 'react'
import { colors } from '../theme'

type Tone = 'success' | 'warning' | 'danger' | 'muted'

const toneColors: Record<Tone, string> = {
  success: colors.success,
  warning: colors.warning,
  danger: colors.danger,
  muted: colors.muted,
}

export function StatusDot({ tone, pulse = false, children }: { tone: Tone; pulse?: boolean; children?: ReactNode }) {
  return (
    <span style={{ display: 'inline-flex', alignItems: 'center' }}>
      <span className={pulse ? 'status-dot pulse' : 'status-dot'} style={{ background: toneColors[tone] }} />
      {children}
    </span>
  )
}
