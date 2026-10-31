import { credentials } from '../support/env'
import { expect, test } from '../fixtures'

test.describe('Вход в панель', () => {
  test('администратор входит с верными данными', async ({ loginPage, signInAsAdmin, shell }) => {
    await signInAsAdmin()

    await loginPage.expectSignedIn()
    await expect(shell.userName).toHaveText('Администратор')
  })

  test('неверный пароль показывает ошибку и оставляет на странице входа', async ({ loginPage }) => {
    const { adminEmail } = credentials()

    await loginPage.open()
    await loginPage.signIn(adminEmail, 'WrongPass-1')

    await expect(loginPage.errorAlert).toBeVisible()
    await loginPage.expectOnLoginPage()
  })

  test('пустая форма показывает сообщения об обязательных полях', async ({ loginPage, page }) => {
    await loginPage.open()
    await loginPage.submitButton.click()

    await expect(page.getByText('Введите email')).toBeVisible()
    await expect(page.getByText('Введите пароль')).toBeVisible()
  })

  test('отключённый сотрудник не может войти', async ({ api, unique, loginPage }) => {
    const email = `${unique('off')}@test.local`
    const user = await api.createUser({ fullName: unique('Отключён'), role: 'Storekeeper', email, password: 'Secret123' })
    await api.deactivateUser(user.id)

    await loginPage.open()
    await loginPage.signIn(email, 'Secret123')

    await expect(loginPage.errorAlert).toBeVisible()
    await loginPage.expectOnLoginPage()
  })

  test('выход возвращает на страницу входа и закрывает разделы', async ({ loginPage, signInAsAdmin, shell, page }) => {
    await signInAsAdmin()

    await shell.logout()
    await loginPage.expectOnLoginPage()

    await page.goto('/storage')
    await loginPage.expectOnLoginPage()
  })

  test('вход сразу после открытия страницы не отменяется запоздавшим ответом проверки сессии', async ({ page, loginPage, signInAsAdmin, shell }) => {
    let staleResponseSent: () => void = () => undefined
    const staleResponse = new Promise<void>((resolve) => {
      staleResponseSent = resolve
    })
    await page.route('**/api/auth/refresh', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 1500))
      await route.fulfill({ status: 401, contentType: 'application/json', body: '{}' })
      staleResponseSent()
    })

    await signInAsAdmin()
    await staleResponse
    await page.waitForLoadState('networkidle')

    await loginPage.expectSignedIn()
    await expect(shell.userName).toHaveText('Администратор')
  })

  for (const path of ['/dashboard', '/devices', '/storage', '/journal', '/users']) {
    test(`адрес ${path} без входа ведёт на страницу входа`, async ({ page, loginPage }) => {
      await page.goto(path)

      await loginPage.expectOnLoginPage()
    })
  }
})
