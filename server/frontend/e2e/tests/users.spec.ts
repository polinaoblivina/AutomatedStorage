import { expect, test } from '../fixtures'

test.describe('Пользователи', () => {
  test('сотрудник создаётся с ролью и картой', async ({ signInAsAdmin, usersPage, unique }) => {
    const fullName = unique('Иванов')
    const card = unique('UI')
    await signInAsAdmin()
    await usersPage.open()

    await usersPage.add({ fullName, role: 'Employee', cardUid: card })

    await expect(usersPage.dialog).toBeHidden()
    const row = usersPage.row(fullName)
    await expect(row.getByText(card.toUpperCase())).toBeVisible()
    await expect(row.getByText('Employee')).toBeVisible()
    await expect(row.getByText('активен')).toBeVisible()
  })

  test('форма без ФИО показывает сообщение', async ({ signInAsAdmin, usersPage, shell, page }) => {
    await signInAsAdmin()
    await usersPage.open()

    await page.getByRole('button', { name: 'Добавить сотрудника' }).click()
    await shell.saveDialog()

    await expect(page.getByText('Введите ФИО')).toBeVisible()
    await expect(usersPage.dialog).toBeVisible()
  })

  test('карта привязывается и отвязывается', async ({ api, signInAsAdmin, usersPage, unique }) => {
    const user = await api.createUser({ fullName: unique('Петров'), role: 'Employee' })
    const card = unique('BIND')
    await signInAsAdmin()
    await usersPage.open()

    await usersPage.assignCard(user.fullName, card)
    await expect(usersPage.row(user.fullName).getByText(card.toUpperCase())).toBeVisible()

    await usersPage.removeCard(user.fullName)
    await expect(usersPage.row(user.fullName).getByText(card.toUpperCase())).toHaveCount(0)
    await expect(usersPage.row(user.fullName).getByText('нет')).toBeVisible()
  })

  test('сотрудник отключается и включается', async ({ api, signInAsAdmin, usersPage, unique }) => {
    const user = await api.createUser({ fullName: unique('Сидоров'), role: 'Employee' })
    await signInAsAdmin()
    await usersPage.open()

    await usersPage.deactivate(user.fullName)
    await expect(usersPage.row(user.fullName).getByText('отключён')).toBeVisible()

    await usersPage.activate(user.fullName)
    await expect(usersPage.row(user.fullName).getByText('активен')).toBeVisible()
  })

  test('роль сотрудника меняется', async ({ api, signInAsAdmin, usersPage, unique }) => {
    const user = await api.createUser({ fullName: unique('Кузнецов'), role: 'Employee' })
    await signInAsAdmin()
    await usersPage.open()

    await usersPage.changeRole(user.fullName, 'Storekeeper')

    await expect(usersPage.row(user.fullName).getByText('Storekeeper')).toBeVisible()
  })

  test('ФИО сотрудника изменяется', async ({ api, signInAsAdmin, usersPage, unique }) => {
    const user = await api.createUser({ fullName: unique('Смирнов'), role: 'Employee' })
    const newName = unique('Смирнова')
    await signInAsAdmin()
    await usersPage.open()

    await usersPage.rename(user.fullName, newName)

    await expect(usersPage.row(newName)).toBeVisible()
    await expect(usersPage.row(user.fullName)).toHaveCount(0)
  })

  test('поиск оставляет только подходящих сотрудников', async ({ api, signInAsAdmin, usersPage, unique }) => {
    const wanted = await api.createUser({ fullName: unique('Искомый'), role: 'Employee' })
    const other = await api.createUser({ fullName: unique('Посторонний'), role: 'Employee' })
    await signInAsAdmin()
    await usersPage.open()

    await usersPage.search(wanted.fullName)

    await expect(usersPage.row(wanted.fullName)).toBeVisible()
    await expect(usersPage.row(other.fullName)).toHaveCount(0)
  })

  test('кладовщик не видит раздел и получает «Нет доступа» по прямому адресу', async ({ api, unique, signIn, shell, usersPage, page }) => {
    const email = `${unique('keeper')}@test.local`
    await api.createUser({ fullName: unique('Кладовщик'), role: 'Storekeeper', email, password: 'Secret123' })
    await signIn(email, 'Secret123')

    await expect(shell.menuItem('Пользователи')).toHaveCount(0)
    await page.goto('/users')

    await expect(usersPage.forbidden).toBeVisible()
    await expect(page.getByRole('button', { name: 'Добавить сотрудника' })).toHaveCount(0)
  })
})
