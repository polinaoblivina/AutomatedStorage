import { useEffect, useState, type ReactNode } from 'react'
import { api, refreshSession, saveSession } from '../api/client'
import type { LoginResponse, User } from '../api/types'
import { AuthContext } from './AuthContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    refreshSession().then((session) => {
      setUser(session?.user ?? null)
      setLoading(false)
    })
  }, [])

  async function login(email: string, password: string) {
    const session = await api<LoginResponse>('/auth/login', { method: 'POST', body: { email, password } })
    saveSession(session)
    setUser(session.user)
  }

  async function logout() {
    await api('/auth/logout', { method: 'POST' }).catch(() => undefined)
    saveSession(null)
    setUser(null)
  }

  return <AuthContext.Provider value={{ user, loading, login, logout }}>{children}</AuthContext.Provider>
}
