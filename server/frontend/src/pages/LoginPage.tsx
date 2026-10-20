import { useState } from 'react'
import { Alert, Button, Card, Form, Input } from 'antd'
import { Navigate, useNavigate } from 'react-router'
import { useAuth } from '../auth/AuthContext'

interface LoginForm {
  email: string
  password: string
}

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
    <div style={{ display: 'flex', justifyContent: 'center', paddingTop: 120 }}>
      <Card title="Вход в SmartStorage" style={{ width: 360 }}>
        <Form<LoginForm> layout="vertical" onFinish={handleSubmit}>
          <Form.Item name="email" label="Email" rules={[{ required: true, message: 'Введите email' }]}>
            <Input autoComplete="username" />
          </Form.Item>
          <Form.Item name="password" label="Пароль" rules={[{ required: true, message: 'Введите пароль' }]}>
            <Input.Password autoComplete="current-password" />
          </Form.Item>
          {error && <Alert type="error" title={error} style={{ marginBottom: 16 }} />}
          <Button type="primary" htmlType="submit" loading={submitting} block>
            Войти
          </Button>
        </Form>
      </Card>
    </div>
  )
}
