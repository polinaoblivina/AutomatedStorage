import { expect, test } from '../fixtures'

test.describe('Шкафы на связи', () => {
  test('после сигнала шкаф отображается в сети', async ({ api, signInAsAdmin, devicesPage }) => {
    await api.device().heartbeat()
    await signInAsAdmin()

    await devicesPage.open()

    await expect(devicesPage.onlineBadge).toBeVisible()
  })

  test('событие карты появляется без перезагрузки страницы', async ({ api, unique, signInAsAdmin, devicesPage }) => {
    const fullName = unique('Событие')
    const card = unique('EVT')
    await api.createUser({ fullName, role: 'Employee', cardUid: card })
    await api.device().heartbeat()
    await signInAsAdmin()
    await devicesPage.open()

    await api.device().checkCard(card)

    await expect(devicesPage.eventRow(fullName)).toContainText('Доступ разрешён')
  })

  test('отказ по неизвестной карте отображается в событиях', async ({ api, unique, signInAsAdmin, devicesPage }) => {
    const card = unique('UNKNOWN')
    await api.device().heartbeat()
    await signInAsAdmin()
    await devicesPage.open()

    await api.device().checkCard(card)

    const row = devicesPage.eventRow(card.toUpperCase())
    await expect(row).toBeVisible()
    await expect(row).toContainText('отказ')
  })
})
