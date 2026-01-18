import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import type { SessionInfo, SpotifyUser } from '../types/auth'
import { authApi } from '../api/auth'

interface AuthContextValue {
  user: SpotifyUser | null
  isLoading: boolean
  isAuthenticated: boolean
  login: () => void
  logout: () => Promise<void>
  refreshSession: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

const REFRESH_BUFFER_MS = 5 * 60 * 1000 // Refresh 5 minutes before expiry

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<SessionInfo | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  // Check session on mount
  useEffect(() => {
    checkSession()
  }, [])

  // Auto-refresh before expiry
  useEffect(() => {
    if (!session) return

    const expiresAt = new Date(session.expiresAt).getTime()
    const now = Date.now()
    const timeUntilRefresh = expiresAt - now - REFRESH_BUFFER_MS

    if (timeUntilRefresh <= 0) {
      // Already expired or about to expire, refresh immediately
      refreshSession()
      return
    }

    // Schedule refresh
    const timer = setTimeout(() => {
      refreshSession()
    }, timeUntilRefresh)

    return () => clearTimeout(timer)
  }, [session])

  async function checkSession() {
    setIsLoading(true)
    try {
      const sessionInfo = await authApi.getSession()
      setSession(sessionInfo)
    } finally {
      setIsLoading(false)
    }
  }

  async function refreshSession() {
    const sessionInfo = await authApi.refresh()
    if (sessionInfo) {
      setSession(sessionInfo)
    } else {
      setSession(null)
    }
  }

  function login() {
    authApi.login()
  }

  async function logout() {
    await authApi.logout()
    setSession(null)
  }

  const value: AuthContextValue = {
    user: session?.user ?? null,
    isLoading,
    isAuthenticated: session !== null,
    login,
    logout,
    refreshSession
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
