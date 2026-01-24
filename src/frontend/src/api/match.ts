import type { components } from './generated-client'
import { handleApiError } from './errors'
import { client } from './client'

type MatchTracksRequest = components['schemas']['MatchTracksRequest']
type MatchedDataResponse = components['schemas']['MatchedDataResponse']
type SpotifyTrack = components['schemas']['SpotifyTrack']



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
  }
}

