/**
 * Filter definitions for Discogs API calls.
 */

export interface DiscogsFilter {
  minReleaseYear?: number
  maxReleaseYear?: number
  mediaFormat?: string
  minYearAdded?: number
  maxYearAdded?: number
  maxTracks?: number
}
