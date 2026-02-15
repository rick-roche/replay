import type { components } from './generated-client'
import { handleApiError } from './errors'
import { client } from './client'

type MatchTracksRequest = components['schemas']['MatchTracksRequest']
type MatchedDataResponse = components['schemas']['MatchedDataResponse']
type SpotifyTrack = components['schemas']['SpotifyTrack']
type MatchAlbumsRequest = components['schemas']['MatchAlbumsRequest']
type MatchArtistsRequest = components['schemas']['MatchArtistsRequest']
type MatchedAlbumsResponse = components['schemas']['MatchedAlbumsResponse']
type MatchedArtistsResponse = components['schemas']['MatchedArtistsResponse']
type SpotifyAlbumInfo = components['schemas']['SpotifyAlbumInfo']
type SpotifyArtistInfo = components['schemas']['SpotifyArtistInfo']

export const matchApi = {
  /**
   * Match normalized tracks to Spotify
   */
  async matchTracksToSpotify(request: MatchTracksRequest): Promise<MatchedDataResponse> {
    // Use the generated client path; backend OpenAPI currently exposes this double-prefixed route
    const { data, error } = await client.POST('/api/api/match/spotify', {
      body: request
    })
    if (error) handleApiError(error, 'Failed to match tracks to Spotify')
    return data!
  },

  /**
   * Search Spotify for tracks to manually match
   */
  async searchTracksForManualMatch(query: string): Promise<SpotifyTrack[]> {
    const { data, error } = await client.GET('/api/api/match/spotify/search', {
      params: {
        query: {
          query
        }
      }
    })
    if (error) handleApiError(error, 'Failed to search Spotify')
    return data || []
  },

  /**
   * Match normalized albums to Spotify and load tracks from each album
   */
  async matchAlbumsToSpotify(request: MatchAlbumsRequest): Promise<MatchedAlbumsResponse> {
    const { data, error } = await client.POST('/api/api/match/spotify/albums', {
      body: request
    })
    if (error) handleApiError(error, 'Failed to match albums to Spotify')
    return data!
  },

  /**
   * Search Spotify for albums
   */
  async searchAlbumsForManualMatch(query: string): Promise<SpotifyAlbumInfo[]> {
    const { data, error } = await client.GET('/api/api/match/spotify/albums/search', {
      params: {
        query: {
          query
        }
      }
    })
    if (error) handleApiError(error, 'Failed to search Spotify albums')
    return data || []
  },

  /**
   * Match normalized artists to Spotify and load top tracks from each artist
   */
  async matchArtistsToSpotify(request: MatchArtistsRequest): Promise<MatchedArtistsResponse> {
    const { data, error } = await client.POST('/api/api/match/spotify/artists', {
      body: request
    })
    if (error) handleApiError(error, 'Failed to match artists to Spotify')
    return data!
  },

  /**
   * Search Spotify for artists
   */
  async searchArtistsForManualMatch(query: string): Promise<SpotifyArtistInfo[]> {
    const { data, error } = await client.GET('/api/api/match/spotify/artists/search', {
      params: {
        query: {
          query
        }
      }
    })
    if (error) handleApiError(error, 'Failed to search Spotify artists')
    return data || []
  }
}

