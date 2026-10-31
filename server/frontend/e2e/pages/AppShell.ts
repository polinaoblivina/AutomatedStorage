import { expect, type Locator, type Page } from '@playwright/test'

export class AppShell {
  constructor(readonly page: Page) {}

  menuItem(name: string): Locator {
    return this.page.getByRole('menuitem', { name })
  }

  async open(name: string, heading: string) {
    const width = this.page.viewportSize()?.width ?? 1440
    if (width < 992) await this.page.getByRole('button', { name: 'Меню' }).click()
    await this.menuItem(name).click()
    await expect(this.page.getByRole('heading', { name: heading, exact: true })).toBeVisible()
  }

  get userName(): Locator {
    return this.page.locator('.sider-user-name')
  }

  get logoutButton(): Locator {
    return this.page.getByRole('button', { name: 'Выйти' })
  }

  async logout() {
    await this.logoutButton.click()
  }

  get openDropdown(): Locator {
    return this.page.locator('.ant-dropdown:not(.ant-dropdown-hidden)').last()
  }

  async openMenu(trigger: Locator) {
    await trigger.click()
    await expect(this.openDropdown).toBeVisible()
    await expect(this.openDropdown).not.toHaveClass(/ant-slide-up-(appear|enter|leave)/)
  }

  async chooseMenuItem(trigger: Locator, action: string) {
    await this.openMenu(trigger)
    await this.openDropdown.getByRole('menuitem', { name: action }).click()
  }

  get toast(): Locator {
    return this.page.locator('.ant-message-notice')
  }

  get errorToast(): Locator {
    return this.page.locator('.ant-message-error')
  }

  get dialog(): Locator {
    return this.page.getByRole('dialog')
  }

  async saveDialog() {
    await this.dialog.getByRole('button', { name: 'Сохранить' }).click()
  }

  async confirm() {
    await this.page.getByRole('button', { name: 'Да' }).click()
  }

  async chooseOption(label: string) {
    await this.page.locator('.ant-select-item-option', { hasText: label }).click()
  }
}
