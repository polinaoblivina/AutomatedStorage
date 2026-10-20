import { Button, Layout, Menu, Space, Typography } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router'
import { useAuth } from '../auth/AuthContext'

const menuItems = [
  { key: '/devices', label: 'Шкафы' },
  { key: '/journal', label: 'Журнал' },
]

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

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
          items={menuItems}
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
