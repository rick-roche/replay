import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import type { components } from '../api/generated-client'
import { configApi } from '../api/config'

type ConfigureLastfmResponse = components['schemas']['ConfigureLastfmResponse']
type LastfmFilter = components['schemas']['LastfmFilter']

interface ConfigContextValue {
  lastfmConfig: ConfigureLastfmResponse | null
  lastfmFilter: LastfmFilter
  isLoading: boolean
  error: string | null
  configureLastfm: (username: string) => Promise<void>
  updateFilter: (updates: Partial<LastfmFilter>) => void
  clearError: () => void
}

const ConfigContext = createContext<ConfigContextValue | null>(null)

const LASTFM_CONFIG_KEY = 'replay:lastfm_config'
const LASTFM_FILTER_KEY = 'replay:lastfm_filter'

const DEFAULT_FILTER: LastfmFilter = {
  dataType: 'Tracks',
  timePeriod: 'Last12Months',
  maxResults: 50
}

export function ConfigProvider({ children }: { children: ReactNode }) {
  const [lastfmConfig, setLastfmConfig] = useState<ConfigureLastfmResponse | null>(null)
  const [lastfmFilter, setLastfmFilter] = useState<LastfmFilter>(DEFAULT_FILTER)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

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

    const storedFilter = localStorage.getItem(LASTFM_FILTER_KEY)
    if (storedFilter) {
      try {
        setLastfmFilter(JSON.parse(storedFilter))
      } catch {
        localStorage.removeItem(LASTFM_FILTER_KEY)
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

  function updateFilter(updates: Partial<LastfmFilter>) {
    const newFilter = { ...lastfmFilter, ...updates }
    setLastfmFilter(newFilter)
    // Persist to localStorage
    localStorage.setItem(LASTFM_FILTER_KEY, JSON.stringify(newFilter))
  }

  function clearError() {
    setError(null)
  }

  const value: ConfigContextValue = {
    lastfmConfig,
    lastfmFilter,
    isLoading,
    error,
    configureLastfm,
    updateFilter,
    clearError
  }

  return <ConfigContext.Provider value={value}>{children}</ConfigContext.Provider>
}

export function useConfig() {
  const context = useContext(ConfigContext)
  if (!context) {
    throw new Error('useConfig must be used within ConfigProvider')
  }
  return context
}
