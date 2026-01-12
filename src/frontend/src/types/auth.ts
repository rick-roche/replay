// Frontend types matching backend models

export interface SpotifyUser {
  id: string
  displayName: string
  email?: string | null
  imageUrl?: string | null
}

export interface SessionInfo {
  sessionId: string
  user: SpotifyUser
  expiresAt: string
}
