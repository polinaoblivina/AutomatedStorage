import { expect, test } from '../fixtures'

test.describe('Сводка', () => {
  test('показывает все блоки итогов', async ({ signInAsAdmin, dashboardPage }) => {
    await signInAsAdmin()
    await dashboardPage.open()

    for (const label of [
      'Шкафы на связи',
      'Операций сегодня',
      'Заканчиваются',
      'Активных сотрудников',
      'Шкафов',
      'Ячеек занято',
      'Товаров',
      'Единиц на складе',
    ]) {
      await expect(dashboardPage.statCard(label)).toBeVisible()
    }
  })

  test('ссылка «Весь журнал» ведёт в журнал операций', async ({ signInAsAdmin, dashboardPage, page }) => {
    await signInAsAdmin()
    await dashboardPage.open()

    await page.getByRole('button', { name: 'Весь журнал' }).click()

    await expect(page).toHaveURL(/\/journal$/)
    await expect(page.getByRole('heading', { name: 'Журнал операций', exact: true })).toBeVisible()
  })
})
