import { randomUUID } from 'node:crypto'
import path from 'node:path'
import type { Locator } from '@playwright/test'
import { Api } from '../support/api'
import {
  boxesOf,
  collectDiagnostics,
  hasHorizontalScroll,
  misalignedBoxes,
  panelPages,
  seedRealisticData,
  type RealisticData,
} from '../support/layout'
import { expect, test as base } from '../fixtures'

const test = base.extend<object, { data: RealisticData }>({
  data: [
    async ({ browserName }, provide, workerInfo) => {
      const api = await new Api(workerInfo.project.use.baseURL as string).signInAsAdmin()
      const data = await seedRealisticData(api, (prefix) => `${prefix}-${browserName.slice(0, 2)}${randomUUID().slice(0, 8)}`)
      await provide(data)
    },
    { scope: 'worker', timeout: 120_000 },
  ],
})

const screenshotDirectory = path.resolve(import.meta.dirname, '../../../../docs/img/compat')
const takeScreenshots = process.env.COMPAT_SHOTS === '1'

test.describe('Вёрстка и совместимость', () => {
  test('K1 Нет ошибок JavaScript, ответов 404 и неуспешных запросов', async ({ data, signInAsAdmin, page }) => {
    test.slow()
    expect(data.mainCabinet).toBeTruthy()
    const diagnostics = collectDiagnostics(page)
    await signInAsAdmin()
    const afterSignIn = { consoleErrors: diagnostics.consoleErrors.length, failedResponses: diagnostics.failedResponses.length }

    for (const panelPage of panelPages) {
      await page.goto(panelPage.path)
      await expect(page.getByRole('heading', { name: panelPage.heading, exact: true })).toBeVisible()
      await page.waitForLoadState('networkidle')
    }

    expect(diagnostics.failedResponses.slice(afterSignIn.failedResponses)).toEqual([])
    expect(diagnostics.consoleErrors.slice(afterSignIn.consoleErrors)).toEqual([])
  })

  test('K2 Favicon подключён и доступен', async ({ page, request, baseURL }) => {
    await page.goto('/login')

    const href = await page.locator('link[rel~="icon"]').first().getAttribute('href')
    expect(href).toBeTruthy()

    const response = await request.get(new URL(href as string, baseURL).toString())
    expect(response.status()).toBe(200)
    expect(response.headers()['content-type']).toMatch(/image/)
  })

  for (const width of [1440, 1280]) {
    test(`V1 Блоки целые, горизонтальной прокрутки нет при ширине ${width}`, async ({ data, signInAsAdmin, page }) => {
      test.slow()
      expect(data.mainCabinet).toBeTruthy()
      await page.setViewportSize({ width, height: 900 })
      await signInAsAdmin()

      for (const panelPage of panelPages) {
        await page.goto(panelPage.path)
        await expect(page.getByRole('heading', { name: panelPage.heading, exact: true })).toBeVisible()
        await page.waitForLoadState('networkidle')
        expect(await hasHorizontalScroll(page), `${panelPage.name}: горизонтальная прокрутка`).toBe(false)
      }
    })
  }

  test('V2 Блоки выровнены по сетке', async ({ data, signInAsAdmin, page, shell }) => {
    expect(data.mainCabinet).toBeTruthy()
    await page.setViewportSize({ width: 1440, height: 900 })
    await signInAsAdmin()
    await expect(page.locator('.stat-card').first()).toBeVisible()

    expect(misalignedBoxes(await boxesOf(page.locator('.stat-card')))).toEqual([])

    await shell.menuItem('Ячейки и товары').click()
    const bigCabinet = page.locator('.ant-card', { has: page.locator('.ant-card-head-title', { hasText: data.bigCabinet }) })
    await expect(bigCabinet.locator('.cell-tile').first()).toBeVisible()
    expect(misalignedBoxes(await boxesOf(bigCabinet.locator('.cell-tile')))).toEqual([])

    await shell.menuItem('Пользователи').click()
    await expect(page.locator('.stat-card').first()).toBeVisible()
    expect(misalignedBoxes(await boxesOf(page.locator('.stat-card')))).toEqual([])
  })

  test('V3 Знак и текст рядом выровнены и не перекрываются', async ({ signInAsAdmin, page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await signInAsAdmin()

    const mark = await page.locator('.brand-mark').boundingBox()
    const name = await page.locator('.brand-name').boundingBox()
    expect(mark).not.toBeNull()
    expect(name).not.toBeNull()
    expect((mark as { x: number; width: number }).x + (mark as { width: number }).width).toBeLessThanOrEqual((name as { x: number }).x)
    const markCenter = (mark as { y: number; height: number }).y + (mark as { height: number }).height / 2
    const nameCenter = (name as { y: number; height: number }).y + (name as { height: number }).height / 2
    expect(Math.abs(markCenter - nameCenter)).toBeLessThanOrEqual(6)

    const avatar = await page.locator('.sider-user .ant-avatar').boundingBox()
    const userName = await page.locator('.sider-user-name').boundingBox()
    expect((avatar as { x: number; width: number }).x + (avatar as { width: number }).width).toBeLessThanOrEqual((userName as { x: number }).x)
  })

  for (const width of [1280, 1024, 768, 375]) {
    test(`A1 Адаптивность: ширина ${width}, горизонтальной прокрутки нет`, async ({ data, signInAsAdmin, page }) => {
      test.slow()
      expect(data.mainCabinet).toBeTruthy()
      await page.setViewportSize({ width, height: 900 })
      await signInAsAdmin()

      for (const panelPage of panelPages) {
        await page.goto(panelPage.path)
        await expect(page.getByRole('heading', { name: panelPage.heading, exact: true })).toBeVisible()
        await page.waitForLoadState('networkidle')
        expect(await hasHorizontalScroll(page), `${panelPage.name}: горизонтальная прокрутка`).toBe(false)
      }
    })
  }

  test('A2 Меню доступно на узком экране и открывает разделы', async ({ signInAsAdmin, page, shell }) => {
    await page.setViewportSize({ width: 768, height: 900 })
    await signInAsAdmin()

    await page.getByRole('button', { name: 'Меню' }).click()
    await expect(shell.menuItem('Журнал операций')).toBeVisible()
    await shell.menuItem('Журнал операций').click()

    await expect(page).toHaveURL(/\/journal$/)
    await expect(page.getByRole('heading', { name: 'Журнал операций', exact: true })).toBeVisible()
  })

  for (const zoom of [80, 150, 200]) {
    test(`A3 Масштаб страницы ${zoom}%, горизонтальной прокрутки нет`, async ({ data, signInAsAdmin, page }) => {
      test.slow()
      expect(data.mainCabinet).toBeTruthy()
      await page.setViewportSize({ width: Math.round(1440 / (zoom / 100)), height: Math.round(900 / (zoom / 100)) })
      await signInAsAdmin()

      for (const panelPage of panelPages) {
        await page.goto(panelPage.path)
        await expect(page.getByRole('heading', { name: panelPage.heading, exact: true })).toBeVisible()
        await page.waitForLoadState('networkidle')
        expect(await hasHorizontalScroll(page), `${panelPage.name}: горизонтальная прокрутка`).toBe(false)
      }
    })
  }

  test('V4 Заголовки карточек не перекрываются кнопками на узком экране', async ({ data, signInAsAdmin, storagePage, page }) => {
    expect(data.mainCabinet).toBeTruthy()
    await page.setViewportSize({ width: 375, height: 900 })
    await signInAsAdmin()
    await storagePage.open()

    const card = storagePage.cabinetCard(data.bigCabinet)
    const title = await card.locator('.ant-card-head-title').boundingBox()
    const extra = await card.locator('.ant-card-extra').boundingBox()
    expect(title).not.toBeNull()
    expect(extra).not.toBeNull()

    const apart =
      (title as { x: number; width: number }).x + (title as { width: number }).width <= (extra as { x: number }).x + 1 ||
      (title as { y: number; height: number }).y + (title as { height: number }).height <= (extra as { y: number }).y + 1
    expect(apart, 'заголовок карточки перекрыт кнопками').toBe(true)
    expect(await card.locator('.ant-card-head-title').evaluate((element) => element.scrollWidth <= element.clientWidth + 1)).toBe(true)
  })

  test('I1 Кнопки доступны для нажатия и не перекрыты', async ({ data, signInAsAdmin, storagePage, page, shell }) => {
    expect(data.mainCabinet).toBeTruthy()
    await page.setViewportSize({ width: 1440, height: 900 })
    await signInAsAdmin()
    await storagePage.open()

    const addCabinet = page.getByRole('button', { name: 'Добавить шкаф' })
    await expect(addCabinet).toBeEnabled()
    await addCabinet.click({ trial: true })
    await expect(storagePage.tile(data.mainCells[0]).getByRole('button', { name: 'Пополнить' })).toBeEnabled()
    await storagePage.tile(data.mainCells[0]).getByRole('button', { name: 'Пополнить' }).click({ trial: true })

    await addCabinet.click()
    await expect(shell.dialog).toBeVisible()
    await page.keyboard.press('Escape')
    await expect(shell.dialog).toBeHidden()
  })

  test('I2 Фокус устанавливается в поля форм', async ({ loginPage, page }) => {
    await loginPage.open()

    await loginPage.emailInput.click()
    await expect(loginPage.emailInput).toBeFocused()

    await page.keyboard.press('Tab')
    await expect(loginPage.passwordInput).toBeFocused()
  })

  test('I3 Элементы реагируют на наведение', async ({ signInAsAdmin, storagePage, page, isMobile }) => {
    test.skip(isMobile, 'на сенсорном экране наведения нет')
    await page.setViewportSize({ width: 1440, height: 900 })
    await signInAsAdmin()
    await storagePage.open()

    const addCabinet = page.getByRole('button', { name: 'Добавить шкаф' })
    const visualState = (locator: Locator) =>
      locator.evaluate((element) => {
        const style = getComputedStyle(element)
        return [style.backgroundColor, style.color, style.borderColor, style.boxShadow].join('|')
      })

    const steadyState = async (locator: Locator) => {
      let previous = ''
      await expect
        .poll(async () => {
          const current = await visualState(locator)
          const steady = current === previous
          previous = current
          return steady
        })
        .toBe(true)
      return previous
    }

    await page.mouse.move(720, 450)
    const before = await steadyState(addCabinet)
    await addCabinet.hover()
    await expect.poll(() => visualState(addCabinet)).not.toBe(before)

    const menuItem = page.getByRole('menuitem', { name: 'Журнал операций' })
    await page.mouse.move(720, 450)
    const menuBefore = await steadyState(menuItem)
    await menuItem.hover()
    await expect.poll(() => visualState(menuItem)).not.toBe(menuBefore)
  })

  test('I4 Логотип ведёт на главную страницу', async ({ signInAsAdmin, storagePage, page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await signInAsAdmin()
    await storagePage.open()

    await page.locator('.brand').click()

    await expect(page).toHaveURL(/\/dashboard$/)
  })

  test('I5 Название в мобильной шапке ведёт на главную страницу', async ({ signInAsAdmin, page }) => {
    await page.setViewportSize({ width: 768, height: 900 })
    await signInAsAdmin()
    await page.goto('/journal')
    await expect(page.getByRole('heading', { name: 'Журнал операций', exact: true })).toBeVisible()

    await page.locator('.mobile-bar').getByText('SmartStorage').click()

    await expect(page).toHaveURL(/\/dashboard$/)
  })

  test('D1 Очень длинные и очень короткие строки не ломают вёрстку', async ({ data, signInAsAdmin, storagePage, usersPage, page, isMobile }) => {
    await page.setViewportSize(isMobile ? { width: 412, height: 915 } : { width: 1440, height: 900 })
    await signInAsAdmin()
    await storagePage.open()

    const longTile = storagePage.tile(data.longItemCell)
    await expect(longTile).toBeVisible()
    const tileBox = await longTile.boundingBox()
    const nameBox = await longTile.locator('.cell-item').boundingBox()
    expect((nameBox as { x: number; width: number }).x + (nameBox as { width: number }).width).toBeLessThanOrEqual(
      (tileBox as { x: number; width: number }).x + (tileBox as { width: number }).width + 1,
    )
    expect(await hasHorizontalScroll(page)).toBe(false)
    await expect(storagePage.tile(data.shortItemCell).getByText(data.shortItemName, { exact: true })).toBeVisible()

    await usersPage.open()
    await expect(usersPage.row(data.longUserName.slice(0, 30))).toBeVisible()
    expect(await hasHorizontalScroll(page)).toBe(false)
  })

  test('D2 Пустые списки показывают пояснение', async ({ data, signInAsAdmin, storagePage, usersPage, journalPage, page }) => {
    await page.setViewportSize({ width: 1440, height: 900 })
    await signInAsAdmin()
    await storagePage.open()
    await expect(storagePage.cabinetCard(data.emptyCabinet).getByText('В шкафу нет ячеек')).toBeVisible()

    await storagePage.searchInput.fill(`нет-такого-${randomUUID()}`)
    await expect(page.getByText('Ничего не найдено').first()).toBeVisible()

    await usersPage.open()
    await usersPage.search(`нет-такого-${randomUUID()}`)
    await expect(page.getByText('Никого не найдено')).toBeVisible()

    await journalPage.open()
    await journalPage.filterByItem(data.idleItemName)
    await expect(journalPage.emptyState).toBeVisible()
  })

  test('D3 Большое число элементов размещается без поломки', async ({ data, signInAsAdmin, storagePage, journalPage, page, isMobile }) => {
    await page.setViewportSize(isMobile ? { width: 412, height: 915 } : { width: 1440, height: 900 })
    await signInAsAdmin()
    await storagePage.open()

    const bigCabinet = storagePage.cabinetCard(data.bigCabinet)
    await expect(bigCabinet.locator('.cell-tile')).toHaveCount(40)
    const rows = new Set((await boxesOf(bigCabinet.locator('.cell-tile'))).map((box) => box.top))
    expect(rows.size).toBeGreaterThan(1)
    expect(await hasHorizontalScroll(page)).toBe(false)

    await journalPage.open()
    await journalPage.filterByItem(data.journalItemName)
    await journalPage.expectTotal(61)
    await expect(page.locator('.ant-pagination')).toBeVisible()
    await page.locator('.ant-pagination-item-2').click()
    await expect(journalPage.rows).toHaveCount(20)
    expect(await hasHorizontalScroll(page)).toBe(false)
  })

  test('S1 Снимки страниц', async ({ data, signInAsAdmin, page }, testInfo) => {
    test.skip(!takeScreenshots, 'снимки делаются при COMPAT_SHOTS=1')
    expect(data.mainCabinet).toBeTruthy()
    const mobile = testInfo.project.name === 'mobile'
    await page.setViewportSize(mobile ? { width: 412, height: 915 } : { width: 1440, height: 900 })
    await signInAsAdmin()

    for (const panelPage of panelPages) {
      await page.goto(panelPage.path)
      await expect(page.getByRole('heading', { name: panelPage.heading, exact: true })).toBeVisible()
      await page.waitForLoadState('networkidle')
      await page.screenshot({ path: path.join(screenshotDirectory, testInfo.project.name, `${panelPage.path.slice(1)}.png`) })
    }
  })
})
