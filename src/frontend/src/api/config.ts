import type { components } from './generated-client'
import { client } from './client'
import { handleApiError } from './errors'

type ConfigureLastfmRequest = components['schemas']['ConfigureLastfmRequest']
type ConfigureLastfmResponse = components['schemas']['ConfigureLastfmResponse']
type LastfmFilter = components['schemas']['LastfmFilter']
type LastfmDataResponse = components['schemas']['LastfmDataResponse']
type NormalizedDataResponse = components['schemas']['NormalizedDataResponse']

const CONFIG_LASTFM_PATH = '/api/config/lastfm' as const
const SOURCES_LASTFM_DATA_PATH = '/api/sources/lastfm/data' as const

export const configApi = {
  /**
   * Configure a Last.fm username
   */
  async configureLastfm(username: string): Promise<ConfigureLastfmResponse> {
    const { data, error } = await client.POST(CONFIG_LASTFM_PATH, {
      body: { username } as ConfigureLastfmRequest
    })
    if (error) handleApiError(error, 'Failed to configure Last.fm')
    return data!
  },

  /**
   * Fetch Last.fm data (tracks, albums, or artists) with specified filters
   */
  async fetchLastfmData(username: string, filter: LastfmFilter): Promise<LastfmDataResponse> {
    const { data, error } = await client.POST(SOURCES_LASTFM_DATA_PATH, {
      body: { username, filter } as { username: string; filter: LastfmFilter }
    })
    if (error) handleApiError(error, 'Failed to fetch Last.fm data')
    return data!
  },

  /**
   * Fetch normalized Last.fm data (tracks, albums, or artists) with specified filters
   */
  async fetchLastfmDataNormalized(username: string, filter: LastfmFilter): Promise<NormalizedDataResponse> {
    const { data, error } = await client.POST('/api/sources/lastfm/data/normalized', {
      body: { username, filter } as { username: string; filter: LastfmFilter }
    })
    if (error) handleApiError(error, 'Failed to fetch normalized Last.fm data')
    return data!
  }
}
