import { useCallback, useEffect, useState } from 'react'
import { Alert, App, Avatar, Button, Card, Col, Dropdown, Flex, Form, Input, Modal, Row, Segmented, Select, Table, Tag } from 'antd'
import type { MenuProps } from 'antd'
import { api } from '../api/client'
import type { Role, User } from '../api/types'
import { PageHeader } from '../components/PageHeader'
import { StatCard } from '../components/StatCard'
import { StatusDot } from '../components/StatusDot'
import { colors } from '../theme'
import { errorText, initials } from '../utils/format'

type Dialog =
  | { kind: 'create' }
  | { kind: 'rename'; user: User }
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

type StatusFilter = 'all' | 'active' | 'inactive'

const dialogTitles: Record<Dialog['kind'], string> = {
  create: 'Новый сотрудник',
  rename: 'Изменить ФИО',
  card: 'Привязать карту',
  role: 'Сменить роль',
  credentials: 'Вход в веб-панель',
}

const roleColors = ['geekblue', 'cyan', 'purple', 'gold', 'magenta']

function initialValues(dialog: Dialog | null): DialogForm {
  switch (dialog?.kind) {
    case 'rename':
      return { fullName: dialog.user.fullName }
    case 'card':
      return { cardUid: dialog.user.cardUid ?? undefined }
    case 'role':
      return { roleId: dialog.user.roleId }
    case 'credentials':
      return { email: dialog.user.email ?? undefined }
    default:
      return {}
  }
}

