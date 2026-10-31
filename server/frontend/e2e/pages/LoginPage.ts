import { expect, type Locator, type Page } from '@playwright/test'

export class LoginPage {
  constructor(readonly page: Page) {}

  get emailInput(): Locator {
    return this.page.getByLabel('Email')
  }

  get passwordInput(): Locator {
    return this.page.getByLabel('Пароль')
  }

  get submitButton(): Locator {
    return this.page.getByRole('button', { name: 'Войти' })
  }

  get errorAlert(): Locator {
    return this.page.locator('.ant-alert-error')
  }

  async open() {
    await this.page.goto('/login')
    await expect(this.submitButton).toBeVisible()
  }

  async signIn(email: string, password: string) {
    await this.emailInput.fill(email)
    await this.passwordInput.fill(password)
    await this.submitButton.click()
  }

  async expectSignedIn() {
    await expect(this.page).toHaveURL(/\/dashboard$/)
    await expect(this.page.getByRole('heading', { name: 'Сводка', exact: true })).toBeVisible()
  }

  async expectOnLoginPage() {
    await expect(this.page).toHaveURL(/\/login$/)
    await expect(this.submitButton).toBeVisible()
  }
}
