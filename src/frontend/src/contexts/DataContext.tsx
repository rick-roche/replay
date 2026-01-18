import { createContext, useContext, useState, type ReactNode } from 'react'
import type { LastfmDataResponse, LastfmFilter } from '../types/lastfm'
import { configApi } from '../api/config'

interface DataContextValue {
  data: LastfmDataResponse | null
  isLoading: boolean
  error: string | null
  fetchData: (username: string, filter: LastfmFilter) => Promise<void>
  clearData: () => void
  clearError: () => void
}

const DataContext = createContext<DataContextValue | null>(null)

export function DataProvider({ children }: { children: ReactNode }) {
  const [data, setData] = useState<LastfmDataResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function fetchData(username: string, filter: LastfmFilter) {
    setIsLoading(true)
    setError(null)
    setData(null)

    try {
      const result = await configApi.fetchLastfmData(username, filter)
      setData(result)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch data')
    } finally {
      setIsLoading(false)
    }
  }

  function clearData() {
    setData(null)
  }

  function clearError() {
    setError(null)
  }

  const value: DataContextValue = {
    data,
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