export function UsersPage() {
  const { message, modal } = App.useApp()
  const [form] = Form.useForm<DialogForm>()
  const [users, setUsers] = useState<User[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dialog, setDialog] = useState<Dialog | null>(null)
  const [saving, setSaving] = useState(false)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<StatusFilter>('all')
  const [roleId, setRoleId] = useState<string | undefined>(undefined)

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

  function confirm(title: string, action: () => Promise<unknown>, success: string) {
    modal.confirm({
      title,
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
        case 'rename':
          return api(`/users/${dialog.user.id}`, { method: 'PUT', body: { fullName: values.fullName } })
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

  function userMenu(user: User): MenuProps['items'] {
    const entries: MenuProps['items'] = [
      { key: 'rename', label: 'Изменить ФИО', onClick: () => setDialog({ kind: 'rename', user }) },
      { key: 'role', label: 'Сменить роль', onClick: () => setDialog({ kind: 'role', user }) },
      { key: 'card', label: user.cardUid ? 'Заменить карту' : 'Привязать карту', onClick: () => setDialog({ kind: 'card', user }) },
    ]
    if (user.cardUid) {
      entries.push({
        key: 'removeCard',
        label: 'Отвязать карту',
        onClick: () =>
          confirm(`Отвязать карту у «${user.fullName}»?`, () => api(`/users/${user.id}/card`, { method: 'DELETE' }), 'Карта отвязана'),
      })
    }
    entries.push(
      { key: 'credentials', label: 'Вход в веб-панель', onClick: () => setDialog({ kind: 'credentials', user }) },
      { type: 'divider' },
      user.isActive
        ? {
            key: 'deactivate',
            label: 'Отключить',
            danger: true,
            onClick: () =>
              confirm(
                `Отключить «${user.fullName}»?`,
                () => api(`/users/${user.id}/deactivate`, { method: 'POST' }),
                'Сотрудник отключён',
              ),
          }
        : {
            key: 'activate',
            label: 'Включить',
            onClick: () => run(() => api(`/users/${user.id}/activate`, { method: 'POST' }), 'Сотрудник включён'),
          },
    )
    return entries
  }

  const roleOptions = roles.map((role) => ({ value: role.id, label: role.name }))
  const roleColor = (id: string) => roleColors[Math.max(0, roles.findIndex((r) => r.id === id)) % roleColors.length]
  const kind = dialog?.kind

  const visibleUsers = users.filter((u) => {
    if (status === 'active' && !u.isActive) return false
    if (status === 'inactive' && u.isActive) return false
    if (roleId && u.roleId !== roleId) return false
    const text = `${u.fullName} ${u.email ?? ''} ${u.cardUid ?? ''}`.toLowerCase()
    return text.includes(search.toLowerCase())
  })

  return (
    <>
      <PageHeader
        title="Пользователи"
        subtitle="Сотрудники, их карты, роли и доступ к панели"
        extra={
          <Button type="primary" onClick={() => setDialog({ kind: 'create' })}>
            Добавить сотрудника
          </Button>
        }
      />

      <Row gutter={[16, 16]} style={{ marginBottom: 16 }}>
        <Col xs={12} lg={6}>
          <StatCard label="Всего" value={users.length} accent={colors.sidebar} />
        </Col>
        <Col xs={12} lg={6}>
          <StatCard label="Активных" value={users.filter((u) => u.isActive).length} accent={colors.success} />
        </Col>
        <Col xs={12} lg={6}>
          <StatCard label="С картой" value={users.filter((u) => u.cardUid).length} />
        </Col>
        <Col xs={12} lg={6}>
          <StatCard label="С входом в панель" value={users.filter((u) => u.email).length} accent="#2563eb" />
        </Col>
      </Row>

      <Card style={{ marginBottom: 16 }}>
        <Flex gap={12} wrap align="center">
          <Input
            allowClear
            placeholder="Поиск по имени, email или карте"
            style={{ maxWidth: 320 }}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Segmented<StatusFilter>
            value={status}
            onChange={setStatus}
            options={[
              { value: 'all', label: 'Все' },
              { value: 'active', label: 'Активные' },
              { value: 'inactive', label: 'Отключённые' },
            ]}
          />
          <Select
            allowClear
            placeholder="Любая роль"
            style={{ minWidth: 180 }}
            value={roleId}
            onChange={setRoleId}
            options={roleOptions}
          />
        </Flex>
      </Card>

      {error && <Alert type="error" title={error} style={{ marginBottom: 16 }} />}

      <Card styles={{ body: { padding: 0, overflowX: 'auto' } }}>
        <Table<User>
          rowKey="id"
          loading={loading}
          dataSource={visibleUsers}
          pagination={false}
          locale={{ emptyText: 'Никого не найдено' }}
          columns={[
            {
              title: 'Сотрудник',
              render: (_, u) => (
                <Flex align="center" gap={12} style={{ opacity: u.isActive ? 1 : 0.55 }}>
                  <Avatar style={{ background: u.isActive ? colors.primary : colors.muted, flexShrink: 0 }}>
                    {initials(u.fullName)}
                  </Avatar>
                  <div>
                    <div style={{ fontWeight: 500 }}>{u.fullName}</div>
                    <div className="muted" style={{ fontSize: 12 }}>
                      {u.email ?? 'без входа в панель'}
                    </div>
                  </div>
                </Flex>
              ),
            },
            {
              title: 'Роль',
              dataIndex: 'roleName',
              render: (name, u) => <Tag color={roleColor(u.roleId)}>{name}</Tag>,
            },
            {
              title: 'Карта',
              dataIndex: 'cardUid',
              render: (card) => (card ? <span className="mono">{card}</span> : <span className="muted">нет</span>),
            },
            {
              title: 'Статус',
              dataIndex: 'isActive',
              render: (active) =>
                active ? <StatusDot tone="success">активен</StatusDot> : <StatusDot tone="muted">отключён</StatusDot>,
            },
            {
              title: '',
              align: 'right',
              render: (_, u) => (
                <Dropdown menu={{ items: userMenu(u) }} trigger={['click']}>
                  <Button size="small">Действия</Button>
                </Dropdown>
              ),
            },
          ]}
        />
      </Card>

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
          {(kind === 'create' || kind === 'rename') && (
            <Form.Item name="fullName" label="ФИО" rules={[{ required: true, message: 'Введите ФИО' }]}>
              <Input />
            </Form.Item>
          )}
          {(kind === 'create' || kind === 'role') && (
            <Form.Item name="roleId" label="Роль" rules={[{ required: true, message: 'Выберите роль' }]}>
              <Select options={roleOptions} />
            </Form.Item>
          )}
          {(kind === 'create' || kind === 'card') && (
            <Form.Item
              name="cardUid"
              label="UID карты"
              extra="Его видно в «Шкафы на связи», раздел «Последние события», если приложить новую карту к считывателю"
              rules={kind === 'card' ? [{ required: true, message: 'Введите UID карты' }] : []}
            >
              <Input className="mono" />
            </Form.Item>
          )}
          {(kind === 'create' || kind === 'credentials') && (
            <>
              <Form.Item
                name="email"
                label="Email для входа в панель"
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
                <Input.Password autoComplete="new-password" visibilityToggle={false} />
              </Form.Item>
            </>
          )}
        </Form>
      </Modal>
    </>
  )
}
