import type { components } from './generated-client'
import { client } from './client'
import { handleApiError } from './errors'

type ConfigureLastfmRequest = components['schemas']['ConfigureLastfmRequest']
type ConfigureLastfmResponse = components['schemas']['ConfigureLastfmResponse']
type ConfigureDiscogsRequest = components['schemas']['ConfigureDiscogsRequest']
type ConfigureDiscogsResponse = components['schemas']['ConfigureDiscogsResponse']
type ConfigureSetlistRequest = components['schemas']['ConfigureSetlistRequest']
type ConfigureSetlistResponse = components['schemas']['ConfigureSetlistResponse']
type LastfmFilter = components['schemas']['LastfmFilter']
type LastfmDataResponse = components['schemas']['LastfmDataResponse']
type NormalizedDataResponse = components['schemas']['NormalizedDataResponse']
type SetlistFmFilter = components['schemas']['SetlistFmFilter']
type SetlistFmDataResponse = components['schemas']['SetlistFmDataResponse']

const CONFIG_LASTFM_PATH = '/api/config/lastfm' as const
const CONFIG_DISCOGS_PATH = '/api/config/discogs' as const
const CONFIG_SETLIST_PATH = '/api/config/setlistfm' as const
const SOURCES_LASTFM_DATA_PATH = '/api/sources/lastfm/data' as const
const SOURCES_SETLISTFM_DATA_PATH = '/api/sources/setlistfm/data' as const

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
   * Configure a Discogs profile
   */
  async configureDiscogs(identifier: string): Promise<ConfigureDiscogsResponse> {
    const { data, error } = await client.POST(CONFIG_DISCOGS_PATH, {
      body: { usernameOrCollectionId: identifier } as ConfigureDiscogsRequest
    })
    if (error) handleApiError(error, 'Failed to configure Discogs')
    return data!
  },

  /**
   * Configure a Setlist.fm username or ID
   */
  async configureSetlistFm(usernameOrId: string): Promise<ConfigureSetlistResponse> {
    const { data, error } = await client.POST(CONFIG_SETLIST_PATH, {
      body: { usernameOrId } as ConfigureSetlistRequest
    })
    if (error) handleApiError(error, 'Failed to configure Setlist.fm')
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
  },

  /**
   * Fetch Setlist.fm concert data with specified filters
   */
  async fetchSetlistFmData(userId: string, filter: SetlistFmFilter): Promise<SetlistFmDataResponse> {
    const { data, error } = await client.POST(SOURCES_SETLISTFM_DATA_PATH, {
      body: { userId, filter } as { userId: string; filter: SetlistFmFilter }
    })
    if (error) handleApiError(error, 'Failed to fetch Setlist.fm data')
    return data!
  },

  /**
   * Fetch normalized Setlist.fm concert data with specified filters
   */
  async fetchSetlistFmDataNormalized(userId: string, filter: SetlistFmFilter): Promise<NormalizedDataResponse> {
    const { data, error } = await client.POST('/api/sources/setlistfm/data/normalized', {
      body: { userId, filter } as { userId: string; filter: SetlistFmFilter }
    })
    if (error) handleApiError(error, 'Failed to fetch normalized Setlist.fm data')
    return data!
  }
}
