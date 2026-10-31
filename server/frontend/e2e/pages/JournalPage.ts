import { expect, type Locator, type Page } from '@playwright/test'
import { AppShell } from './AppShell'

export class JournalPage {
  private readonly shell: AppShell

  constructor(readonly page: Page) {
    this.shell = new AppShell(page)
  }

  async open() {
    await this.shell.open('Журнал операций', 'Журнал операций')
  }

  get rows(): Locator {
    return this.page.locator('.ant-table-tbody tr.ant-table-row')
  }

  get emptyState(): Locator {
    return this.page.getByText('Операций не найдено')
  }

  async filterByType(label: string) {
    await this.page.locator('.ant-segmented').getByText(label, { exact: true }).click()
  }

  async filterByItem(itemName: string) {
    const select = this.page.locator('.ant-select').filter({ has: this.page.locator('.ant-select-placeholder', { hasText: /^Товар$/ }) })
    await select.click()
    await this.page.keyboard.type(itemName)
    await this.shell.chooseOption(itemName)
    await expect(this.page.getByRole('button', { name: 'Сбросить' })).toBeVisible()
  }

  async expectTotal(count: number) {
    await expect(this.page.getByText(`Найдено записей: ${count}`)).toBeVisible()
  }
}
