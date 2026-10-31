import { expect, type Locator, type Page } from '@playwright/test'
import { AppShell } from './AppShell'

export class DashboardPage {
  private readonly shell: AppShell

  constructor(readonly page: Page) {
    this.shell = new AppShell(page)
  }

  async open() {
    await this.shell.open('Сводка', 'Сводка')
    await expect(this.statCard('Товаров')).toBeVisible()
  }

  statCard(label: string): Locator {
    return this.page.locator('.stat-card', { has: this.page.locator('.stat-label', { hasText: new RegExp(`^${label}$`) }) })
  }

  statValue(label: string): Locator {
    return this.statCard(label).locator('.stat-value')
  }

  lowStockCard(): Locator {
    return this.page.locator('.ant-card', { has: this.page.locator('.ant-card-head-title', { hasText: 'Заканчиваются' }) })
  }
}
