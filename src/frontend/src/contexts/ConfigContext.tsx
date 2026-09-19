import { createContext, useContext, useEffect, useRef, useState, type ReactNode } from 'react'
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
  const [lastfmConfig, setLastfmConfig] = useState<ConfigureLastfmResponse | null>(null)
  const [discogsConfig, setDiscogsConfig] = useState<ConfigureDiscogsResponse | null>(null)
  const [setlistConfig, setSetlistConfig] = useState<ConfigureSetlistResponse | null>(null)
  const [lastfmFilter, setLastfmFilter] = useState<LastfmFilter>(DEFAULT_FILTER)
  const [discogsFilter, setDiscogsFilter] = useState<DiscogsFilter>(DEFAULT_DISCOGS_FILTER)
  const [setlistFmFilter, setSetlistFmFilter] = useState<SetlistFmFilter>(DEFAULT_SETLISTFM_FILTER)
  const [setlistFmFetchMode, setSetlistFmFetchModeState] = useState<SetlistFmFetchMode>('quick')
  const [selectedSetlistConcertIdsByUser, setSelectedSetlistConcertIdsByUser] = useState<Record<string, string[]>>({})
  const selectedSetlistConcertIdsByUserRef = useRef<Record<string, string[]>>({})
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [autoFetch, setAutoFetchState] = useState(true)

  // Load configuration and filter from localStorage on mount
  useEffect(() => {
    const storedConfig = localStorage.getItem(LASTFM_CONFIG_KEY)
    if (storedConfig) {
      try {
        setLastfmConfig(JSON.parse(storedConfig))
      } catch {
        localStorage.removeItem(LASTFM_CONFIG_KEY)
      }
    }

    const storedDiscogs = localStorage.getItem(DISCOGS_CONFIG_KEY)
    if (storedDiscogs) {
      try {
        setDiscogsConfig(JSON.parse(storedDiscogs))
      } catch {
        localStorage.removeItem(DISCOGS_CONFIG_KEY)
      }
    }

    const storedSetlist = localStorage.getItem(SETLIST_CONFIG_KEY)
    if (storedSetlist) {
      try {
        setSetlistConfig(JSON.parse(storedSetlist))
      } catch {
        localStorage.removeItem(SETLIST_CONFIG_KEY)
      }
    }

    const storedFilter = localStorage.getItem(LASTFM_FILTER_KEY)
    if (storedFilter) {
      try {
        setLastfmFilter(JSON.parse(storedFilter))
      } catch {
        localStorage.removeItem(LASTFM_FILTER_KEY)
      }
    }

    const storedDiscogsFilter = localStorage.getItem(DISCOGS_FILTER_KEY)
    if (storedDiscogsFilter) {
      try {
        setDiscogsFilter(JSON.parse(storedDiscogsFilter))
      } catch {
        localStorage.removeItem(DISCOGS_FILTER_KEY)
      }
    }

    const storedSetlistFmFilter = localStorage.getItem(SETLISTFM_FILTER_KEY)
    if (storedSetlistFmFilter) {
      try {
        setSetlistFmFilter(JSON.parse(storedSetlistFmFilter))
      } catch {
        localStorage.removeItem(SETLISTFM_FILTER_KEY)
      }
    }

    const storedSetlistFmFetchMode = localStorage.getItem(SETLISTFM_FETCH_MODE_KEY)
    if (storedSetlistFmFetchMode === 'quick' || storedSetlistFmFetchMode === 'selectConcerts') {
      setSetlistFmFetchModeState(storedSetlistFmFetchMode)
    }

    const storedSelectedConcerts = localStorage.getItem(SETLISTFM_SELECTED_CONCERTS_KEY)
    if (storedSelectedConcerts) {
      try {
        const parsed: unknown = JSON.parse(storedSelectedConcerts)
        if (!isSelectedConcertsByUser(parsed)) {
          throw new Error('Invalid selected concert storage')
        }
        const deduped = dedupeSelectedConcertsByUser(parsed)
        selectedSetlistConcertIdsByUserRef.current = deduped
        setSelectedSetlistConcertIdsByUser(deduped)
      } catch {
        localStorage.removeItem(SETLISTFM_SELECTED_CONCERTS_KEY)
      }
    }
  }, [])

  async function configureLastfm(username: string) {
    setIsLoading(true)
    setError(null)
    try {
      const config = await configApi.configureLastfm(username)
      setLastfmConfig(config)
      // Persist to localStorage
      localStorage.setItem(LASTFM_CONFIG_KEY, JSON.stringify(config))
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
      localStorage.setItem(DISCOGS_CONFIG_KEY, JSON.stringify(config))
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
      localStorage.setItem(SETLIST_CONFIG_KEY, JSON.stringify(config))
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
