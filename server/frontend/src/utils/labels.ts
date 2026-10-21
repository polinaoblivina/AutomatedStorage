import type { Transaction } from '../api/types'
import { colors } from '../theme'

export const transactionTypes: Record<Transaction['type'], { text: string; tag: string; color: string }> = {
  Withdrawal: { text: 'Взято', tag: 'orange', color: colors.warning },
  Replenishment: { text: 'Пополнено', tag: 'green', color: colors.success },
  Adjustment: { text: 'Инвентаризация', tag: 'blue', color: '#2563eb' },
}

export function signedQuantity(t: Transaction): string {
  if (t.type === 'Withdrawal') return `-${t.quantity}`
  if (t.type === 'Replenishment') return `+${t.quantity}`
  return t.weightAfter >= t.weightBefore ? `+${t.quantity}` : `-${t.quantity}`
}
