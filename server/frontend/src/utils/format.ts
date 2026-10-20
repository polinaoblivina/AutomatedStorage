export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—'
  return new Date(value).toLocaleString('ru-RU')
}

export function formatGrams(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—'
  return `${value.toFixed(1)} г`
}

export function errorText(error: unknown): string {
  return error instanceof Error ? error.message : 'Неизвестная ошибка'
}
