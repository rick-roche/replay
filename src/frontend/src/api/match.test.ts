import { describe, it, expect, vi, beforeEach } from 'vitest'
import { matchApi } from '@/api/match'
import { client } from '@/api/client'

vi.mock('@/api/client')
vi.mock('@/api/errors', () => ({
  handleApiError: vi.fn()
}))

describe('matchApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('matchTracksToSpotify', () => {
    it('should send match request to API', async () => {
      const mockRequest = {
        tracks: [
          { title: 'Track 1', artist: 'Artist 1' },
          { title: 'Track 2', artist: 'Artist 2' }
        ]
      }

      const mockResponse = {
        tracks: [
          {
            id: '1',
            name: 'Track 1',
            artist: 'Artist 1',
            album: 'Album 1',
            uri: 'spotify:track:1',
            match: null
          },
          {
            id: '2',
            name: 'Track 2',
            artist: 'Artist 2',
            album: 'Album 2',
            uri: 'spotify:track:2',
            match: null
          }
        ]
      }

      vi.mocked(client.POST).mockResolvedValueOnce({
        data: mockResponse,
        error: null
      })

      const result = await matchApi.matchTracksToSpotify(mockRequest as Parameters<typeof matchApi.matchTracksToSpotify>[0])

      expect(result).toEqual(mockResponse)
      expect(client.POST).toHaveBeenCalledWith(
        '/api/api/match/spotify',
        expect.objectContaining({
          body: mockRequest
        })
      )
    })

    it('should handle API errors', async () => {
      const mockError = { message: 'API Error' }

      vi.mocked(client.POST).mockResolvedValueOnce({
        data: undefined,
        error: mockError
      })

      const { handleApiError } = await import('@/api/errors')

      const mockRequest = { tracks: [] }

      try {
        await matchApi.matchTracksToSpotify(mockRequest as Parameters<typeof matchApi.matchTracksToSpotify>[0])
      } catch {
        // Expected to throw
      }

      expect(handleApiError).toHaveBeenCalled()
    })

    it('should include request body in POST call', async () => {
      const mockRequest = {
        tracks: [{ title: 'Test', artist: 'Test Artist' }]
      }

      vi.mocked(client.POST).mockResolvedValueOnce({
        data: { tracks: [] },
        error: null
      })

      await matchApi.matchTracksToSpotify(mockRequest as Parameters<typeof matchApi.matchTracksToSpotify>[0])

      const call = vi.mocked(client.POST).mock.calls[0]
      expect(call[1]?.body).toEqual(mockRequest)
    })
  })

  describe('searchTracksForManualMatch', () => {
    it('should search for tracks on Spotify', async () => {
      const mockResults = [
        {
          id: '1',
          name: 'Test Track',
          artist: 'Test Artist',
          album: 'Test Album',
          uri: 'spotify:track:1'
        }
      ]

      vi.mocked(client.GET).mockResolvedValueOnce({
        data: mockResults,
        error: null
      })

      const result = await matchApi.searchTracksForManualMatch('test query')

      expect(result).toEqual(mockResults)
      expect(client.GET).toHaveBeenCalledWith(
        '/api/api/match/spotify/search',
        expect.objectContaining({
          params: {
            query: {
              query: 'test query'
            }
          }
        })
      )
    })

    it('should handle no results', async () => {
      vi.mocked(client.GET).mockResolvedValueOnce({
        data: null,
        error: null
      })

      const result = await matchApi.searchTracksForManualMatch('nonexistent')

      expect(result).toEqual([])
    })

    it('should handle API errors', async () => {
      const mockError = { message: 'API Error' }

      vi.mocked(client.GET).mockResolvedValueOnce({
        data: undefined,
        error: mockError
      })

      const { handleApiError } = await import('@/api/errors')

      try {
        await matchApi.searchTracksForManualMatch('test')
      } catch {
        // Expected
      }

      expect(handleApiError).toHaveBeenCalled()
    })

    it('should pass query parameter correctly', async () => {
      const testQuery = 'Beatles Let It Be'

      vi.mocked(client.GET).mockResolvedValueOnce({
        data: [],
        error: null
      })

      await matchApi.searchTracksForManualMatch(testQuery)

      const call = vi.mocked(client.GET).mock.calls[0]
      expect(call[1]?.params?.query?.query).toBe(testQuery)
    })

    it('should return empty array on null data', async () => {
      vi.mocked(client.GET).mockResolvedValueOnce({
        data: null,
        error: null
      })

      const result = await matchApi.searchTracksForManualMatch('test')

      expect(Array.isArray(result)).toBe(true)
      expect(result.length).toBe(0)
    })
  })

  describe('matchAlbumsToSpotify', () => {
    it('should send match albums request to API', async () => {
      const mockRequest = {
        albums: [
          { name: 'Album 1', artist: 'Artist 1', tracks: [], sourceMetadata: {}, source: 'lastfm' }
        ]
      }

      const mockResponse = {
        albums: [
          {
            sourceAlbum: mockRequest.albums[0],
            match: {
              spotifyId: 'albumId1',
              name: 'Album 1',
              artist: 'Artist 1',
              uri: 'spotify:album:albumId1',
              tracks: [],
              confidence: 100,
              method: 'Exact' as const
            },
            isMatched: true
          }
        ]
      }

      vi.mocked(client.POST).mockResolvedValueOnce({
        data: mockResponse,
        error: null
      })

      const result = await matchApi.matchAlbumsToSpotify(mockRequest)

      expect(result).toEqual(mockResponse)
      expect(client.POST).toHaveBeenCalledWith(
        '/api/api/match/spotify/albums',
        expect.objectContaining({
          body: mockRequest
        })
      )
    })

    it('should handle album API errors', async () => {
      const mockError = { message: 'API Error' }

      vi.mocked(client.POST).mockResolvedValueOnce({
        data: undefined,
        error: mockError
      })

      const { handleApiError } = await import('@/api/errors')

      const mockRequest = { albums: [] }

      try {
        await matchApi.matchAlbumsToSpotify(mockRequest)
      } catch {
        // Expected to throw
      }

      expect(handleApiError).toHaveBeenCalled()
    })
  })

  describe('searchAlbumsForManualMatch', () => {
    it('should search for albums on Spotify', async () => {
      const mockResults = [
        {
          id: 'albumId1',
          name: 'Test Album',
          artist: 'Test Artist',
          releaseDate: '2024-01-01',
          uri: 'spotify:album:albumId1',
          totalTracks: 10
        }
      ]

      vi.mocked(client.GET).mockResolvedValueOnce({
        data: mockResults,
        error: null
      })

      const result = await matchApi.searchAlbumsForManualMatch('test album')

      expect(result).toEqual(mockResults)
      expect(client.GET).toHaveBeenCalledWith(
        '/api/api/match/spotify/albums/search',
        expect.objectContaining({
          params: {
            query: {
              query: 'test album'
            }
          }
        })
      )
    })

    it('should handle no album results', async () => {
      vi.mocked(client.GET).mockResolvedValueOnce({
        data: null,
        error: null
      })

      const result = await matchApi.searchAlbumsForManualMatch('nonexistent')

      expect(result).toEqual([])
    })
  })

  describe('matchArtistsToSpotify', () => {
    it('should send match artists request to API', async () => {
      const mockRequest = {
        artists: [
          { name: 'Artist 1', sourceMetadata: {}, source: 'lastfm' }
        ]
      }

      const mockResponse = {
        artists: [
          {
            sourceArtist: mockRequest.artists[0],
            match: {
              spotifyId: 'artistId1',
              name: 'Artist 1',
              uri: 'spotify:artist:artistId1',
              topTracks: [],
              confidence: 100,
              method: 'Exact' as const
            },
            isMatched: true
          }
        ]
      }

      vi.mocked(client.POST).mockResolvedValueOnce({
        data: mockResponse,
        error: null
      })

      const result = await matchApi.matchArtistsToSpotify(mockRequest)

      expect(result).toEqual(mockResponse)
      expect(client.POST).toHaveBeenCalledWith(
        '/api/api/match/spotify/artists',
        expect.objectContaining({
          body: mockRequest
        })
      )
    })

    it('should handle artist API errors', async () => {
      const mockError = { message: 'API Error' }

      vi.mocked(client.POST).mockResolvedValueOnce({
        data: undefined,
        error: mockError
      })

      const { handleApiError } = await import('@/api/errors')

      const mockRequest = { artists: [] }

      try {
        await matchApi.matchArtistsToSpotify(mockRequest)
      } catch {
        // Expected to throw
      }

      expect(handleApiError).toHaveBeenCalled()
    })
  })

  describe('searchArtistsForManualMatch', () => {
    it('should search for artists on Spotify', async () => {
      const mockResults = [
        {
          id: 'artistId1',
          name: 'Test Artist',
          uri: 'spotify:artist:artistId1',
          genres: ['rock', 'pop']
        }
      ]

      vi.mocked(client.GET).mockResolvedValueOnce({
        data: mockResults,
        error: null
      })

      const result = await matchApi.searchArtistsForManualMatch('test artist')

      expect(result).toEqual(mockResults)
      expect(client.GET).toHaveBeenCalledWith(
        '/api/api/match/spotify/artists/search',
        expect.objectContaining({
          params: {
            query: {
              query: 'test artist'
            }
          }
        })
      )
    })

    it('should handle no artist results', async () => {
      vi.mocked(client.GET).mockResolvedValueOnce({
        data: null,
        error: null
      })

      const result = await matchApi.searchArtistsForManualMatch('nonexistent')

      expect(result).toEqual([])
    })
  })
})
