import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
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
    vi.clearAllMocks()
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
