import { describe, it, expect, vi, beforeEach, afterEach, type Mock } from 'vitest'
import { configApi } from './config'

// Helper to mock global fetch (avoid explicit any)
const mockFetch = (responseInit: Partial<Response> & { json?: unknown }) => {
  const resp = {
    ok: responseInit.ok ?? true,
    status: responseInit.status ?? (responseInit.ok ? 200 : 400),
    headers: new Headers(),
    json: async () => (responseInit.json ?? {}),
    text: async () => JSON.stringify(responseInit.json ?? {})
  } as Response
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(resp))
}

describe('configApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('configureLastfm', () => {
    it('returns configuration on success', async () => {
      mockFetch({ ok: true, json: { username: 'alice', playCount: 123, isConfigured: true } })
      const res = await configApi.configureLastfm('alice')
      expect(res).toEqual({ username: 'alice', playCount: 123, isConfigured: true })
      const call = (fetch as Mock).mock.calls[0]
      const req: Request = call[0]
      expect(req.url.endsWith('/api/config/lastfm')).toBe(true)
      expect(req.method).toBe('POST')
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'INVALID_USERNAME', message: 'INVALID_USERNAME' } })
      await expect(configApi.configureLastfm('bad')).rejects.toThrow('INVALID_USERNAME')
    })
  })

  describe('configureDiscogs', () => {
    it('returns configuration on success', async () => {
      mockFetch({ ok: true, json: { username: 'alice', collectionUrl: 'https://discogs.com/users/alice/collection', releaseCount: 42, isConfigured: true } })
      const res = await configApi.configureDiscogs('alice')
      expect(res).toEqual({ username: 'alice', collectionUrl: 'https://discogs.com/users/alice/collection', releaseCount: 42, isConfigured: true })
      const call = (fetch as Mock).mock.calls[0]
      const req: Request = call[0]
      expect(req.url.endsWith('/api/config/discogs')).toBe(true)
      expect(req.method).toBe('POST')
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'INVALID_DISCOGS_PROFILE', message: 'INVALID_DISCOGS_PROFILE' } })
      await expect(configApi.configureDiscogs('bad')).rejects.toThrow('INVALID_DISCOGS_PROFILE')
    })
  })

  describe('configureSetlistFm', () => {
    it('returns configuration on success', async () => {
      mockFetch({ ok: true, json: { userId: '123', displayName: 'alice', profileUrl: 'https://setlist.fm/users/alice', attendedConcerts: 10, isConfigured: true } })
      const res = await configApi.configureSetlistFm('alice')
      expect(res).toEqual({ userId: '123', displayName: 'alice', profileUrl: 'https://setlist.fm/users/alice', attendedConcerts: 10, isConfigured: true })
      const call = (fetch as Mock).mock.calls[0]
      const req: Request = call[0]
      expect(req.url.endsWith('/api/config/setlistfm')).toBe(true)
      expect(req.method).toBe('POST')
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'USER_NOT_FOUND', message: 'USER_NOT_FOUND' } })
      await expect(configApi.configureSetlistFm('nobody')).rejects.toThrow('USER_NOT_FOUND')
    })
  })
})
