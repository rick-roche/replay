import type { components } from './generated-client'
import { client } from './client'
import { handleApiError } from './errors'
import type { DiscogsFilter } from '../types/discogs'

type LastfmFilter = components['schemas']['LastfmFilter']
type NormalizedDataResponse = components['schemas']['NormalizedDataResponse']
type SetlistFmFilter = components['schemas']['SetlistFmFilter']

const SOURCES_LASTFM_DATA_PATH = '/api/sources/lastfm/data' as const
const SOURCES_DISCOGS_DATA_PATH = '/api/sources/discogs/data' as const
const SOURCES_SETLISTFM_DATA_PATH = '/api/sources/setlistfm/data' as const

export const sourcesApi = {
  /**
   * Fetch Last.fm data (normalized format for matching)
   */
  async fetchLastfmData(username: string, filter: LastfmFilter): Promise<NormalizedDataResponse> {
    const { data, error } = await client.POST(SOURCES_LASTFM_DATA_PATH, {
      body: { username, filter } as { username: string; filter: LastfmFilter }
    })
    if (error) handleApiError(error, 'Failed to fetch Last.fm data')
    return data!
  },

  /**
   * Fetch Discogs collection data (normalized format for matching)
   */
  async fetchDiscogsData(usernameOrCollectionId: string, filter: DiscogsFilter): Promise<NormalizedDataResponse> {
    try {
      const response = await fetch(SOURCES_DISCOGS_DATA_PATH, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ usernameOrCollectionId, filter })
      })
      if (!response.ok) {
        const errorData = await response.json()
        handleApiError(errorData, 'Failed to fetch Discogs data')
      }
      return await response.json()
    } catch (error) {
      handleApiError(error, 'Failed to fetch Discogs data')
      throw error
    }
  },

  /**
   * Fetch Setlist.fm concert data (normalized format for matching)
   */
  async fetchSetlistFmData(userId: string, filter: SetlistFmFilter): Promise<NormalizedDataResponse> {
    const { data, error } = await client.POST(SOURCES_SETLISTFM_DATA_PATH, {
      body: { userId, filter } as { userId: string; filter: SetlistFmFilter }
    })
    if (error) handleApiError(error, 'Failed to fetch Setlist.fm data')
    return data!
  }
}
