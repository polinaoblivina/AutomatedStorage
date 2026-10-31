import { expect, test } from '../fixtures'

test.describe('Журнал операций', () => {
  test('операции товара отображаются в журнале', async ({ api, signInAsAdmin, journalPage, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const cell = await api.createCell(cabinet.id, unique('Яч'))
    const item = await api.createItem(cell.id, unique('Журнал'), { stock: 3 })
    await api.adjust(item.id, 5)
    await api.replenish(item.id, 2)
    await signInAsAdmin()
    await journalPage.open()

    await journalPage.filterByItem(item.name)

    await journalPage.expectTotal(3)
    await expect(journalPage.rows).toHaveCount(3)
  })

  test('фильтр по типу оставляет только пополнения', async ({ api, signInAsAdmin, journalPage, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const cell = await api.createCell(cabinet.id, unique('Яч'))
    const item = await api.createItem(cell.id, unique('Фильтр'), { stock: 3 })
    await api.adjust(item.id, 5)
    await api.replenish(item.id, 2)
    await signInAsAdmin()
    await journalPage.open()
    await journalPage.filterByItem(item.name)

    await journalPage.filterByType('Пополнено')

    await journalPage.expectTotal(2)
    await expect(journalPage.rows).toHaveCount(2)
    await expect(journalPage.rows.getByText('Инвентаризация')).toHaveCount(0)
  })

  test('у товара без операций журнал пуст', async ({ api, signInAsAdmin, journalPage, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const cell = await api.createCell(cabinet.id, unique('Яч'))
    const item = await api.createItem(cell.id, unique('Пустой'))
    await signInAsAdmin()
    await journalPage.open()

    await journalPage.filterByItem(item.name)

    await expect(journalPage.emptyState).toBeVisible()
    await journalPage.expectTotal(0)
  })
})
