import { expect, type Locator, type Page } from '@playwright/test'
import { AppShell } from './AppShell'

interface ItemForm {
  name: string
  unitWeightGrams: number
  minQuantity?: number
}

export class StoragePage {
  private readonly shell: AppShell

  constructor(readonly page: Page) {
    this.shell = new AppShell(page)
  }

  async open() {
    await this.shell.open('Ячейки и товары', 'Ячейки и товары')
  }

  cabinetCard(name: string): Locator {
    return this.page.locator('.ant-card', { has: this.page.locator('.ant-card-head-title', { hasText: name }) })
  }

  tile(cellName: string): Locator {
    return this.page.locator('.cell-tile', { has: this.page.locator('.cell-name', { hasText: cellName }) })
  }

  stock(cellName: string): Locator {
    return this.tile(cellName).locator('.cell-stock')
  }

  get dialog(): Locator {
    return this.shell.dialog
  }

  get searchInput(): Locator {
    return this.page.getByPlaceholder('Поиск по товару или ячейке')
  }

  async filterView(label: string) {
    await this.page.locator('.ant-segmented').getByText(label).click()
  }

  async addCabinet(name: string, location: string) {
    await this.page.getByRole('button', { name: 'Добавить шкаф' }).click()
    await this.dialog.getByLabel('Название').fill(name)
    await this.dialog.getByLabel('Где стоит').fill(location)
    await this.shell.saveDialog()
    await expect(this.dialog).toBeHidden()
    await expect(this.cabinetCard(name)).toBeVisible()
  }

  async addCell(cabinetName: string, cellName: string) {
    await this.cabinetCard(cabinetName).getByRole('button', { name: 'Добавить ячейку' }).click()
    await this.dialog.getByLabel('Название').fill(cellName)
    await this.shell.saveDialog()
    await expect(this.dialog).toBeHidden()
    await expect(this.tile(cellName)).toBeVisible()
  }

  async placeItem(cellName: string, item: ItemForm) {
    await this.tile(cellName).getByRole('button', { name: 'Положить товар' }).click()
    await this.dialog.getByLabel('Название').fill(item.name)
    await this.dialog.getByLabel('Вес одной штуки, г').fill(String(item.unitWeightGrams))
    await this.dialog.getByLabel('Минимальный остаток, шт.').fill(String(item.minQuantity ?? 0))
    await this.shell.saveDialog()
    await expect(this.dialog).toBeHidden()
    await expect(this.tile(cellName).getByText(item.name)).toBeVisible()
  }

  async startReplenish(cellName: string, quantity: number) {
    await this.tile(cellName).getByRole('button', { name: 'Пополнить' }).click()
    await this.dialog.getByLabel('Сколько штук добавить').fill(String(quantity))
    await this.shell.saveDialog()
  }

  async replenish(cellName: string, quantity: number) {
    await this.startReplenish(cellName, quantity)
    await expect(this.dialog).toBeHidden()
  }

  private async openCellMenu(cellName: string, action: string) {
    await this.shell.chooseMenuItem(this.tile(cellName).getByRole('button', { name: 'Ещё' }), action)
  }

  async startAdjust(cellName: string, quantity: number) {
    await this.openCellMenu(cellName, 'Пересчитать')
    await this.dialog.getByLabel('Сколько штук в ячейке на самом деле').fill(String(quantity))
    await this.shell.saveDialog()
  }

  async adjust(cellName: string, quantity: number) {
    await this.startAdjust(cellName, quantity)
    await expect(this.dialog).toBeHidden()
  }

  async move(cellName: string, targetLabel: string) {
    await this.openCellMenu(cellName, 'Переместить')
    await this.dialog.getByLabel('Свободная ячейка').click()
    await this.page.keyboard.type(targetLabel)
    await this.shell.chooseOption(targetLabel)
    await this.shell.saveDialog()
    await expect(this.dialog).toBeHidden()
  }

  async removeItem(cellName: string) {
    await this.openCellMenu(cellName, 'Убрать из ячейки')
    await this.shell.confirm()
  }

  async deleteCell(cellName: string) {
    await this.openCellMenu(cellName, 'Удалить ячейку')
    await this.shell.confirm()
  }

  async deleteCabinet(cabinetName: string) {
    await this.shell.chooseMenuItem(this.cabinetCard(cabinetName).getByRole('button', { name: 'Ещё' }).first(), 'Удалить шкаф')
    await this.shell.confirm()
  }

  menuItem(action: string): Locator {
    return this.shell.openDropdown.getByRole('menuitem', { name: action })
  }

  async openMenuOf(cellName: string) {
    await this.shell.openMenu(this.tile(cellName).getByRole('button', { name: 'Ещё' }))
  }
}
