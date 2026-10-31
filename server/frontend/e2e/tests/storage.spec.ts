import { expect, test } from '../fixtures'

test.describe('Ячейки и товары', () => {
  test('шкаф, ячейка и товар создаются через интерфейс', async ({ signInAsAdmin, storagePage, unique }) => {
    const cabinet = unique('Шкаф')
    const cell = unique('Яч')
    const item = unique('Болт')
    await signInAsAdmin()
    await storagePage.open()

    await storagePage.addCabinet(cabinet, 'Лаборатория')
    await storagePage.addCell(cabinet, cell)
    await storagePage.placeItem(cell, { name: item, unitWeightGrams: 12.5, minQuantity: 5 })

    await expect(storagePage.stock(cell)).toHaveText('0 шт.')
    await expect(storagePage.tile(cell).getByText('мало')).toBeVisible()
  })

  test('пополнение увеличивает остаток и убирает метку «мало»', async ({ api, signInAsAdmin, storagePage, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const cell = await api.createCell(cabinet.id, unique('Яч'))
    await api.createItem(cell.id, unique('Гайка'), { minQuantity: 5 })
    await signInAsAdmin()
    await storagePage.open()
    await expect(storagePage.tile(cell.name).getByText('мало')).toBeVisible()

    await storagePage.replenish(cell.name, 25)

    await expect(storagePage.stock(cell.name)).toHaveText('25 шт.')
    await expect(storagePage.tile(cell.name).getByText('мало')).toBeHidden()
  })

  test('пополнение на недопустимое количество отклоняется', async ({ api, signInAsAdmin, storagePage, shell, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const cell = await api.createCell(cabinet.id, unique('Яч'))
    await api.createItem(cell.id, unique('Шайба'), { stock: 3 })
    await signInAsAdmin()
    await storagePage.open()

    await storagePage.startReplenish(cell.name, 100001)

    await expect(shell.errorToast).toBeVisible()
    await expect(storagePage.dialog).toBeVisible()
  })

  test('инвентаризация устанавливает фактический остаток', async ({ api, signInAsAdmin, storagePage, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const cell = await api.createCell(cabinet.id, unique('Яч'))
    await api.createItem(cell.id, unique('Винт'), { stock: 10 })
    await signInAsAdmin()
    await storagePage.open()

    await storagePage.adjust(cell.name, 7)

    await expect(storagePage.stock(cell.name)).toHaveText('7 шт.')
  })

  test('товар перемещается в свободную ячейку', async ({ api, signInAsAdmin, storagePage, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const source = await api.createCell(cabinet.id, unique('Из'))
    const target = await api.createCell(cabinet.id, unique('В'))
    const item = unique('Саморез')
    await api.createItem(source.id, item, { stock: 4 })
    await signInAsAdmin()
    await storagePage.open()

    await storagePage.move(source.name, `${cabinet.name}, ${target.name}`)

    await expect(storagePage.tile(target.name).getByText(item)).toBeVisible()
    await expect(storagePage.tile(source.name).getByText('свободна')).toBeVisible()
  })

  test('товар без записей в журнале удаляется, ячейка освобождается', async ({ api, signInAsAdmin, storagePage, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const cell = await api.createCell(cabinet.id, unique('Яч'))
    await api.createItem(cell.id, unique('Пробка'))
    await signInAsAdmin()
    await storagePage.open()

    await storagePage.removeItem(cell.name)

    await expect(storagePage.tile(cell.name).getByText('свободна')).toBeVisible()
  })

  test('товар с историей операций уходит в архив', async ({ api, signInAsAdmin, storagePage, shell, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const cell = await api.createCell(cabinet.id, unique('Яч'))
    await api.createItem(cell.id, unique('Ролик'), { stock: 5 })
    await signInAsAdmin()
    await storagePage.open()

    await storagePage.removeItem(cell.name)

    await expect(shell.toast.getByText('перенесён в архив')).toBeVisible()
    await expect(storagePage.tile(cell.name).getByText('свободна')).toBeVisible()
  })

  test('у занятой ячейки нет удаления, пустая ячейка удаляется', async ({ api, signInAsAdmin, storagePage, unique, page }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const busy = await api.createCell(cabinet.id, unique('Занята'))
    const empty = await api.createCell(cabinet.id, unique('Пуста'))
    await api.createItem(busy.id, unique('Втулка'))
    await signInAsAdmin()
    await storagePage.open()

    await storagePage.openMenuOf(busy.name)
    await expect(storagePage.menuItem('Переименовать ячейку')).toBeVisible()
    await expect(storagePage.menuItem('Удалить ячейку')).toHaveCount(0)
    await page.keyboard.press('Escape')

    await storagePage.deleteCell(empty.name)

    await expect(storagePage.tile(empty.name)).toHaveCount(0)
    await expect(storagePage.tile(busy.name)).toBeVisible()
  })

  test('поиск оставляет только подходящие ячейки', async ({ api, signInAsAdmin, storagePage, unique }) => {
    const cabinet = await api.createCabinet(unique('Шкаф'))
    const first = await api.createCell(cabinet.id, unique('Яч'))
    const second = await api.createCell(cabinet.id, unique('Яч'))
    const firstItem = unique('Искомый')
    await api.createItem(first.id, firstItem)
    await api.createItem(second.id, unique('Другой'))
    await signInAsAdmin()
    await storagePage.open()

    await storagePage.searchInput.fill(firstItem)

    await expect(storagePage.tile(first.name)).toBeVisible()
    await expect(storagePage.tile(second.name)).toHaveCount(0)
  })
})
