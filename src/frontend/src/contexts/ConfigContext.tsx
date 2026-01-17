import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import type { ConfigureLastfmResponse } from '../types/lastfm'
import { configApi } from '../api/config'

interface ConfigContextValue {
  lastfmConfig: ConfigureLastfmResponse | null
  isLoading: boolean
  error: string | null
  configureLastfm: (username: string) => Promise<void>
  clearError: () => void
}

const ConfigContext = createContext<ConfigContextValue | null>(null)

const LASTFM_CONFIG_KEY = 'replay:lastfm_config'

export function ConfigProvider({ children }: { children: ReactNode }) {
  const [lastfmConfig, setLastfmConfig] = useState<ConfigureLastfmResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Load configuration from localStorage on mount
  useEffect(() => {
    const stored = localStorage.getItem(LASTFM_CONFIG_KEY)
    if (stored) {
      try {
        setLastfmConfig(JSON.parse(stored))
      } catch (e) {
        localStorage.removeItem(LASTFM_CONFIG_KEY)
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

  function clearError() {
    setError(null)
  }

  const value: ConfigContextValue = {
    lastfmConfig,
    isLoading,
    error,
    configureLastfm,
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
