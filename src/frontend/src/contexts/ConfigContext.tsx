import { createContext, useContext, useRef, useState, type ReactNode } from 'react'
import type { components } from '../api/generated-client'
import { configApi } from '../api/config'
import type { DiscogsFilter } from '../types/discogs'

type ConfigureLastfmResponse = components['schemas']['ConfigureLastfmResponse']
type ConfigureDiscogsResponse = components['schemas']['ConfigureDiscogsResponse']
type ConfigureSetlistResponse = components['schemas']['ConfigureSetlistResponse']
type LastfmFilter = components['schemas']['LastfmFilter']
type SetlistFmFilter = components['schemas']['SetlistFmFilter']
type SetlistFmFetchMode = 'quick' | 'selectConcerts'

interface ConfigContextValue {
  lastfmConfig: ConfigureLastfmResponse | null
  discogsConfig: ConfigureDiscogsResponse | null
  setlistConfig: ConfigureSetlistResponse | null
  lastfmFilter: LastfmFilter
  discogsFilter: DiscogsFilter
  setlistFmFilter: SetlistFmFilter
  setlistFmFetchMode: SetlistFmFetchMode
  isLoading: boolean
  error: string | null
  autoFetch: boolean
  configureLastfm: (username: string) => Promise<void>
  configureDiscogs: (identifier: string) => Promise<void>
  configureSetlistFm: (usernameOrId: string) => Promise<void>
  updateFilter: (updates: Partial<LastfmFilter>) => void
  updateDiscogsFilter: (updates: Partial<DiscogsFilter>) => void
  updateSetlistFmFilter: (updates: Partial<SetlistFmFilter>) => void
  setSetlistFmFetchMode: (mode: SetlistFmFetchMode) => void
  getSelectedSetlistConcertIds: (userId: string) => string[]
  setSelectedSetlistConcertIds: (userId: string, concertIds: string[]) => void
  clearSelectedSetlistConcertIds: (userId: string) => void
  setAutoFetch: (value: boolean) => void
  clearError: () => void
}

const ConfigContext = createContext<ConfigContextValue | null>(null)

const LASTFM_CONFIG_KEY = 'replay:lastfm_config'
const DISCOGS_CONFIG_KEY = 'replay:discogs_config'
const SETLIST_CONFIG_KEY = 'replay:setlist_config'
const LASTFM_FILTER_KEY = 'replay:lastfm_filter'
const DISCOGS_FILTER_KEY = 'replay:discogs_filter'
const SETLISTFM_FILTER_KEY = 'replay:setlistfm_filter'
const SETLISTFM_FETCH_MODE_KEY = 'replay:setlistfm_fetch_mode'
const SETLISTFM_SELECTED_CONCERTS_KEY = 'replay:setlistfm_selected_concerts'

const DEFAULT_FILTER: LastfmFilter = {
  dataType: 'Tracks',
  timePeriod: 'Last12Months',
  maxResults: 50
}

const DEFAULT_DISCOGS_FILTER: DiscogsFilter = {
  maxTracks: 100
}

const DEFAULT_SETLISTFM_FILTER: SetlistFmFilter = {
  maxConcerts: 10,
  maxTracks: 100
}

