import { expect, type Locator, type Page } from '@playwright/test'
import { AppShell } from './AppShell'

interface NewUser {
  fullName: string
  role: string
  cardUid?: string
  email?: string
  password?: string
}

export class UsersPage {
  private readonly shell: AppShell

  constructor(readonly page: Page) {
    this.shell = new AppShell(page)
  }

  async open() {
    await this.shell.open('Пользователи', 'Пользователи')
  }

  row(fullName: string): Locator {
    return this.page.locator('tr.ant-table-row', { hasText: fullName })
  }

  get dialog(): Locator {
    return this.shell.dialog
  }

  get searchInput(): Locator {
    return this.page.getByPlaceholder('Поиск по имени, email или карте')
  }

  get forbidden(): Locator {
    return this.page.getByText('Нет доступа')
  }

  async search(text: string) {
    await this.searchInput.fill(text)
  }

  async filterStatus(label: string) {
    await this.page.locator('.ant-segmented').getByText(label, { exact: true }).click()
  }

  async add(user: NewUser) {
    await this.page.getByRole('button', { name: 'Добавить сотрудника' }).click()
    await this.dialog.getByLabel('ФИО').fill(user.fullName)
    await this.dialog.getByRole('combobox', { name: 'Роль' }).click()
    await this.shell.chooseOption(user.role)
    if (user.cardUid) await this.dialog.getByLabel('UID карты').fill(user.cardUid)
    if (user.email) await this.dialog.getByLabel('Email для входа в панель').fill(user.email)
    if (user.password) await this.dialog.locator('input[type=password]').fill(user.password)
    await this.shell.saveDialog()
  }

  async chooseAction(fullName: string, action: string) {
    await this.shell.chooseMenuItem(this.row(fullName).getByRole('button', { name: 'Действия' }), action)
  }

  async assignCard(fullName: string, cardUid: string) {
    await this.chooseAction(fullName, 'Привязать карту')
    await this.dialog.getByLabel('UID карты').fill(cardUid)
    await this.shell.saveDialog()
    await expect(this.dialog).toBeHidden()
  }

  async removeCard(fullName: string) {
    await this.chooseAction(fullName, 'Отвязать карту')
    await this.shell.confirm()
  }

  async deactivate(fullName: string) {
    await this.chooseAction(fullName, 'Отключить')
    await this.shell.confirm()
  }

  async activate(fullName: string) {
    await this.chooseAction(fullName, 'Включить')
  }

  async changeRole(fullName: string, role: string) {
    await this.chooseAction(fullName, 'Сменить роль')
    await this.dialog.getByRole('combobox', { name: 'Роль' }).click()
    await this.shell.chooseOption(role)
    await this.shell.saveDialog()
    await expect(this.dialog).toBeHidden()
  }

  async rename(fullName: string, newName: string) {
    await this.chooseAction(fullName, 'Изменить ФИО')
    await this.dialog.getByLabel('ФИО').fill(newName)
    await this.shell.saveDialog()
    await expect(this.dialog).toBeHidden()
  }
}
