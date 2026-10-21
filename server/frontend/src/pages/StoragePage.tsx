import { useCallback, useEffect, useState } from 'react'
import {
  Alert,
  App,
  Button,
  Card,
  Dropdown,
  Empty,
  Flex,
  Form,
  Input,
  InputNumber,
  Modal,
  Progress,
  Segmented,
  Select,
  Spin,
  Tag,
} from 'antd'
import type { MenuProps } from 'antd'
import { api } from '../api/client'
import type { Cabinet, Cell, CellItem } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { can } from '../auth/can'
import { PageHeader } from '../components/PageHeader'
import { colors } from '../theme'
import { errorText, formatGrams } from '../utils/format'

type Dialog =
  | { kind: 'cabinet' }
  | { kind: 'editCabinet'; cabinet: Cabinet }
  | { kind: 'cell'; cabinet: Cabinet }
  | { kind: 'renameCell'; cabinet: Cabinet; cell: Cell }
  | { kind: 'item'; cell: Cell }
  | { kind: 'editItem'; item: CellItem }
  | { kind: 'move'; item: CellItem }
  | { kind: 'replenish'; item: CellItem }
  | { kind: 'adjust'; item: CellItem }

interface DialogForm {
  name?: string
  location?: string
  unitWeightGrams?: number
  minQuantity?: number
  quantity?: number
  cellId?: string
}

type View = 'all' | 'low' | 'empty'

const dialogTitles: Record<Dialog['kind'], string> = {
  cabinet: 'Новый шкаф',
  editCabinet: 'Изменить шкаф',
  cell: 'Новая ячейка',
  renameCell: 'Переименовать ячейку',
  item: 'Положить товар в ячейку',
  editItem: 'Изменить товар',
  move: 'Переместить товар в другую ячейку',
  replenish: 'Пополнить запас',
  adjust: 'Инвентаризация',
}

function initialValues(dialog: Dialog | null): DialogForm {
  switch (dialog?.kind) {
    case 'editCabinet':
      return { name: dialog.cabinet.name, location: dialog.cabinet.location }
    case 'renameCell':
      return { name: dialog.cell.name }
    case 'editItem':
      return { name: dialog.item.name, unitWeightGrams: dialog.item.unitWeightGrams, minQuantity: dialog.item.minQuantity }
    case 'adjust':
      return { quantity: dialog.item.stockQuantity }
    case 'item':
      return { minQuantity: 0 }
    default:
      return {}
  }
}

function stockPercent(item: CellItem) {
  if (item.minQuantity <= 0) return item.stockQuantity > 0 ? 100 : 0
  return Math.min(100, (item.stockQuantity / (item.minQuantity * 2)) * 100)
}

function matches(cell: Cell, view: View, search: string) {
  if (view === 'low' && !cell.item?.isLowStock) return false
  if (view === 'empty' && cell.item) return false
  if (!search) return true
  const text = `${cell.name} ${cell.item?.name ?? ''}`.toLowerCase()
  return text.includes(search.toLowerCase())
}