export function ConfigProvider({ children }: { children: ReactNode }) {
  // Stored profile names are input preferences, not proof of a live server session.
  const [lastfmConfig, setLastfmConfig] = useState<ConfigureLastfmResponse | null>(() => {
    const stored = readStored(LASTFM_CONFIG_KEY)
    return isRecord(stored) && typeof stored.username === 'string'
      ? { username: stored.username, isConfigured: false, playCount: 0, profileUrl: '' } : null
  })
  const [discogsConfig, setDiscogsConfig] = useState<ConfigureDiscogsResponse | null>(() => {
    const stored = readStored(DISCOGS_CONFIG_KEY)
    return isRecord(stored) && typeof stored.username === 'string'
      ? { username: stored.username, isConfigured: false, releaseCount: 0, collectionUrl: '' } : null
  })
  const [setlistConfig, setSetlistConfig] = useState<ConfigureSetlistResponse | null>(() => {
    const stored = readStored(SETLIST_CONFIG_KEY)
    return isRecord(stored) && typeof stored.userId === 'string'
      ? { userId: stored.userId, displayName: typeof stored.displayName === 'string' ? stored.displayName : stored.userId, isConfigured: false } : null
  })
  const [lastfmFilter, setLastfmFilter] = useState<LastfmFilter>(() => loadLastfmFilter())
  const [discogsFilter, setDiscogsFilter] = useState<DiscogsFilter>(() => loadDiscogsFilter())
  const [setlistFmFilter, setSetlistFmFilter] = useState<SetlistFmFilter>(() => loadSetlistFilter())
  const [setlistFmFetchMode, setSetlistFmFetchModeState] = useState<SetlistFmFetchMode>(() => localStorage.getItem(SETLISTFM_FETCH_MODE_KEY) === 'selectConcerts' ? 'selectConcerts' : 'quick')
  const [selectedSetlistConcertIdsByUser, setSelectedSetlistConcertIdsByUser] = useState<Record<string, string[]>>(() => {
    const stored = readStored(SETLISTFM_SELECTED_CONCERTS_KEY)
    if (isSelectedConcertsByUser(stored)) return dedupeSelectedConcertsByUser(stored)
    localStorage.removeItem(SETLISTFM_SELECTED_CONCERTS_KEY)
    return {}
  })
  const selectedSetlistConcertIdsByUserRef = useRef(selectedSetlistConcertIdsByUser)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [autoFetch, setAutoFetchState] = useState(true)

  async function configureLastfm(username: string) {
    setIsLoading(true)
    setError(null)
    try {
      const config = await configApi.configureLastfm(username)
      setLastfmConfig(config)
      // Persist to localStorage
      localStorage.setItem(LASTFM_CONFIG_KEY, JSON.stringify({ ...config, isConfigured: false }))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error')
    } finally {
      setIsLoading(false)
    }
  }

  async function configureDiscogs(identifier: string) {
    setIsLoading(true)
    setError(null)
    try {
      const config = await configApi.configureDiscogs(identifier)
      setDiscogsConfig(config)
      localStorage.setItem(DISCOGS_CONFIG_KEY, JSON.stringify({ ...config, isConfigured: false }))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error')
    } finally {
      setIsLoading(false)
    }
  }

  async function configureSetlistFm(usernameOrId: string) {
    setIsLoading(true)
    setError(null)
    try {
      const config = await configApi.configureSetlistFm(usernameOrId)
      setSetlistConfig(config)
      localStorage.setItem(SETLIST_CONFIG_KEY, JSON.stringify({ ...config, isConfigured: false }))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error')
    } finally {
      setIsLoading(false)
    }
  }

  function updateFilter(updates: Partial<LastfmFilter>) {
    const newFilter = { ...lastfmFilter, ...updates }
    setLastfmFilter(newFilter)
    // Persist to localStorage
    localStorage.setItem(LASTFM_FILTER_KEY, JSON.stringify(newFilter))
  }

  function updateDiscogsFilter(updates: Partial<DiscogsFilter>) {
    const newFilter = { ...discogsFilter, ...updates }
    setDiscogsFilter(newFilter)
    // Persist to localStorage
    localStorage.setItem(DISCOGS_FILTER_KEY, JSON.stringify(newFilter))
  }

  function updateSetlistFmFilter(updates: Partial<SetlistFmFilter>) {
    const newFilter = { ...setlistFmFilter, ...updates }
    setSetlistFmFilter(newFilter)
    // Persist to localStorage
    localStorage.setItem(SETLISTFM_FILTER_KEY, JSON.stringify(newFilter))
  }

  function setSetlistFmFetchMode(mode: SetlistFmFetchMode) {
    setSetlistFmFetchModeState(mode)
    localStorage.setItem(SETLISTFM_FETCH_MODE_KEY, mode)
  }

  function getSelectedSetlistConcertIds(userId: string): string[] {
    return selectedSetlistConcertIdsByUser[userId] ?? []
  }

  function setSelectedSetlistConcertIds(userId: string, concertIds: string[]) {
    const seenConcertIds = new Set<string>()
    const deduped = concertIds.flatMap((concertId) => {
      const trimmedId = concertId.trim()
      const normalizedId = trimmedId.toLowerCase()
      if (!trimmedId || seenConcertIds.has(normalizedId)) {
        return []
      }

      seenConcertIds.add(normalizedId)
      return [trimmedId]
    })
    const next = {
      ...selectedSetlistConcertIdsByUserRef.current,
      [userId]: deduped
    }

    selectedSetlistConcertIdsByUserRef.current = next
    setSelectedSetlistConcertIdsByUser(next)
    localStorage.setItem(SETLISTFM_SELECTED_CONCERTS_KEY, JSON.stringify(next))
  }

  function clearSelectedSetlistConcertIds(userId: string) {
    if (!(userId in selectedSetlistConcertIdsByUserRef.current)) {
      return
    }

    const next = { ...selectedSetlistConcertIdsByUserRef.current }
    delete next[userId]
    selectedSetlistConcertIdsByUserRef.current = next
    setSelectedSetlistConcertIdsByUser(next)
    localStorage.setItem(SETLISTFM_SELECTED_CONCERTS_KEY, JSON.stringify(next))
  }

  function clearError() {
    setError(null)
  }

  function setAutoFetch(value: boolean) {
    setAutoFetchState(value)
  }

  const value: ConfigContextValue = {
    lastfmConfig,
    discogsConfig,
    setlistConfig,
    lastfmFilter,
    discogsFilter,
    setlistFmFilter,
    setlistFmFetchMode,
    isLoading,
    error,
    autoFetch,
    configureLastfm,
    configureDiscogs,
    configureSetlistFm,
    updateFilter,
    updateDiscogsFilter,
    updateSetlistFmFilter,
    setSetlistFmFetchMode,
    getSelectedSetlistConcertIds,
    setSelectedSetlistConcertIds,
    clearSelectedSetlistConcertIds,
    setAutoFetch,
    clearError
  }

  return <ConfigContext.Provider value={value}>{children}</ConfigContext.Provider>
}

