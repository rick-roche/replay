import type { ConfigureLastfmRequest, ConfigureLastfmResponse } from '../types/lastfm'

const API_BASE = '/api/config'

export const configApi = {
  /**
   * Configure a Last.fm username
   */
  async configureLastfm(username: string): Promise<ConfigureLastfmResponse> {
    const response = await fetch(`${API_BASE}/lastfm`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      credentials: 'include',
      body: JSON.stringify({ username } as ConfigureLastfmRequest)
    })

    if (!response.ok) {
      const error = await response.json()
      throw new Error(error.error || 'Failed to configure Last.fm')
    }

    return await response.json()
  },

  /**
   * Get current Last.fm configuration
   */
  async getLastfmConfig(): Promise<ConfigureLastfmResponse> {
    const response = await fetch(`${API_BASE}/lastfm`, {
      credentials: 'include'
    })

    if (!response.ok) {
      throw new Error('Failed to fetch Last.fm config')
    }

    return await response.json()
  }
}
