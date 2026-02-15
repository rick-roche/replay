import { createContext, useContext, useState, type ReactNode } from 'react'
import type { components } from '../api/generated-client'
import { sourcesApi } from '../api/sources'
import type { DiscogsFilter } from '../types/discogs'

type LastfmFilter = components['schemas']['LastfmFilter']
type SetlistFmFilter = components['schemas']['SetlistFmFilter']
type NormalizedDataResponse = components['schemas']['NormalizedDataResponse']
type NormalizedTrack = components['schemas']['NormalizedTrack']
type NormalizedAlbum = components['schemas']['NormalizedAlbum']
type NormalizedArtist = components['schemas']['NormalizedArtist']

interface DataContextValue {
  normalizedData: NormalizedDataResponse | null
  isLoading: boolean
  error: string | null
  fetchData: (username: string, filter: LastfmFilter) => Promise<void>
  fetchMoreData: (username: string, filter: LastfmFilter) => Promise<NormalizedTrack[]>
  fetchSetlistFmData: (userId: string, filter: SetlistFmFilter) => Promise<void>
  fetchDiscogsData: (username: string, filter: DiscogsFilter) => Promise<void>
  clearData: () => void
  clearError: () => void
}

const DataContext = createContext<DataContextValue | null>(null)

const trackKey = (t: { name?: string | null; artist?: string | null; album?: string | null }) =>
  `${(t.name ?? '').toLowerCase()}|${(t.artist ?? '').toLowerCase()}|${(t.album ?? '').toLowerCase()}`

const albumKey = (a: { name?: string | null; artist?: string | null }) =>
  `${(a.name ?? '').toLowerCase()}|${(a.artist ?? '').toLowerCase()}`

const artistKey = (a: { name?: string | null }) => `${(a.name ?? '').toLowerCase()}`

const mergeNormalizedTracks = (existing: NormalizedTrack[], incoming: NormalizedTrack[]) => {
  const seen = new Set(existing.map(trackKey))
  const merged = [...existing]
  for (const t of incoming) {
    const key = trackKey(t)
    if (!seen.has(key)) {
      seen.add(key)
      merged.push(t)
    }
  }
  return merged
}

const mergeNormalizedAlbums = (existing: NormalizedAlbum[], incoming: NormalizedAlbum[]) => {
  const seen = new Set(existing.map(albumKey))
  const merged = [...existing]
  for (const a of incoming) {
    const key = albumKey(a)
    if (!seen.has(key)) {
      seen.add(key)
      merged.push(a)
    }
  }
  return merged
}

const mergeNormalizedArtists = (existing: NormalizedArtist[], incoming: NormalizedArtist[]) => {
  const seen = new Set(existing.map(artistKey))
  const merged = [...existing]
  for (const a of incoming) {
    const key = artistKey(a)
    if (!seen.has(key)) {
      seen.add(key)
      merged.push(a)
    }
  }
  return merged
}

export function DataProvider({ children }: { children: ReactNode }) {
  const [normalizedData, setNormalizedData] = useState<NormalizedDataResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function fetchData(username: string, filter: LastfmFilter) {
    setIsLoading(true)
    setError(null)
    setNormalizedData(null)

    try {
      const normalized = await sourcesApi.fetchLastfmData(username, filter)
      setNormalizedData(normalized)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch data')
    } finally {
      setIsLoading(false)
    }
  }

  async function fetchMoreData(username: string, filter: LastfmFilter) {
    setIsLoading(true)
    setError(null)

    try {
      const normalized = await sourcesApi.fetchLastfmData(username, filter)

      // If data types differ from existing, treat as a fresh set
      if (!normalizedData || normalizedData.dataType !== normalized.dataType) {
        setNormalizedData(normalized)
        return normalized.tracks ?? []
      }

      // Merge normalized results without duplicates
      const mergedNormalizedTracks = mergeNormalizedTracks(normalizedData?.tracks ?? [], normalized.tracks ?? [])
      const mergedNormalizedAlbums = mergeNormalizedAlbums(normalizedData?.albums ?? [], normalized.albums ?? [])
      const mergedNormalizedArtists = mergeNormalizedArtists(normalizedData?.artists ?? [], normalized.artists ?? [])

      const mergedNormalized: NormalizedDataResponse = {
        ...normalized,
        tracks: mergedNormalizedTracks,
        albums: mergedNormalizedAlbums,
        artists: mergedNormalizedArtists,
        totalResults: normalized.totalResults ?? (mergedNormalizedTracks.length || mergedNormalizedAlbums.length || mergedNormalizedArtists.length)
      }

      setNormalizedData(mergedNormalized)

      // Return only the newly-added normalized tracks to allow matching append
      const existingKeys = new Set((normalizedData?.tracks ?? []).map(trackKey))
      const newlyAdded = (normalized.tracks ?? []).filter((t) => !existingKeys.has(trackKey(t)))
      return newlyAdded
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch more data')
      return []
    } finally {
      setIsLoading(false)
    }
  }

  async function fetchSetlistFmData(userId: string, filter: SetlistFmFilter) {
    setIsLoading(true)
    setError(null)
    setNormalizedData(null)

    try {
      const normalized = await sourcesApi.fetchSetlistFmData(userId, filter)
      setNormalizedData(normalized)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch Setlist.fm data')
    } finally {
      setIsLoading(false)
    }
  }

  async function fetchDiscogsData(username: string, filter: DiscogsFilter) {
    setIsLoading(true)
    setError(null)
    setNormalizedData(null)

    try {
      const normalized = await sourcesApi.fetchDiscogsData(username, filter)
      setNormalizedData(normalized)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch Discogs data')
    } finally {
      setIsLoading(false)
    }
  }

  function clearData() {
    setNormalizedData(null)
  }

  function clearError() {
    setError(null)
  }

  const value: DataContextValue = {
    normalizedData,
    isLoading,
    error,
    fetchData,
    fetchMoreData,
    fetchSetlistFmData,
    fetchDiscogsData,
    clearData,
    clearError
  }

  return <DataContext.Provider value={value}>{children}</DataContext.Provider>
}

export function useData() {
  const context = useContext(DataContext)
  if (!context) {
    throw new Error('useData must be used within DataProvider')
  }
  return context
}
