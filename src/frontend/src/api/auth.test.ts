import { describe, it, expect, vi, beforeEach } from 'vitest'
import { authApi } from '@/api/auth'

describe('authApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    global.fetch = vi.fn()
  })

  describe('login', () => {
    it('should redirect to login endpoint', () => {
      const originalHref = window.location.href
      const locationHrefMock = vi.fn()

      Object.defineProperty(window, 'location', {
        value: { href: '' },
        writable: true
      })

      authApi.login()

      expect(window.location.href).toContain('/api/auth/login')
      expect(window.location.href).toContain('returnUrl=')
    })

    it('should include return URL parameter', () => {
      const testUrl = 'http://localhost:5173/test'
      Object.defineProperty(window, 'location', {
        value: { href: testUrl },
        writable: true
      })

      authApi.login()

      const encodedUrl = encodeURIComponent(testUrl)
      expect(window.location.href).toContain(encodedUrl)
    })
  })

  describe('getSession', () => {
    it('should fetch session from API', async () => {
      const mockSession = {
        displayName: 'Test User',
        imageUrl: 'http://example.com/image.jpg',
        spotifyUserId: 'spotify123'
      }

      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        json: async () => mockSession
      })

      const result = await authApi.getSession()

      expect(result).toEqual(mockSession)
      expect(global.fetch).toHaveBeenCalledWith(
        '/api/auth/session',
        expect.objectContaining({
          credentials: 'include'
        })
      )
    })

    it('should return null on failed response', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: false,
        status: 401
      })

      const result = await authApi.getSession()

      expect(result).toBeNull()
    })

    it('should return null on fetch error', async () => {
      global.fetch = vi.fn().mockRejectedValueOnce(new Error('Network error'))

      const result = await authApi.getSession()

      expect(result).toBeNull()
    })

    it('should include credentials in request', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        json: async () => ({})
      })

      await authApi.getSession()

      const callArgs = (global.fetch as any).mock.calls[0]
      expect(callArgs[1]).toHaveProperty('credentials', 'include')
    })
  })

  describe('refresh', () => {
    it('should refresh session via POST', async () => {
      const mockSession = {
        displayName: 'Test User',
        imageUrl: 'http://example.com/image.jpg',
        spotifyUserId: 'spotify123'
      }

      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        json: async () => mockSession
      })

      const result = await authApi.refresh()

      expect(result).toEqual(mockSession)
      expect(global.fetch).toHaveBeenCalledWith(
        '/api/auth/refresh',
        expect.objectContaining({
          method: 'POST',
          credentials: 'include'
        })
      )
    })

    it('should return null on failed response', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: false,
        status: 401
      })

      const result = await authApi.refresh()

      expect(result).toBeNull()
    })

    it('should return null on fetch error', async () => {
      global.fetch = vi.fn().mockRejectedValueOnce(new Error('Network error'))

      const result = await authApi.refresh()

      expect(result).toBeNull()
    })

    it('should use POST method', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true,
        json: async () => ({})
      })

      await authApi.refresh()

      const callArgs = (global.fetch as any).mock.calls[0]
      expect(callArgs[1]).toHaveProperty('method', 'POST')
    })
  })

  describe('logout', () => {
    it('should call logout endpoint with POST', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true
      })

      await authApi.logout()

      expect(global.fetch).toHaveBeenCalledWith(
        '/api/auth/logout',
        expect.objectContaining({
          method: 'POST',
          credentials: 'include'
        })
      )
    })

    it('should include credentials in logout request', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true
      })

      await authApi.logout()

      const callArgs = (global.fetch as any).mock.calls[0]
      expect(callArgs[1]).toHaveProperty('credentials', 'include')
    })

    it('should use correct logout endpoint', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true
      })

      await authApi.logout()

      const callArgs = (global.fetch as any).mock.calls[0]
      expect(callArgs[0]).toBe('/api/auth/logout')
    })

    it('should not throw on successful logout', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: true
      })

      const result = authApi.logout()

      await expect(result).resolves.not.toThrow()
    })

    it('should handle errors gracefully', async () => {
      global.fetch = vi.fn().mockRejectedValueOnce(new Error('Network error'))

      await expect(authApi.logout()).rejects.toThrow()
    })

    it('should still complete even if response is not ok', async () => {
      global.fetch = vi.fn().mockResolvedValueOnce({
        ok: false,
        status: 500
      })

      const result = authApi.logout()

      await expect(result).resolves.toBeUndefined()
    })
  })
})
