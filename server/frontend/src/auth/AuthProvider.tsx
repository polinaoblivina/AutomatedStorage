import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { api, refreshSession, saveSession } from '../api/client'
import type { LoginResponse, User } from '../api/types'
import { AuthContext } from './AuthContext'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const signedInByLogin = useRef(false)

  useEffect(() => {
    refreshSession().then((session) => {
      if (!signedInByLogin.current) setUser(session?.user ?? null)
      setLoading(false)
    })
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const session = await api<LoginResponse>('/auth/login', { method: 'POST', body: { email, password } })
    signedInByLogin.current = true
    saveSession(session)
    setUser(session.user)
  }, [])

  const logout = useCallback(async () => {
    await api('/auth/logout', { method: 'POST' }).catch(() => undefined)
    saveSession(null)
    setUser(null)
  }, [])

  const value = useMemo(() => ({ user, loading, login, logout }), [user, loading, login, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
