import type { components } from './generated-client'
import { client } from './client'
import { handleApiError } from './errors'

type ConfigureLastfmRequest = components['schemas']['ConfigureLastfmRequest']
type ConfigureLastfmResponse = components['schemas']['ConfigureLastfmResponse']
type ConfigureDiscogsRequest = components['schemas']['ConfigureDiscogsRequest']
type ConfigureDiscogsResponse = components['schemas']['ConfigureDiscogsResponse']
type ConfigureSetlistRequest = components['schemas']['ConfigureSetlistRequest']
type ConfigureSetlistResponse = components['schemas']['ConfigureSetlistResponse']

const CONFIG_LASTFM_PATH = '/api/config/lastfm' as const
const CONFIG_DISCOGS_PATH = '/api/config/discogs' as const
const CONFIG_SETLIST_PATH = '/api/config/setlistfm' as const

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
  }
}
