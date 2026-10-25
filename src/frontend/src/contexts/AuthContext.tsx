import { Fragment, createContext, useContext, useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { authApi } from '../api/auth'
import type { SessionInfo, SpotifyUser } from '../types/auth'

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
  const generation = useRef(0)
  const loggingOut = useRef(false)

  const tryRefresh = useCallback(async () => {
    if (loggingOut.current) return true
    const request = generation.current
    try {
      const sessionInfo = await authApi.refresh()
      if (request === generation.current) setSession(sessionInfo)
      return true
    } catch {
      // Transport failures do not prove that the server session has expired.
      return false
    }
  }, [])

  const refreshSession = useCallback(async () => {
    await tryRefresh()
  }, [tryRefresh])

  // Check session on mount
  useEffect(() => {
    let active = true
    const request = generation.current
    void authApi.getSession().then(sessionInfo => {
      if (active && request === generation.current) setSession(sessionInfo)
    }).finally(() => {
      if (active) setIsLoading(false)
    })
    return () => { active = false }
  }, [])

  // Auto-refresh before expiry
  useEffect(() => {
    if (!session) return

    const expiresAt = new Date(session.expiresAt).getTime()
    const now = Date.now()
    const timeUntilRefresh = expiresAt - now - REFRESH_BUFFER_MS

    let active = true
    let attempts = 0
    let timer: ReturnType<typeof setTimeout>
    const run = async () => {
      const refreshed = await tryRefresh()
      if (!refreshed && active) {
        timer = setTimeout(run, Math.min(30000 * 2 ** attempts++, 120000))
      }
    }
    timer = setTimeout(run, Math.max(0, timeUntilRefresh))
    return () => { active = false; clearTimeout(timer) }
  }, [session, tryRefresh])

  function login() {
    authApi.login()
  }

  async function logout() {
    if (loggingOut.current) return
    loggingOut.current = true
    generation.current++
    setSession(null)
    try {
      await authApi.logout()
    } finally {
      generation.current++
      setSession(null)
      loggingOut.current = false
    }
  }

  const value: AuthContextValue = {
    user: session?.user ?? null,
    isLoading,
    isAuthenticated: session !== null,
    login,
    logout,
    refreshSession
  }

  return <AuthContext.Provider value={value}>
    {/* Session-owned providers must never carry private state into another login. */}
    <Fragment key={session?.user?.id ?? 'anonymous'}>{children}</Fragment>
  </AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
