import { useState } from 'react'
import { Avatar, Button, Layout, Menu } from 'antd'
import { Outlet, useLocation, useNavigate } from 'react-router'
import type { Permission } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { can } from '../auth/can'
import { colors } from '../theme'
import { initials } from '../utils/format'

const menuItems: { key: string; label: string; permission: Permission }[] = [
  { key: '/dashboard', label: 'Сводка', permission: 'ViewJournal' },
  { key: '/devices', label: 'Шкафы на связи', permission: 'ViewJournal' },
  { key: '/storage', label: 'Ячейки и товары', permission: 'ViewJournal' },
  { key: '/journal', label: 'Журнал операций', permission: 'ViewJournal' },
  { key: '/users', label: 'Пользователи', permission: 'ManageUsers' },
]

export function AppLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [collapsed, setCollapsed] = useState(false)
  const [mobile, setMobile] = useState(false)

  const visibleItems = menuItems
    .filter((item) => can(user, item.permission))
    .map(({ key, label }) => ({ key, label }))

  function handleMenuClick(key: string) {
    navigate(key)
    if (mobile) setCollapsed(true)
  }

  async function handleLogout() {
    await logout()
    navigate('/login')
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Layout.Sider
        className="sider"
        width={248}
        breakpoint="lg"
        collapsedWidth={0}
        collapsed={collapsed}
        trigger={null}
        onBreakpoint={(broken) => {
          setMobile(broken)
          setCollapsed(broken)
        }}
      >
        <div className="sider-inner">
          <div className="brand">
            <span className="brand-mark">SS</span>
            <span className="brand-name">SmartStorage</span>
            <div className="brand-sub">Панель склада</div>
          </div>
          <Menu
            theme="dark"
            mode="inline"
            selectedKeys={[location.pathname]}
            items={visibleItems}
            onClick={(item) => handleMenuClick(item.key)}
          />
          <div className="sider-user">
            <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 14 }}>
              <Avatar style={{ background: colors.primary, flexShrink: 0 }}>{initials(user?.fullName ?? '')}</Avatar>
              <div style={{ minWidth: 0 }}>
                <div className="sider-user-name">{user?.fullName}</div>
                <div className="sider-user-role">{user?.roleName}</div>
              </div>
            </div>
            <Button block ghost onClick={handleLogout}>
              Выйти
            </Button>
          </div>
        </div>
      </Layout.Sider>
      <Layout>
        <div className="mobile-bar">
          <strong>SmartStorage</strong>
          <Button onClick={() => setCollapsed(!collapsed)}>{collapsed ? 'Меню' : 'Закрыть'}</Button>
        </div>
        <Layout.Content className="content">
          <Outlet />
        </Layout.Content>
      </Layout>
    </Layout>
  )
}
