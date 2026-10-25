import { describe, it, expect, vi, beforeEach } from 'vitest'
import { authApi } from './auth'

const session = { sessionId: 'session-1', user: { id: 'alice', displayName: 'Alice' }, expiresAt: '2030-01-01T00:00:00Z' }
describe('authApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    global.fetch = vi.fn()
  })
  it('sends only a local return path to OAuth login', () => {
    Object.defineProperty(window, 'location', { value: { origin: 'http://localhost', href: 'https://example.com/test?mode=albums#curate', pathname: '/test', search: '?mode=albums', hash: '#curate' }, writable: true })
    authApi.login()
    expect(window.location.href).toBe('/api/auth/login?returnUrl=%2Ftest%3Fmode%3Dalbums%23curate')
  })
  it('loads an authenticated session with cookies through the generated client', async () => {
    global.fetch = vi.fn().mockResolvedValue(Response.json(session))
    expect(await authApi.getSession()).toEqual(session)
    const [request, init] = vi.mocked(fetch).mock.calls[0]
    expect(request).toBeInstanceOf(Request)
    expect(init?.credentials).toBe('include')
  })
  it('treats 401 session lookup as unauthenticated', async () => {
    global.fetch = vi.fn().mockResolvedValue(Response.json({ code: 'SESSION_EXPIRED', message: 'Expired' }, { status: 401 }))
    expect(await authApi.getSession()).toBeNull()
  })
  it('handles an unavailable initial session lookup', async () => {
    global.fetch = vi.fn().mockRejectedValue(new Error('offline'))
    expect(await authApi.getSession()).toBeNull()
  })
  it('returns a refreshed session', async () => {
    global.fetch = vi.fn().mockResolvedValue(Response.json(session))
    expect(await authApi.refresh()).toEqual(session)
    const [request] = vi.mocked(fetch).mock.calls[0]
    expect(request).toMatchObject({ method: 'POST' })
  })
  it('signals fatal 401 refresh separately from a service outage', async () => {
    global.fetch = vi.fn().mockResolvedValueOnce(Response.json({ code: 'SESSION_EXPIRED' }, { status: 401 })).mockResolvedValueOnce(Response.json({ code: 'UPSTREAM_UNAVAILABLE' }, { status: 503 }))
    expect(await authApi.refresh()).toBeNull()
    await expect(authApi.refresh()).rejects.toThrow('temporarily unavailable')
  })
  it('preserves transient transport failure as a retryable rejection', async () => {
    global.fetch = vi.fn().mockRejectedValue(new Error('Network error'))
    await expect(authApi.refresh()).rejects.toThrow('Network error')
  })
  it('logs out through the generated POST endpoint', async () => {
    global.fetch = vi.fn().mockResolvedValue(new Response(null, { status: 204 }))
    await authApi.logout()
    const [request] = vi.mocked(fetch).mock.calls[0]
    expect(request).toMatchObject({ method: 'POST' })
    expect(request instanceof Request && new URL(request.url).pathname).toBe('/api/auth/logout')
  })
})
