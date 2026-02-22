import { createContext, useContext, useState, type ReactNode } from 'react'
import { DataSource } from '../types/datasource'

interface DataSourceContextValue {
  selectedSource: DataSource | null
  selectSource: (source: DataSource) => void
  clearSource: () => void
}

const DataSourceContext = createContext<DataSourceContextValue | null>(null)

const DATA_SOURCE_KEY = 'replay:selected_source'

function initializeSource(): DataSource | null {
  const stored = localStorage.getItem(DATA_SOURCE_KEY)
  if (stored && Object.values(DataSource).includes(stored as DataSource)) {
    return stored as DataSource
  }
  return null
}

export function DataSourceProvider({ children }: { children: ReactNode }) {
  const [selectedSource, setSelectedSource] = useState<DataSource | null>(initializeSource)

  function selectSource(source: DataSource) {
    setSelectedSource(source)
    localStorage.setItem(DATA_SOURCE_KEY, source)
  }

  function clearSource() {
    setSelectedSource(null)
    localStorage.removeItem(DATA_SOURCE_KEY)
  }

  const value: DataSourceContextValue = {
    selectedSource,
    selectSource,
    clearSource
  }

  return <DataSourceContext.Provider value={value}>{children}</DataSourceContext.Provider>
}

export function useDataSource() {
  const context = useContext(DataSourceContext)
  if (!context) {
    throw new Error('useDataSource must be used within DataSourceProvider')
  }
  return context
}
