import type { SessionInfo } from '../types/auth'

const API_BASE = '/api/auth'

export const authApi = {
  /**
   * Initiate Spotify OAuth login
   */
  login(): void {
    const returnUrl = window.location.href
    window.location.href = `${API_BASE}/login?returnUrl=${encodeURIComponent(returnUrl)}`
  },

  /**
   * Get current session information
   */
  async getSession(): Promise<SessionInfo | null> {
    try {
      const response = await fetch(`${API_BASE}/session`, {
        credentials: 'include'
      })

      if (!response.ok) {
        return null
      }

      return await response.json()
    } catch {
      return null
    }
  },

  /**
   * Refresh the current session
   */
  async refresh(): Promise<SessionInfo | null> {
    try {
      const response = await fetch(`${API_BASE}/refresh`, {
        method: 'POST',
        credentials: 'include'
      })

      if (!response.ok) {
        return null
      }

      return await response.json()
    } catch {
      return null
    }
  },

  /**
   * Logout and clear session
   */
  async logout(): Promise<void> {
    await fetch(`${API_BASE}/logout`, {
      method: 'POST',
      credentials: 'include'
    })
  }
}
