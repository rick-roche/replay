import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { configApi } from './config'

// Helper to mock global fetch (avoid explicit any)
const mockFetch = (responseInit: Partial<Response> & { json?: unknown }) => {
  const resp = {
    ok: responseInit.ok ?? true,
    status: responseInit.status ?? (responseInit.ok ? 200 : 400),
    json: async () => (responseInit.json ?? {}),
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
      expect(fetch).toHaveBeenCalledWith('/api/config/lastfm', expect.objectContaining({ method: 'POST' }))
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { error: 'INVALID_USERNAME' } })
      await expect(configApi.configureLastfm('bad')).rejects.toThrow('INVALID_USERNAME')
    })
  })

  describe('getLastfmConfig', () => {
    it('returns configuration on success', async () => {
      mockFetch({ ok: true, json: { username: 'alice', playCount: 123, isConfigured: true } })
      const res = await configApi.getLastfmConfig()
      expect(res.username).toBe('alice')
    })

    it('throws a generic error on failure', async () => {
      mockFetch({ ok: false })
      await expect(configApi.getLastfmConfig()).rejects.toThrow('Failed to fetch Last.fm config')
    })
  })

  describe('fetchLastfmData', () => {
    it('returns data on success', async () => {
      mockFetch({ ok: true, json: { dataType: 'Tracks', tracks: [{ name: 't', artist: 'a', playCount: 1 }], albums: [], artists: [] } })
      const res = await configApi.fetchLastfmData('alice', { dataType: 'Tracks', timePeriod: 'Last12Months', maxResults: 50 })
      expect(res.dataType).toBe('Tracks')
      expect(res.tracks.length).toBe(1)
      expect(fetch).toHaveBeenCalledWith('/api/config/lastfm/data', expect.objectContaining({ method: 'POST' }))
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { error: 'INVALID_FILTER' } })
      await expect(configApi.fetchLastfmData('alice', { dataType: 'Tracks', timePeriod: 'Last12Months', maxResults: 50 })).rejects.toThrow('INVALID_FILTER')
    })
  })
})
