import { expect, type Locator, type Page } from '@playwright/test'
import { AppShell } from './AppShell'

export class DevicesPage {
  private readonly shell: AppShell

  constructor(readonly page: Page) {
    this.shell = new AppShell(page)
  }

  async open() {
    await this.shell.open('Шкафы на связи', 'Шкафы на связи')
    await expect(this.liveIndicator).toBeVisible()
  }

  get liveIndicator(): Locator {
    return this.page.getByText('Обновляется вживую')
  }

  get deviceCards(): Locator {
    return this.page.locator('.ant-card').filter({ hasText: 'Считыватель карт' })
  }

  get onlineBadge(): Locator {
    return this.deviceCards.first().getByText('в сети', { exact: true })
  }

  eventRow(text: string): Locator {
    return this.page.locator('tr.ant-table-row', { hasText: text })
  }
}
