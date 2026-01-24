import { createContext, useContext, useState, type ReactNode } from 'react'
import type { components } from '../api/generated-client'
import { configApi } from '../api/config'

type LastfmDataResponse = components['schemas']['LastfmDataResponse']
type LastfmFilter = components['schemas']['LastfmFilter']
type NormalizedDataResponse = components['schemas']['NormalizedDataResponse']

interface DataContextValue {
  data: LastfmDataResponse | null
  normalizedData: NormalizedDataResponse | null
  isLoading: boolean
  error: string | null
  fetchData: (username: string, filter: LastfmFilter) => Promise<void>
  clearData: () => void
  clearError: () => void
}

const DataContext = createContext<DataContextValue | null>(null)

export function DataProvider({ children }: { children: ReactNode }) {
  const [data, setData] = useState<LastfmDataResponse | null>(null)
  const [normalizedData, setNormalizedData] = useState<NormalizedDataResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function fetchData(username: string, filter: LastfmFilter) {
    setIsLoading(true)
    setError(null)
    setData(null)
    setNormalizedData(null)

    try {
      // Fetch both raw and normalized data
      const [rawData, normalized] = await Promise.all([
        configApi.fetchLastfmData(username, filter),
        configApi.fetchLastfmDataNormalized(username, filter)
      ])
      setData(rawData)
      setNormalizedData(normalized)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch data')
    } finally {
      setIsLoading(false)
    }
  }

  function clearData() {
    setData(null)
    setNormalizedData(null)
  }

  function clearError() {
    setError(null)
  }

  const value: DataContextValue = {
    data,
    normalizedData,
    isLoading,
    error,
    fetchData,
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
