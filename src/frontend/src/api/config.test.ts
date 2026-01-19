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

  describe('fetchLastfmData', () => {
    it('returns data on success', async () => {
      mockFetch({ ok: true, json: { dataType: 'Tracks', tracks: [{ name: 't', artist: 'a', playCount: 1 }], albums: [], artists: [] } })
      const res = await configApi.fetchLastfmData('alice', { dataType: 'Tracks', timePeriod: 'Last12Months', maxResults: 50 })
      expect(res.dataType).toBe('Tracks')
      expect(res.tracks.length).toBe(1)
      const call2 = (fetch as Mock).mock.calls[0]
      const req2: Request = call2[0]
      expect(req2.url.endsWith('/api/sources/lastfm/data')).toBe(true)
      expect(req2.method).toBe('POST')
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'INVALID_FILTER', message: 'INVALID_FILTER' } })
      await expect(configApi.fetchLastfmData('alice', { dataType: 'Tracks', timePeriod: 'Last12Months', maxResults: 50 })).rejects.toThrow('INVALID_FILTER')
    })
  })
})
