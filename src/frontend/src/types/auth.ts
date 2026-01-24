export interface SpotifyUser {
  id: string;
  displayName: string;
  imageUrl?: string;
  externalUrl?: string;
}

export interface SessionInfo {
  isAuthenticated: boolean;
  user?: SpotifyUser;
  expiresAt: string;
  source?: string;
}
