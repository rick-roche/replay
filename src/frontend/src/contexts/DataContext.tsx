import { createContext, useContext, useState, useCallback, useRef, type ReactNode } from 'react'
import type { components } from '../api/generated-client'
import { sourcesApi, type SetlistConcertsResponse } from '../api/sources'
import type { DiscogsFilter } from '../types/discogs'

type LastfmFilter = components['schemas']['LastfmFilter']
type SetlistFmFilter = components['schemas']['SetlistFmFilter']
type NormalizedDataResponse = components['schemas']['NormalizedDataResponse']
type NormalizedTrack = components['schemas']['NormalizedTrack']
type NormalizedAlbum = components['schemas']['NormalizedAlbum']
type NormalizedArtist = components['schemas']['NormalizedArtist']

interface DataContextValue {
  normalizedData: NormalizedDataResponse | null
  setlistConcertsPage: SetlistConcertsResponse | null
  isLoading: boolean
  isLoadingConcerts: boolean
  error: string | null
  fetchData: (username: string, filter: LastfmFilter) => Promise<void>
  fetchMoreData: (username: string, filter: LastfmFilter) => Promise<NormalizedTrack[]>
  fetchSetlistFmData: (userId: string, filter: SetlistFmFilter, selectedConcertIds?: string[]) => Promise<void>
  fetchSetlistFmConcerts: (userId: string, filter: SetlistFmFilter, pageNumber: number) => Promise<void>
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
  const [setlistConcertsPage, setSetlistConcertsPage] = useState<SetlistConcertsResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [isLoadingConcerts, setIsLoadingConcerts] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const concertRequestId = useRef(0)

  const fetchData = useCallback(async (username: string, filter: LastfmFilter) => {
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
  }, [])

  const fetchMoreData = useCallback(async (username: string, filter: LastfmFilter) => {
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
  }, [normalizedData])

  const fetchSetlistFmData = useCallback(async (userId: string, filter: SetlistFmFilter, selectedConcertIds?: string[]) => {
    setIsLoading(true)
    setError(null)
    setNormalizedData(null)

    try {
      const normalized = await sourcesApi.fetchSetlistFmData(userId, filter, selectedConcertIds)
      setNormalizedData(normalized)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch Setlist.fm data')
    } finally {
      setIsLoading(false)
    }
  }, [])

  const fetchSetlistFmConcerts = useCallback(async (userId: string, filter: SetlistFmFilter, pageNumber: number) => {
    const requestId = ++concertRequestId.current
    setIsLoadingConcerts(true)
    setError(null)
    setSetlistConcertsPage(null)

    try {
      const concertsPage = await sourcesApi.fetchSetlistFmConcerts(userId, filter, pageNumber)
      if (requestId === concertRequestId.current) {
        setSetlistConcertsPage(concertsPage)
      }
    } catch (err) {
      if (requestId === concertRequestId.current) {
        setError(err instanceof Error ? err.message : 'Failed to fetch Setlist.fm concerts')
      }
    } finally {
      if (requestId === concertRequestId.current) {
        setIsLoadingConcerts(false)
      }
    }
  }, [])

  const fetchDiscogsData = useCallback(async (username: string, filter: DiscogsFilter) => {
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
  }, [])

  const clearData = useCallback(() => {
    setNormalizedData(null)
    setSetlistConcertsPage(null)
  }, [])

  const clearError = useCallback(() => {
    setError(null)
  }, [])

  const value: DataContextValue = {
    normalizedData,
    setlistConcertsPage,
    isLoading,
    isLoadingConcerts,
    error,
    fetchData,
    fetchMoreData,
    fetchSetlistFmData,
    fetchSetlistFmConcerts,
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
