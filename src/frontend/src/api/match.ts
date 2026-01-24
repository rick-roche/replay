import type { components } from './generated-client'
import { handleApiError } from './errors'
import { client } from './client'

type MatchTracksRequest = components['schemas']['MatchTracksRequest']
type MatchedDataResponse = components['schemas']['MatchedDataResponse']



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
  }
}
