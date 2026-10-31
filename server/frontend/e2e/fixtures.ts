import { randomUUID } from 'node:crypto'
import { test as base, expect } from '@playwright/test'
import { AppShell } from './pages/AppShell'
import { DashboardPage } from './pages/DashboardPage'
import { DevicesPage } from './pages/DevicesPage'
import { JournalPage } from './pages/JournalPage'
import { LoginPage } from './pages/LoginPage'
import { StoragePage } from './pages/StoragePage'
import { UsersPage } from './pages/UsersPage'
import { Api } from './support/api'
import { credentials } from './support/env'

interface Fixtures {
  api: Api
  unique: (prefix: string) => string
  shell: AppShell
  loginPage: LoginPage
  dashboardPage: DashboardPage
  storagePage: StoragePage
  usersPage: UsersPage
  journalPage: JournalPage
  devicesPage: DevicesPage
  signIn: (email: string, password: string) => Promise<void>
  signInAsAdmin: () => Promise<void>
}

export const test = base.extend<Fixtures>({
  api: async ({ baseURL }, provide) => {
    const api = new Api(baseURL as string)
    await api.signInAsAdmin()
    await provide(api)
  },
  unique: async ({ browserName }, provide) => {
    await provide((prefix) => `${prefix}-${browserName.slice(0, 2)}${randomUUID().slice(0, 8)}`)
  },
  shell: async ({ page }, provide) => provide(new AppShell(page)),
  loginPage: async ({ page }, provide) => provide(new LoginPage(page)),
  dashboardPage: async ({ page }, provide) => provide(new DashboardPage(page)),
  storagePage: async ({ page }, provide) => provide(new StoragePage(page)),
  usersPage: async ({ page }, provide) => provide(new UsersPage(page)),
  journalPage: async ({ page }, provide) => provide(new JournalPage(page)),
  devicesPage: async ({ page }, provide) => provide(new DevicesPage(page)),
  signIn: async ({ loginPage }, provide) => {
    await provide(async (email, password) => {
      await loginPage.open()
      await loginPage.signIn(email, password)
      await loginPage.expectSignedIn()
    })
  },
  signInAsAdmin: async ({ signIn }, provide) => {
    await provide(async () => {
      const { adminEmail, adminPassword } = credentials()
      await signIn(adminEmail, adminPassword)
    })
  },
})

export { expect }