function readStored(key: string): unknown {
  try {
    const stored = localStorage.getItem(key)
    return stored ? JSON.parse(stored) : null
  } catch {
    localStorage.removeItem(key)
    return null
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function loadLastfmFilter(): LastfmFilter {
  const stored = readStored(LASTFM_FILTER_KEY)
  if (!isRecord(stored)) return DEFAULT_FILTER
  const { dataType, timePeriod } = stored
  if (dataType !== 'Tracks' && dataType !== 'Albums' && dataType !== 'Artists') return DEFAULT_FILTER
  if (timePeriod !== 'Last7Days' && timePeriod !== 'Last1Month' && timePeriod !== 'Last3Months' && timePeriod !== 'Last6Months' && timePeriod !== 'Last12Months' && timePeriod !== 'Overall' && timePeriod !== 'Custom') return DEFAULT_FILTER
  return {
    dataType, timePeriod,
    maxResults: positiveNumber(stored.maxResults) ?? DEFAULT_FILTER.maxResults,
    customStartDate: typeof stored.customStartDate === 'string' ? stored.customStartDate : undefined,
    customEndDate: typeof stored.customEndDate === 'string' ? stored.customEndDate : undefined,
  }
}

function positiveNumber(value: unknown): number | undefined {
  return typeof value === 'number' && Number.isFinite(value) && value > 0 ? value : undefined
}

function loadDiscogsFilter(): DiscogsFilter {
  const stored = readStored(DISCOGS_FILTER_KEY)
  if (!isRecord(stored)) return DEFAULT_DISCOGS_FILTER
  return {
    minReleaseYear: positiveNumber(stored.minReleaseYear), maxReleaseYear: positiveNumber(stored.maxReleaseYear),
    minYearAdded: positiveNumber(stored.minYearAdded), maxYearAdded: positiveNumber(stored.maxYearAdded),
    mediaFormat: typeof stored.mediaFormat === 'string' ? stored.mediaFormat : undefined,
    maxTracks: positiveNumber(stored.maxTracks) ?? DEFAULT_DISCOGS_FILTER.maxTracks,
  }
}

function loadSetlistFilter(): SetlistFmFilter {
  const stored = readStored(SETLISTFM_FILTER_KEY)
  if (!isRecord(stored)) return DEFAULT_SETLISTFM_FILTER
  return {
    maxConcerts: positiveNumber(stored.maxConcerts) ?? DEFAULT_SETLISTFM_FILTER.maxConcerts,
    maxTracks: positiveNumber(stored.maxTracks) ?? DEFAULT_SETLISTFM_FILTER.maxTracks,
    startDate: typeof stored.startDate === 'string' ? stored.startDate : undefined,
    endDate: typeof stored.endDate === 'string' ? stored.endDate : undefined,
  }
}

function isSelectedConcertsByUser(value: unknown): value is Record<string, string[]> {
  return typeof value === 'object' && value !== null && !Array.isArray(value) && Object.values(value).every(
    (concertIds) => Array.isArray(concertIds) && concertIds.every((concertId) => typeof concertId === 'string')
  )
}

function dedupeSelectedConcertsByUser(data: Record<string, string[]>): Record<string, string[]> {
  return Object.fromEntries(
    Object.entries(data).map(([userId, ids]) => {
      const seen = new Set<string>()
      const deduped = ids.flatMap((id) => {
        const trimmed = id.trim()
        const normalized = trimmed.toLowerCase()
        if (!trimmed || seen.has(normalized)) return []
        seen.add(normalized)
        return [trimmed]
      })
      return [userId, deduped]
    })
  )
}

export function useConfig() {
  const context = useContext(ConfigContext)
  if (!context) {
    throw new Error('useConfig must be used within ConfigProvider')
  }
  return context
}
