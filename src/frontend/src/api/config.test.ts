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

  describe('fetchLastfmDataNormalized', () => {
    it('returns normalized data on success', async () => {
      mockFetch({ ok: true, json: { dataType: 'Tracks', tracks: [{ name: 't', artist: 'a', source: 'lastfm', sourceMetadata: { playCount: 1 } }], albums: [], artists: [], source: 'lastfm' } })
      const res = await configApi.fetchLastfmDataNormalized('alice', { dataType: 'Tracks', timePeriod: 'Last12Months', maxResults: 50 })
      expect(res.dataType).toBe('Tracks')
      expect(res.source).toBe('lastfm')
      const call = (fetch as Mock).mock.calls[0]
      const req: Request = call[0]
      expect(req.url.endsWith('/api/sources/lastfm/data/normalized')).toBe(true)
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'SERVER_ERROR', message: 'SERVER_ERROR' } })
      await expect(configApi.fetchLastfmDataNormalized('alice', { dataType: 'Tracks', timePeriod: 'Last12Months', maxResults: 50 })).rejects.toThrow('SERVER_ERROR')
    })
  })

  describe('fetchSetlistFmData', () => {
    it('returns Setlist.fm data on success', async () => {
      mockFetch({ ok: true, json: { concerts: [{ date: '2023-01-01', venue: 'venue1', city: 'NYC' }] } })
      const res = await configApi.fetchSetlistFmData('user123', { maxConcerts: 10, maxTracks: 100 })
      expect(res.concerts).toBeDefined()
      const call = (fetch as Mock).mock.calls[0]
      const req: Request = call[0]
      expect(req.url.endsWith('/api/sources/setlistfm/data')).toBe(true)
      expect(req.method).toBe('POST')
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'INVALID_USER_ID', message: 'INVALID_USER_ID' } })
      await expect(configApi.fetchSetlistFmData('baduser', { maxConcerts: 10, maxTracks: 100 })).rejects.toThrow('INVALID_USER_ID')
    })
  })

  describe('fetchSetlistFmDataNormalized', () => {
    it('returns normalized Setlist.fm data on success', async () => {
      mockFetch({ ok: true, json: { dataType: 'Tracks', tracks: [{ name: 't', artist: 'a', source: 'setlistfm', sourceMetadata: {} }], albums: [], artists: [], source: 'setlistfm' } })
      const res = await configApi.fetchSetlistFmDataNormalized('user123', { maxConcerts: 10, maxTracks: 100 })
      expect(res.dataType).toBe('Tracks')
      expect(res.source).toBe('setlistfm')
      const call = (fetch as Mock).mock.calls[0]
      const req: Request = call[0]
      expect(req.url.endsWith('/api/sources/setlistfm/data/normalized')).toBe(true)
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'API_ERROR', message: 'API_ERROR' } })
      await expect(configApi.fetchSetlistFmDataNormalized('user123', { maxConcerts: 10, maxTracks: 100 })).rejects.toThrow('API_ERROR')
    })
  })
})
