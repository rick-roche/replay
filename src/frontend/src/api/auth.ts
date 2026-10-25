import type { SessionInfo } from '../types/auth'
import { client } from './client'

const API_BASE = '/api/auth'

export const authApi = {
  /**
   * Initiate Spotify OAuth login
   */
  login(): void {
    const returnUrl = window.location.pathname + window.location.search + window.location.hash
    window.location.href = `${API_BASE}/login?returnUrl=${encodeURIComponent(returnUrl)}`
  },

  /**
   * Get current session information
   */
  async getSession(): Promise<SessionInfo | null> {
    try {
      const { data, response } = await client.GET('/api/auth/session')
      return response.ok ? data ?? null : null
    } catch {
      return null
    }
  },

  /**
   * Refresh the current session
   */
  async refresh(): Promise<SessionInfo | null> {
    const { data, response } = await client.POST('/api/auth/refresh')

    if (response.status === 401) return null
    if (!response.ok) throw new Error('Session refresh temporarily unavailable')

    return data ?? null
  },

  /**
   * Logout and clear session
   */
  async logout(): Promise<void> {
    await client.POST('/api/auth/logout')
  }
}
