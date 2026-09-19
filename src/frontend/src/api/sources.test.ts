import { describe, it, expect, vi, beforeEach, afterEach, type Mock } from 'vitest'
import { sourcesApi } from './sources'

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

describe('sourcesApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('fetchLastfmData', () => {
    it('returns data on success', async () => {
      mockFetch({ ok: true, json: { dataType: 'Tracks', tracks: [{ name: 't', artist: 'a', playCount: 1 }], albums: [], artists: [] } })
      const res = await sourcesApi.fetchLastfmData('alice', { dataType: 'Tracks', timePeriod: 'Last12Months', maxResults: 50 })
      expect(res.dataType).toBe('Tracks')
      expect(res.tracks.length).toBe(1)
      const call = (fetch as Mock).mock.calls[0]
      const req: Request = call[0]
      expect(req.url.endsWith('/api/sources/lastfm/data')).toBe(true)
      expect(req.method).toBe('POST')
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'INVALID_FILTER', message: 'INVALID_FILTER' } })
      await expect(sourcesApi.fetchLastfmData('alice', { dataType: 'Tracks', timePeriod: 'Last12Months', maxResults: 50 })).rejects.toThrow('INVALID_FILTER')
    })
  })

  describe('fetchDiscogsData', () => {
    it('returns data on success', async () => {
      mockFetch({ ok: true, json: { dataType: 'Albums', albums: [{ name: 'a', artist: 'ar', releaseYear: 2020 }], tracks: [], artists: [] } })
      const res = await sourcesApi.fetchDiscogsData('alice', { type: 'All', maxResults: 50 })
      expect(res.dataType).toBe('Albums')
      expect(res.albums.length).toBe(1)
      const call = (fetch as Mock).mock.calls[0]
      const url = call[0] as string
      expect(url).toBe('/api/sources/discogs/data')
      const options = call[1]
      expect(options.method).toBe('POST')
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'INVALID_DISCOGS_PROFILE', message: 'INVALID_DISCOGS_PROFILE' } })
      await expect(sourcesApi.fetchDiscogsData('bad', { type: 'All', maxResults: 50 })).rejects.toThrow('INVALID_DISCOGS_PROFILE')
    })
  })

  describe('fetchSetlistFmData', () => {
    it('returns Setlist.fm data on success', async () => {
      mockFetch({ ok: true, json: { dataType: 'Tracks', tracks: [{ name: 'Song', artist: 'Artist' }], albums: [], artists: [], totalResults: 1, source: 'setlistfm' } })
      const res = await sourcesApi.fetchSetlistFmData('user123', { maxConcerts: 10, maxTracks: 100 })
      expect(res.tracks).toHaveLength(1)
      const call = (fetch as Mock).mock.calls[0]
      const request = call[0] as Request
      expect(request.url.endsWith('/api/sources/setlistfm/data')).toBe(true)
      expect(request.method).toBe('POST')
    })

    it('throws error with server message on failure', async () => {
      mockFetch({ ok: false, json: { code: 'INVALID_USER_ID', message: 'INVALID_USER_ID' } })
      await expect(sourcesApi.fetchSetlistFmData('baduser', { maxConcerts: 10, maxTracks: 100 })).rejects.toThrow('INVALID_USER_ID')
    })
  })

  describe('fetchSetlistFmConcerts', () => {
    it('returns paginated concerts on success', async () => {
      mockFetch({
        ok: true,
        json: {
          concerts: [{ id: 'a1', artist: 'Band', date: '01-01-2024' }],
          totalConcerts: 1,
          pageNumber: 1,
          pageSize: 20,
          hasNextPage: false,
          hasPreviousPage: false
        }
      })

      const res = await sourcesApi.fetchSetlistFmConcerts('user123', { maxConcerts: 10, maxTracks: 100 }, 1)
      expect(res.concerts).toHaveLength(1)
      const call = (fetch as Mock).mock.calls[0]
      const request = call[0] as Request
      expect(request.url.endsWith('/api/sources/setlistfm/concerts')).toBe(true)
      expect(request.method).toBe('POST')
    })
  })
})
