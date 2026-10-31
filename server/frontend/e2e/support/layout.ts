import type { ConsoleMessage, Page, Response } from '@playwright/test'
import type { Api } from './api'

export interface PageInfo {
  name: string
  path: string
  heading: string
}

export const panelPages: PageInfo[] = [
  { name: 'Сводка', path: '/dashboard', heading: 'Сводка' },
  { name: 'Шкафы на связи', path: '/devices', heading: 'Шкафы на связи' },
  { name: 'Ячейки и товары', path: '/storage', heading: 'Ячейки и товары' },
  { name: 'Журнал операций', path: '/journal', heading: 'Журнал операций' },
  { name: 'Пользователи', path: '/users', heading: 'Пользователи' },
]

export async function hasHorizontalScroll(page: Page): Promise<boolean> {
  return page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1)
}

export interface Diagnostics {
  consoleErrors: string[]
  failedResponses: string[]
}

export function collectDiagnostics(page: Page): Diagnostics {
  const diagnostics: Diagnostics = { consoleErrors: [], failedResponses: [] }

  page.on('console', (message: ConsoleMessage) => {
    if (message.type() === 'error') diagnostics.consoleErrors.push(`${message.text()} (${message.location().url})`)
  })
  page.on('pageerror', (error) => diagnostics.consoleErrors.push(String(error)))
  page.on('response', (response: Response) => {
    if (response.status() >= 400) diagnostics.failedResponses.push(`${response.status()} ${response.url()}`)
  })

  return diagnostics
}

export interface RealisticData {
  longItemName: string
  longItemCell: string
  shortItemName: string
  shortItemCell: string
  longUserName: string
  emptyCabinet: string
  bigCabinet: string
  bigCabinetCells: string[]
  mainCabinet: string
  mainCells: string[]
  journalItemName: string
  idleItemName: string
}

export async function seedRealisticData(api: Api, unique: (prefix: string) => string): Promise<RealisticData> {
  const mainCabinet = unique('Основной склад')
  const main = await api.createCabinet(mainCabinet)
  const mainCells: string[] = []
  for (let index = 1; index <= 6; index += 1) {
    const cell = await api.createCell(main.id, `A${index}-${unique('ячейка')}`)
    mainCells.push(cell.name)
    await api.createItem(cell.id, unique(`Товар ${index}`), { stock: index * 3, minQuantity: 5 })
  }

  const longItemName = `${unique('Очень длинное название товара')} ${'длинное слово '.repeat(12)}`.slice(0, 200)
  const longCell = await api.createCell(main.id, unique('Длинная'))
  await api.createItem(longCell.id, longItemName, { stock: 2, minQuantity: 5 })

  const shortItemName = 'Я'
  const shortCell = await api.createCell(main.id, unique('Короткая'))
  await api.createItem(shortCell.id, shortItemName, { stock: 1 })

  const emptyCabinet = unique('Пустой шкаф')
  await api.createCabinet(emptyCabinet)

  const bigCabinet = unique('Большой шкаф')
  const big = await api.createCabinet(bigCabinet)
  const bigCabinetCells: string[] = []
  for (let index = 1; index <= 40; index += 1) {
    const cell = await api.createCell(big.id, `B${String(index).padStart(2, '0')}-${unique('яч')}`)
    bigCabinetCells.push(cell.name)
  }

  const longUserName = `${unique('Фамилия')} ${'Длиннейшее имя '.repeat(14)}`.slice(0, 200)
  await api.createUser({ fullName: longUserName, role: 'Employee', cardUid: unique('LONG') })

  const idleCell = await api.createCell(main.id, unique('Простой'))
  const idleItemName = unique('Без операций')
  await api.createItem(idleCell.id, idleItemName)

  const journalCell = await api.createCell(main.id, unique('Журнал'))
  const journalItemName = unique('Для журнала')
  const journalItem = await api.createItem(journalCell.id, journalItemName, { stock: 1 })
  for (let index = 0; index < 60; index += 1) await api.replenish(journalItem.id, 1)

  return {
    longItemName,
    longItemCell: longCell.name,
    shortItemName,
    shortItemCell: shortCell.name,
    longUserName,
    emptyCabinet,
    bigCabinet,
    bigCabinetCells,
    mainCabinet,
    mainCells,
    journalItemName,
    idleItemName,
  }
}

export interface Box {
  left: number
  top: number
  width: number
  height: number
}

export async function boxesOf(locator: import('@playwright/test').Locator): Promise<Box[]> {
  return locator.evaluateAll((elements) =>
    elements.map((element) => {
      const rect = element.getBoundingClientRect()
      return { left: Math.round(rect.left), top: Math.round(rect.top + window.scrollY), width: Math.round(rect.width), height: Math.round(rect.height) }
    }),
  )
}

export function misalignedBoxes(boxes: Box[], tolerance = 2): string[] {
  const problems: string[] = []
  const rows = new Map<number, Box[]>()

  for (const box of boxes) {
    const key = [...rows.keys()].find((top) => Math.abs(top - box.top) <= tolerance) ?? box.top
    rows.set(key, [...(rows.get(key) ?? []), box])
  }

  for (const [top, row] of rows) {
    const heights = new Set(row.map((box) => Math.round(box.height / (tolerance + 1))))
    if (heights.size > 1) problems.push(`в ряду на высоте ${top} разная высота блоков: ${row.map((box) => box.height).join(', ')}`)
  }

  const lefts = boxes.map((box) => box.left)
  const columns = [...new Set(lefts.map((left) => Math.round(left / (tolerance + 1))))]
  if (columns.length > Math.max(...[...rows.values()].map((row) => row.length))) problems.push(`левые края блоков не образуют колонок: ${[...new Set(lefts)].join(', ')}`)

  return problems
}