export function StoragePage() {
  const { user } = useAuth()
  const { message, modal } = App.useApp()
  const [form] = Form.useForm<DialogForm>()
  const [cabinets, setCabinets] = useState<Cabinet[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dialog, setDialog] = useState<Dialog | null>(null)
  const [saving, setSaving] = useState(false)
  const [view, setView] = useState<View>('all')
  const [search, setSearch] = useState('')

  const canManageSystem = can(user, 'ManageSystem')
  const canManageStock = can(user, 'ManageStock')

  const load = useCallback(() => {
    api<Cabinet[]>('/cabinets')
      .then((list) => {
        setCabinets(list)
        setError(null)
      })
      .catch((e) => setError(errorText(e)))
      .finally(() => setLoading(false))
  }, [])

  useEffect(load, [load])

  async function run(action: () => Promise<unknown>, success: string) {
    try {
      await action()
      message.success(success)
      load()
      return true
    } catch (e) {
      message.error(errorText(e))
      return false
    }
  }

  function confirm(title: string, content: string, action: () => Promise<unknown>, success: string) {
    modal.confirm({
      title,
      content,
      okText: 'Да',
      okButtonProps: { danger: true },
      cancelText: 'Отмена',
      onOk: () => run(action, success),
    })
  }

  async function submitDialog(values: DialogForm) {
    if (!dialog) return
    setSaving(true)

    const done = await run(() => {
      switch (dialog.kind) {
        case 'cabinet':
          return api('/cabinets', { method: 'POST', body: { name: values.name, location: values.location } })
        case 'editCabinet':
          return api(`/cabinets/${dialog.cabinet.id}`, {
            method: 'PUT',
            body: { name: values.name, location: values.location },
          })
        case 'cell':
          return api(`/cabinets/${dialog.cabinet.id}/cells`, { method: 'POST', body: { name: values.name } })
        case 'renameCell':
          return api(`/cabinets/${dialog.cabinet.id}/cells/${dialog.cell.id}`, {
            method: 'PUT',
            body: { name: values.name },
          })
        case 'item':
          return api('/items', {
            method: 'POST',
            body: {
              cellId: dialog.cell.id,
              name: values.name,
              unitWeightGrams: values.unitWeightGrams,
              minQuantity: values.minQuantity ?? 0,
            },
          })
        case 'editItem':
          return api(`/items/${dialog.item.id}`, {
            method: 'PUT',
            body: { name: values.name, unitWeightGrams: values.unitWeightGrams, minQuantity: values.minQuantity ?? 0 },
          })
        case 'move':
          return api(`/items/${dialog.item.id}/move`, { method: 'POST', body: { cellId: values.cellId } })
        case 'replenish':
          return api(`/items/${dialog.item.id}/replenish`, { method: 'POST', body: { quantity: values.quantity } })
        case 'adjust':
          return api(`/items/${dialog.item.id}/adjust`, { method: 'POST', body: { actualQuantity: values.quantity } })
      }
    }, 'Сохранено')

    setSaving(false)
    if (done) setDialog(null)
  }

  async function removeItem(item: CellItem) {
    const result = await api<{ archived: boolean }>(`/items/${item.id}`, { method: 'DELETE' })
    if (result?.archived) message.info('У товара есть история операций, поэтому он перенесён в архив')
  }

  function cellMenu(cabinet: Cabinet, cell: Cell): MenuProps['items'] {
    const item = cell.item
    const entries: MenuProps['items'] = []

    if (item && canManageStock) {
      entries.push(
        { key: 'adjust', label: 'Пересчитать', onClick: () => setDialog({ kind: 'adjust', item }) },
        { key: 'editItem', label: 'Изменить товар', onClick: () => setDialog({ kind: 'editItem', item }) },
        { key: 'move', label: 'Переместить', onClick: () => setDialog({ kind: 'move', item }) },
        {
          key: 'remove',
          label: 'Убрать из ячейки',
          danger: true,
          onClick: () => confirm(`Убрать «${item.name}» из ячейки?`, 'Ячейка станет свободной.', () => removeItem(item), 'Товар убран'),
        },
      )
    }
    if (canManageSystem) {
      if (entries.length > 0) entries.push({ type: 'divider' })
      entries.push({
        key: 'renameCell',
        label: 'Переименовать ячейку',
        onClick: () => setDialog({ kind: 'renameCell', cabinet, cell }),
      })
      if (!item) {
        entries.push({
          key: 'removeCell',
          label: 'Удалить ячейку',
          danger: true,
          onClick: () =>
            confirm(
              `Удалить ячейку «${cell.name}»?`,
              'Ячейка исчезнет из шкафа.',
              () => api(`/cabinets/${cabinet.id}/cells/${cell.id}`, { method: 'DELETE' }),
              'Ячейка удалена',
            ),
        })
      }
    }
    return entries
  }

  function cabinetMenu(cabinet: Cabinet): MenuProps['items'] {
    return [
      { key: 'edit', label: 'Изменить шкаф', onClick: () => setDialog({ kind: 'editCabinet', cabinet }) },
      { type: 'divider' },
      {
        key: 'delete',
        label: 'Удалить шкаф',
        danger: true,
        onClick: () =>
          confirm(
            `Удалить шкаф «${cabinet.name}»?`,
            'Удалить можно только шкаф без товаров.',
            () => api(`/cabinets/${cabinet.id}`, { method: 'DELETE' }),
            'Шкаф удалён',
          ),
      },
    ]
  }

  function cellTile(cabinet: Cabinet, cell: Cell) {
    const item = cell.item
    const menu = cellMenu(cabinet, cell)
    const className = item ? (item.isLowStock ? 'cell-tile low' : 'cell-tile') : 'cell-tile empty'

    return (
      <div key={cell.id} className={className}>
        <Flex justify="space-between" align="center">
          <span className="cell-name">{cell.name}</span>
          {item?.isLowStock && <Tag color="red">мало</Tag>}
          {!item && <span className="muted">свободна</span>}
        </Flex>

        {item ? (
          <>
            <div className="cell-item">{item.name}</div>
            <Flex justify="space-between" align="baseline">
              <span className="cell-stock" style={{ color: item.isLowStock ? colors.danger : undefined }}>
                {item.stockQuantity} шт.
              </span>
              <span className="muted">мин. {item.minQuantity}</span>
            </Flex>
            <Progress
              percent={stockPercent(item)}
              showInfo={false}
              size="small"
              strokeColor={item.isLowStock ? colors.danger : colors.primary}
            />
            <span className="muted" style={{ fontSize: 12 }}>
              1 шт. = {formatGrams(item.unitWeightGrams)}, всего {formatGrams(item.unitWeightGrams * item.stockQuantity)}
            </span>
          </>
        ) : (
          <div className="cell-item muted" style={{ fontWeight: 400 }}>
            Пусто
          </div>
        )}

        <Flex gap={8} style={{ marginTop: 'auto' }}>
          {item && canManageStock && (
            <Button size="small" type="primary" ghost onClick={() => setDialog({ kind: 'replenish', item })}>
              Пополнить
            </Button>
          )}
          {!item && canManageStock && (
            <Button size="small" type="primary" ghost onClick={() => setDialog({ kind: 'item', cell })}>
              Положить товар
            </Button>
          )}
          {menu && menu.length > 0 && (
            <Dropdown menu={{ items: menu }} trigger={['click']}>
              <Button size="small">Ещё</Button>
            </Dropdown>
          )}
        </Flex>
      </div>
    )
  }

  const allCells = cabinets.flatMap((cabinet) => cabinet.cells)
  const freeCells = cabinets.flatMap((cabinet) =>
    cabinet.cells.filter((cell) => !cell.item).map((cell) => ({ value: cell.id, label: `${cabinet.name}, ${cell.name}` })),
  )
  const lowCount = allCells.filter((cell) => cell.item?.isLowStock).length
  const kind = dialog?.kind

  return (
    <>
      <PageHeader
        title="Ячейки и товары"
        subtitle={`Шкафов ${cabinets.length}, ячеек ${allCells.length}, свободно ${freeCells.length}`}
        extra={
          canManageSystem && (
            <Button type="primary" onClick={() => setDialog({ kind: 'cabinet' })}>
              Добавить шкаф
            </Button>
          )
        }
      />

      <Card style={{ marginBottom: 16 }}>
        <Flex gap={12} wrap align="center">
          <Input
            allowClear
            placeholder="Поиск по товару или ячейке"
            style={{ maxWidth: 320 }}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Segmented<View>
            value={view}
            onChange={setView}
            options={[
              { value: 'all', label: 'Все' },
              { value: 'low', label: `Заканчиваются (${lowCount})` },
              { value: 'empty', label: `Свободные (${freeCells.length})` },
            ]}
          />
        </Flex>
      </Card>

      {error && <Alert type="error" title={error} style={{ marginBottom: 16 }} />}
      {loading && <Spin size="large" style={{ display: 'block', marginTop: 80 }} />}
      {!loading && cabinets.length === 0 && (
        <Card>
          <Empty description="Шкафов пока нет" />
        </Card>
      )}

      <Flex vertical gap={16}>
        {cabinets.map((cabinet) => {
          const cells = cabinet.cells.filter((cell) => matches(cell, view, search))
          const busy = cabinet.cells.filter((cell) => cell.item).length

          return (
            <Card
              key={cabinet.id}
              title={
                <div style={{ padding: '4px 0' }}>
                  <div>{cabinet.name}</div>
                  <div className="muted" style={{ fontSize: 13, fontWeight: 400 }}>
                    {cabinet.location}, занято {busy} из {cabinet.cells.length}
                  </div>
                </div>
              }
              extra={
                canManageSystem && (
                  <Flex gap={8}>
                    <Button size="small" onClick={() => setDialog({ kind: 'cell', cabinet })}>
                      Добавить ячейку
                    </Button>
                    <Dropdown menu={{ items: cabinetMenu(cabinet) }} trigger={['click']}>
                      <Button size="small">Ещё</Button>
                    </Dropdown>
                  </Flex>
                )
              }
            >
              {cells.length === 0 ? (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description={cabinet.cells.length === 0 ? 'В шкафу нет ячеек' : 'Ничего не найдено'}
                />
              ) : (
                <div className="cell-grid">
                  {cells.map((cell) => cellTile(cabinet, cell))}
                </div>
              )}
            </Card>
          )
        })}
      </Flex>

      <Modal
        open={dialog !== null}
        title={dialog && dialogTitles[dialog.kind]}
        okText="Сохранить"
        cancelText="Отмена"
        confirmLoading={saving}
        onOk={() => form.submit()}
        onCancel={() => setDialog(null)}
        destroyOnHidden
      >
        <Form<DialogForm>
          form={form}
          layout="vertical"
          preserve={false}
          initialValues={initialValues(dialog)}
          onFinish={submitDialog}
        >
          {(kind === 'cabinet' ||
            kind === 'editCabinet' ||
            kind === 'cell' ||
            kind === 'renameCell' ||
            kind === 'item' ||
            kind === 'editItem') && (
            <Form.Item name="name" label="Название" rules={[{ required: true, message: 'Введите название' }]}>
              <Input />
            </Form.Item>
          )}
          {(kind === 'cabinet' || kind === 'editCabinet') && (
            <Form.Item name="location" label="Где стоит" rules={[{ required: true, message: 'Введите место' }]}>
              <Input />
            </Form.Item>
          )}
          {(kind === 'item' || kind === 'editItem') && (
            <>
              <Form.Item
                name="unitWeightGrams"
                label="Вес одной штуки, г"
                rules={[{ required: true, message: 'Введите вес' }]}
              >
                <InputNumber min={0.1} step={0.1} style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="minQuantity" label="Минимальный остаток, шт.">
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </>
          )}
          {kind === 'move' && (
            <Form.Item name="cellId" label="Свободная ячейка" rules={[{ required: true, message: 'Выберите ячейку' }]}>
              <Select options={freeCells} placeholder={freeCells.length === 0 ? 'Свободных ячеек нет' : 'Выберите'} />
            </Form.Item>
          )}
          {(kind === 'replenish' || kind === 'adjust') && (
            <Form.Item
              name="quantity"
              label={kind === 'replenish' ? 'Сколько штук добавить' : 'Сколько штук в ячейке на самом деле'}
              rules={[{ required: true, message: 'Введите количество' }]}
            >
              <InputNumber min={kind === 'replenish' ? 1 : 0} style={{ width: '100%' }} />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </>
  )
}
