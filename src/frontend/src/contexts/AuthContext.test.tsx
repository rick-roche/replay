import { describe, it, expect, vi, beforeEach } from 'vitest'
import { act, render, renderHook, screen, waitFor } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { AuthProvider, useAuth } from '@/contexts/AuthContext'
import * as authApiModule from '@/api/auth'

// Mock the auth API
vi.mock('@/api/auth', () => ({
  authApi: {
    getSession: vi.fn(),
    refresh: vi.fn(),
    login: vi.fn(),
    logout: vi.fn()
  }
}))

const authApi = authApiModule.authApi

// Test component that uses auth
function TestComponent() {
  const { user, isLoading, isAuthenticated, login } = useAuth()

  if (isLoading) {
    return <div>Loading...</div>
  }

  if (isAuthenticated && user) {
    return <div>Logged in as {user.displayName}</div>
  }

  return <button onClick={login}>Login</button>
}

describe('AuthContext', () => {
  beforeEach(() => {
    vi.resetAllMocks()
  })

  it('retains the user on a transient refresh error but clears an expired session', async () => {
    vi.mocked(authApi.getSession).mockResolvedValue({ sessionId: 's', user: { id: 'alice', displayName: 'Alice', email: '', imageUrl: null }, expiresAt: new Date(Date.now() + 3600000).toISOString() })
    const { result } = renderHook(useAuth, { wrapper: AuthProvider })
    await waitFor(() => expect(result.current.isAuthenticated).toBe(true))
    vi.mocked(authApi.refresh).mockRejectedValueOnce(new Error('503'))
    await act(async () => { await result.current.refreshSession() })
    expect(result.current.user?.id).toBe('alice')
    vi.mocked(authApi.refresh).mockResolvedValueOnce(null)
    await act(async () => { await result.current.refreshSession() })
    expect(result.current.isAuthenticated).toBe(false)
  })

  it('does not restore a session if its refresh completes after logout', async () => {
    const session = { sessionId: 's', user: { id: 'alice', displayName: 'Alice', email: '', imageUrl: null }, expiresAt: new Date(Date.now() + 3600000).toISOString() }
    vi.mocked(authApi.getSession).mockResolvedValue(session)
    vi.mocked(authApi.logout).mockResolvedValue(undefined)
    let resolve!: (value: typeof session) => void
    vi.mocked(authApi.refresh).mockReturnValueOnce(new Promise(done => { resolve = done }))
    const { result } = renderHook(useAuth, { wrapper: AuthProvider })
    await waitFor(() => expect(result.current.isAuthenticated).toBe(true))
    let pending!: Promise<void>
    act(() => { pending = result.current.refreshSession() })
    await act(async () => result.current.logout())
    await act(async () => { resolve(session); await pending })
    expect(result.current.isAuthenticated).toBe(false)
  })

  it.each(['before', 'after'])('does not restore auth when refresh requested during logout completes %s logout', async order => {
    const session = { sessionId: 's', user: { id: 'alice', displayName: 'Alice' }, expiresAt: new Date(Date.now() + 3600000).toISOString() }
    vi.mocked(authApi.getSession).mockResolvedValue(session)
    let finishLogout!: () => void
    vi.mocked(authApi.logout).mockReturnValueOnce(new Promise<void>(done => { finishLogout = done }))
    let finishRefresh!: (value: typeof session) => void
    vi.mocked(authApi.refresh).mockReturnValueOnce(new Promise(done => { finishRefresh = done }))
    const { result } = renderHook(useAuth, { wrapper: AuthProvider })
    await waitFor(() => expect(result.current.user?.id).toBe('alice'))
    let logout!: Promise<void>
    let refresh!: Promise<void>
    act(() => { logout = result.current.logout() })
    expect(result.current.isAuthenticated).toBe(false)
    act(() => { refresh = result.current.refreshSession() })
    if (order === 'after') await act(async () => { finishLogout(); await logout })
    await act(async () => { finishRefresh(session); await refresh })
    expect(result.current.isAuthenticated).toBe(false)
    expect(result.current.user).toBeNull()
    if (order === 'before') await act(async () => { finishLogout(); await logout })
  })

  it('backs off transient scheduled refresh failures and retries without logging out', async () => {
    vi.useFakeTimers()
    const session = { sessionId: 's', user: { id: 'alice', displayName: 'Alice', email: '', imageUrl: null }, expiresAt: new Date(Date.now() + 1000).toISOString() }
    vi.mocked(authApi.getSession).mockResolvedValue(session)
    vi.mocked(authApi.refresh).mockRejectedValueOnce(new Error('503')).mockResolvedValueOnce({ ...session, expiresAt: new Date(Date.now() + 3600000).toISOString() })
    const { result, unmount } = renderHook(useAuth, { wrapper: AuthProvider })
    try {
      await act(async () => { await vi.advanceTimersByTimeAsync(0) })
      expect(result.current.user?.id).toBe('alice')
      await act(async () => { await vi.advanceTimersByTimeAsync(30000) })
      expect(result.current.user?.id).toBe('alice')
      // A successful retry schedules the next refresh at the new expiry, not a tight loop.
      expect(authApi.refresh).toHaveBeenCalledTimes(2)
    } finally {
      unmount()
      vi.useRealTimers()
    }
  })

  it('should show loading state initially', () => {
    vi.mocked(authApi.getSession).mockResolvedValue(null)

    render(
      <Theme>
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      </Theme>
    )

    expect(screen.getByText('Loading...')).toBeInTheDocument()
  })

  it('should show login button when not authenticated', async () => {
    vi.mocked(authApi.getSession).mockResolvedValue(null)

    render(
      <Theme>
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      </Theme>
    )

    await waitFor(() => {
      expect(screen.getByText('Login')).toBeInTheDocument()
    })
  })

  it('should show user info when authenticated', async () => {
    const mockSession = {
      sessionId: 'test-session',
      user: {
        id: 'user123',
        displayName: 'Test User',
        email: 'test@example.com',
        imageUrl: null
      },
      expiresAt: new Date(Date.now() + 3600000).toISOString()
    }

    vi.mocked(authApi.getSession).mockResolvedValue(mockSession)

    render(
      <Theme>
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      </Theme>
    )

    await waitFor(() => {
      expect(screen.getByText('Logged in as Test User')).toBeInTheDocument()
    })
  })

  it('should call authApi.login when login is clicked', async () => {
    vi.mocked(authApi.getSession).mockResolvedValue(null)

    render(
      <Theme>
        <AuthProvider>
          <TestComponent />
        </AuthProvider>
      </Theme>
    )

    await waitFor(() => {
      const loginButton = screen.getByText('Login')
      loginButton.click()
    })

    expect(authApi.login).toHaveBeenCalled()
  })
})
