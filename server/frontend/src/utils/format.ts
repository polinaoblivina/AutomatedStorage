export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—'
  return new Date(value).toLocaleString('ru-RU')
}

export function formatTime(value: string | null | undefined): string {
  if (!value) return '—'
  return new Date(value).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
}

export function formatGrams(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—'
  return `${value.toFixed(1)} г`
}

export function formatPlace(cabinetName: string | null, cellName: string | null): string {
  return [cabinetName, cellName].filter(Boolean).join(', ') || '—'
}

export function initials(fullName: string): string {
  return fullName
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0].toUpperCase())
    .join('')
}

export function errorText(error: unknown): string {
  return error instanceof Error ? error.message : 'Неизвестная ошибка'
}
