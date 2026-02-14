/**
 * Temporary type definitions for Discogs models.
 * These will be replaced once the OpenAPI schema is updated.
 */

export interface DiscogsFilter {
  minReleaseYear?: number
  maxReleaseYear?: number
  mediaFormat?: string
  minYearAdded?: number
  maxYearAdded?: number
  maxTracks?: number
}

export interface DiscogsTrack {
  name: string
  artist: string
  album?: string | null
  releaseYear?: number | null
}

export interface DiscogsRelease {
  id: number
  title: string
  artist: string
  year?: number | null
  format?: string | null
  dateAdded?: string | null
  tracks: DiscogsTrack[]
}

export interface DiscogsDataResponse {
  releases: DiscogsRelease[]
  tracks: DiscogsTrack[]
  totalReleases: number
  totalTracks: number
}
