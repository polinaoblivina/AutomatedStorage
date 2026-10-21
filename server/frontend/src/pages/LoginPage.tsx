import { useState } from 'react'
import { Alert, Button, Form, Input, Typography } from 'antd'
import { Navigate, useNavigate } from 'react-router'
import { useAuth } from '../auth/AuthContext'

interface LoginForm {
  email: string
  password: string
}

const facts = [
  { title: 'Доступ', text: 'по RFID-карте' },
  { title: 'Учёт', text: 'по весу в ячейке' },
  { title: 'Журнал', text: 'каждой операции' },
]

export function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  if (user) return <Navigate to="/" replace />

  async function handleSubmit(values: LoginForm) {
    setError(null)
    setSubmitting(true)
    try {
      await login(values.email, values.password)
      navigate('/')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Не удалось войти')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-side">
        <div>
          <span className="brand-mark">SS</span>
          <span className="brand-name">SmartStorage</span>
        </div>
        <div>
          <h1>Автоматизированное хранилище</h1>
          <p>Шкафы с весами и считывателем карт. Панель показывает, что лежит в ячейках, кто и что взял, и какие шкафы сейчас на связи.</p>
        </div>
        <div className="login-facts">
          {facts.map((fact) => (
            <div key={fact.title}>
              <div className="login-fact-value">{fact.title}</div>
              <div className="login-fact-text">{fact.text}</div>
            </div>
          ))}
        </div>
      </div>
      <div className="login-form">
        <div className="login-form-inner">
          <Typography.Title level={2} style={{ marginBottom: 4 }}>
            Вход
          </Typography.Title>
          <Typography.Paragraph type="secondary" style={{ marginBottom: 32 }}>
            Для администратора и кладовщика
          </Typography.Paragraph>
          <Form<LoginForm> layout="vertical" size="large" requiredMark={false} onFinish={handleSubmit}>
            <Form.Item name="email" label="Email" rules={[{ required: true, message: 'Введите email' }]}>
              <Input autoComplete="username" placeholder="name@company.ru" />
            </Form.Item>
            <Form.Item name="password" label="Пароль" rules={[{ required: true, message: 'Введите пароль' }]}>
              <Input.Password autoComplete="current-password" visibilityToggle={false} />
            </Form.Item>
            {error && <Alert type="error" title={error} style={{ marginBottom: 16 }} />}
            <Button type="primary" htmlType="submit" loading={submitting} block>
              Войти
            </Button>
          </Form>
        </div>
      </div>
    </div>
  )
}
