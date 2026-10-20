import { Button, Layout, Menu, Space, Typography } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router'
import type { Permission } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { can } from '../auth/can'

const menuItems: { key: string; label: string; permission: Permission }[] = [
  { key: '/dashboard', label: 'Сводка', permission: 'ViewJournal' },
  { key: '/devices', label: 'Шкафы', permission: 'ViewJournal' },
  { key: '/storage', label: 'Склад', permission: 'ViewJournal' },
  { key: '/journal', label: 'Журнал', permission: 'ViewJournal' },
  { key: '/users', label: 'Пользователи', permission: 'ManageUsers' },
]

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const visibleItems = menuItems
    .filter((item) => can(user, item.permission))
    .map(({ key, label }) => ({ key, label }))

  async function handleLogout() {
    await logout()
    navigate('/login')
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Layout.Header style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
        <Typography.Text strong style={{ color: '#fff', fontSize: 18 }}>
          SmartStorage
        </Typography.Text>
        <Menu
          theme="dark"
          mode="horizontal"
          selectedKeys={[location.pathname]}
          items={visibleItems}
          onClick={(item) => navigate(item.key)}
          style={{ flex: 1 }}
        />
        <Space>
          <Typography.Text style={{ color: '#fff' }}>{user?.fullName}</Typography.Text>
          <Button onClick={handleLogout}>Выйти</Button>
        </Space>
      </Layout.Header>
      <Layout.Content style={{ padding: 24 }}>
        <Outlet />
      </Layout.Content>
    </Layout>
  )
}
