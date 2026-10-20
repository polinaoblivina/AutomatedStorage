import { useCallback, useEffect, useState } from 'react'
import { Alert, App, Button, Flex, Form, Input, Modal, Popconfirm, Select, Table, Tag, Typography } from 'antd'
import { api } from '../api/client'
import type { Role, User } from '../api/types'
import { errorText } from '../utils/format'

type Dialog =
  | { kind: 'create' }
  | { kind: 'card'; user: User }
  | { kind: 'role'; user: User }
  | { kind: 'credentials'; user: User }

interface DialogForm {
  fullName?: string
  roleId?: string
  cardUid?: string
  email?: string
  password?: string
}

const dialogTitles: Record<Dialog['kind'], string> = {
  create: 'Новый сотрудник',
  card: 'Привязать карту',
  role: 'Сменить роль',
  credentials: 'Вход в веб-панель',
}

export function UsersPage() {
  const { message } = App.useApp()
  const [form] = Form.useForm<DialogForm>()
  const [users, setUsers] = useState<User[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dialog, setDialog] = useState<Dialog | null>(null)
  const [saving, setSaving] = useState(false)

  const load = useCallback(() => {
    Promise.all([api<User[]>('/users'), api<Role[]>('/roles')])
      .then(([userList, roleList]) => {
        setUsers(userList)
        setRoles(roleList)
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
        case 'create':
          return api('/users', {
            method: 'POST',
            body: {
              fullName: values.fullName,
              roleId: values.roleId,
              cardUid: values.cardUid || null,
              email: values.email || null,
              password: values.password || null,
            },
          })
        case 'card':
          return api(`/users/${dialog.user.id}/card`, { method: 'PUT', body: { cardUid: values.cardUid } })
        case 'role':
          return api(`/users/${dialog.user.id}/role`, { method: 'PUT', body: { roleId: values.roleId } })
        case 'credentials':
          return api(`/users/${dialog.user.id}/credentials`, {
            method: 'PUT',
            body: { email: values.email, password: values.password },
          })
      }
    }, 'Сохранено')

    setSaving(false)
    if (done) setDialog(null)
  }

  const roleOptions = roles.map((role) => ({ value: role.id, label: role.name }))
  const kind = dialog?.kind

  return (
    <Flex vertical gap={16}>
      <Flex align="center" justify="space-between">
        <Typography.Title level={3} style={{ margin: 0 }}>
          Пользователи
        </Typography.Title>
        <Button type="primary" onClick={() => setDialog({ kind: 'create' })}>
          Добавить сотрудника
        </Button>
      </Flex>

      {error && <Alert type="error" title={error} />}

      <Table<User>
        rowKey="id"
        loading={loading}
        dataSource={users}
        pagination={false}
        size="small"
        columns={[
          { title: 'ФИО', dataIndex: 'fullName' },
          { title: 'Роль', dataIndex: 'roleName' },
          { title: 'Email', dataIndex: 'email', render: (email) => email ?? '—' },
          { title: 'Карта', dataIndex: 'cardUid', render: (card) => card ?? '—' },
          {
            title: 'Статус',
            dataIndex: 'isActive',
            render: (active) => (active ? <Tag color="green">активен</Tag> : <Tag>отключён</Tag>),
          },
          {
            title: '',
            render: (_, user) => (
              <Flex gap={8} wrap>
                <Button size="small" onClick={() => setDialog({ kind: 'card', user })}>
                  Карта
                </Button>
                {user.cardUid && (
                  <Popconfirm
                    title="Отвязать карту?"
                    onConfirm={() => run(() => api(`/users/${user.id}/card`, { method: 'DELETE' }), 'Карта отвязана')}
                  >
                    <Button size="small">Отвязать карту</Button>
                  </Popconfirm>
                )}
                <Button size="small" onClick={() => setDialog({ kind: 'role', user })}>
                  Роль
                </Button>
                <Button size="small" onClick={() => setDialog({ kind: 'credentials', user })}>
                  Вход
                </Button>
                {user.isActive ? (
                  <Popconfirm
                    title="Отключить сотрудника?"
                    onConfirm={() =>
                      run(() => api(`/users/${user.id}/deactivate`, { method: 'POST' }), 'Сотрудник отключён')
                    }
                  >
                    <Button size="small" danger>
                      Отключить
                    </Button>
                  </Popconfirm>
                ) : (
                  <Button
                    size="small"
                    onClick={() => run(() => api(`/users/${user.id}/activate`, { method: 'POST' }), 'Сотрудник включён')}
                  >
                    Включить
                  </Button>
                )}
              </Flex>
            ),
          },
        ]}
      />

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
          {kind === 'create' && (
            <Form.Item name="fullName" label="ФИО" rules={[{ required: true, message: 'Введите ФИО' }]}>
              <Input />
            </Form.Item>
          )}
          {(kind === 'create' || kind === 'role') && (
            <Form.Item
              name="roleId"
              label="Роль"
              initialValue={dialog?.kind === 'role' ? dialog.user.roleId : undefined}
              rules={[{ required: true, message: 'Выберите роль' }]}
            >
              <Select options={roleOptions} />
            </Form.Item>
          )}
          {(kind === 'create' || kind === 'card') && (
            <Form.Item
              name="cardUid"
              label="UID карты"
              extra="Его видно в «Шкафы → Последние события», если приложить новую карту к считывателю"
              rules={kind === 'card' ? [{ required: true, message: 'Введите UID карты' }] : []}
            >
              <Input />
            </Form.Item>
          )}
          {(kind === 'create' || kind === 'credentials') && (
            <>
              <Form.Item
                name="email"
                label="Email для входа в панель"
                initialValue={dialog?.kind === 'credentials' ? (dialog.user.email ?? undefined) : undefined}
                rules={[
                  { type: 'email', message: 'Неверный email' },
                  { required: kind === 'credentials', message: 'Введите email' },
                ]}
              >
                <Input />
              </Form.Item>
              <Form.Item
                name="password"
                label="Пароль"
                rules={[
                  { min: 6, message: 'Минимум 6 символов' },
                  { required: kind === 'credentials', message: 'Введите пароль' },
                ]}
              >
                <Input.Password autoComplete="new-password" />
              </Form.Item>
            </>
          )}
        </Form>
      </Modal>
    </Flex>
  )
}
