import { useCallback, useEffect, useState } from 'react'
import { Alert, App, Button, Card, Empty, Flex, Form, Input, InputNumber, Modal, Popconfirm, Table, Tag, Typography } from 'antd'
import { api } from '../api/client'
import type { Cabinet, Cell, CellItem } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { can } from '../auth/can'
import { errorText, formatGrams } from '../utils/format'

type Dialog =
  | { kind: 'cabinet' }
  | { kind: 'cell'; cabinet: Cabinet }
  | { kind: 'item'; cell: Cell }
  | { kind: 'replenish'; item: CellItem }
  | { kind: 'adjust'; item: CellItem }

interface DialogForm {
  name?: string
  location?: string
  unitWeightGrams?: number
  minQuantity?: number
  quantity?: number
}

const dialogTitles: Record<Dialog['kind'], string> = {
  cabinet: 'Новый шкаф',
  cell: 'Новая ячейка',
  item: 'Положить предмет в ячейку',
  replenish: 'Пополнить запас',
  adjust: 'Инвентаризация: сколько штук на самом деле',
}

export function StoragePage() {
  const { user } = useAuth()
  const { message } = App.useApp()
  const [form] = Form.useForm<DialogForm>()
  const [cabinets, setCabinets] = useState<Cabinet[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dialog, setDialog] = useState<Dialog | null>(null)
  const [saving, setSaving] = useState(false)

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

  async function submitDialog(values: DialogForm) {
    if (!dialog) return
    setSaving(true)

    const done = await run(() => {
      switch (dialog.kind) {
        case 'cabinet':
          return api('/cabinets', { method: 'POST', body: { name: values.name, location: values.location } })
        case 'cell':
          return api(`/cabinets/${dialog.cabinet.id}/cells`, { method: 'POST', body: { name: values.name } })
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
        case 'replenish':
          return api(`/items/${dialog.item.id}/replenish`, { method: 'POST', body: { quantity: values.quantity } })
        case 'adjust':
          return api(`/items/${dialog.item.id}/adjust`, { method: 'POST', body: { actualQuantity: values.quantity } })
      }
    }, 'Сохранено')

    setSaving(false)
    if (done) setDialog(null)
  }

  function cellActions(cabinet: Cabinet, cell: Cell) {
    const item = cell.item
    return (
      <Flex gap={8} wrap>
        {!item && canManageStock && (
          <Button size="small" onClick={() => setDialog({ kind: 'item', cell })}>
            Положить предмет
          </Button>
        )}
        {item && canManageStock && (
          <>
            <Button size="small" onClick={() => setDialog({ kind: 'replenish', item })}>
              Пополнить
            </Button>
            <Button size="small" onClick={() => setDialog({ kind: 'adjust', item })}>
              Пересчитать
            </Button>
            <Popconfirm
              title="Убрать предмет из ячейки?"
              onConfirm={() => run(() => api(`/items/${item.id}`, { method: 'DELETE' }), 'Предмет убран')}
            >
              <Button size="small" danger>
                Убрать
              </Button>
            </Popconfirm>
          </>
        )}
        {!item && canManageSystem && (
          <Popconfirm
            title="Удалить ячейку?"
            onConfirm={() =>
              run(() => api(`/cabinets/${cabinet.id}/cells/${cell.id}`, { method: 'DELETE' }), 'Ячейка удалена')
            }
          >
            <Button size="small" danger>
              Удалить ячейку
            </Button>
          </Popconfirm>
        )}
      </Flex>
    )
  }

  return (
    <Flex vertical gap={16}>
      <Flex align="center" justify="space-between">
        <Typography.Title level={3} style={{ margin: 0 }}>
          Склад
        </Typography.Title>
        {canManageSystem && (
          <Button type="primary" onClick={() => setDialog({ kind: 'cabinet' })}>
            Добавить шкаф
          </Button>
        )}
      </Flex>

      {error && <Alert type="error" title={error} />}
      {!loading && cabinets.length === 0 && <Empty description="Шкафов пока нет" />}

      {cabinets.map((cabinet) => (
        <Card
          key={cabinet.id}
          title={`${cabinet.name} — ${cabinet.location}`}
          extra={
            canManageSystem && (
              <Flex gap={8}>
                <Button size="small" onClick={() => setDialog({ kind: 'cell', cabinet })}>
                  Добавить ячейку
                </Button>
                <Popconfirm
                  title="Удалить шкаф?"
                  onConfirm={() => run(() => api(`/cabinets/${cabinet.id}`, { method: 'DELETE' }), 'Шкаф удалён')}
                >
                  <Button size="small" danger>
                    Удалить шкаф
                  </Button>
                </Popconfirm>
              </Flex>
            )
          }
        >
          <Table<Cell>
            rowKey="id"
            dataSource={cabinet.cells}
            pagination={false}
            size="small"
            locale={{ emptyText: 'В шкафу нет ячеек' }}
            columns={[
              { title: 'Ячейка', dataIndex: 'name' },
              { title: 'Предмет', render: (_, cell) => cell.item?.name ?? <Tag>пусто</Tag> },
              {
                title: 'Остаток',
                render: (_, cell) =>
                  cell.item && (
                    <>
                      {cell.item.stockQuantity} шт. {cell.item.isLowStock && <Tag color="red">мало</Tag>}
                    </>
                  ),
              },
              { title: 'Минимум', render: (_, cell) => cell.item?.minQuantity },
              { title: 'Вес 1 шт.', render: (_, cell) => cell.item && formatGrams(cell.item.unitWeightGrams) },
              { title: '', render: (_, cell) => cellActions(cabinet, cell) },
            ]}
          />
        </Card>
      ))}

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
        <Form<DialogForm> form={form} layout="vertical" preserve={false} onFinish={submitDialog}>
          {(dialog?.kind === 'cabinet' || dialog?.kind === 'cell' || dialog?.kind === 'item') && (
            <Form.Item name="name" label="Название" rules={[{ required: true, message: 'Введите название' }]}>
              <Input />
            </Form.Item>
          )}
          {dialog?.kind === 'cabinet' && (
            <Form.Item name="location" label="Где стоит" rules={[{ required: true, message: 'Введите место' }]}>
              <Input />
            </Form.Item>
          )}
          {dialog?.kind === 'item' && (
            <>
              <Form.Item
                name="unitWeightGrams"
                label="Вес одной штуки, г"
                rules={[{ required: true, message: 'Введите вес' }]}
              >
                <InputNumber min={0.1} step={0.1} style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="minQuantity" label="Минимальный остаток, шт." initialValue={0}>
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </>
          )}
          {(dialog?.kind === 'replenish' || dialog?.kind === 'adjust') && (
            <Form.Item
              name="quantity"
              label={dialog.kind === 'replenish' ? 'Сколько штук добавить' : 'Сколько штук в ячейке сейчас'}
              rules={[{ required: true, message: 'Введите количество' }]}
            >
              <InputNumber min={dialog.kind === 'replenish' ? 1 : 0} style={{ width: '100%' }} />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </Flex>
  )
}
