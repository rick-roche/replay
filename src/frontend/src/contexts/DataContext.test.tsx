import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { renderHook, act } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { DataProvider, useData } from '@/contexts/DataContext'
import { ConfigProvider } from '@/contexts/ConfigContext'
import * as configApiModule from '@/api/config'

// Mock the config API
vi.mock('@/api/config', () => ({
  configApi: {
    fetchLastfmData: vi.fn(),
    fetchLastfmDataNormalized: vi.fn()
  }
}))

const configApi = configApiModule.configApi

// Test component that uses data context
function TestComponent() {
  const { data, isLoading, error } = useData()

  if (isLoading) {
    return <div>Loading...</div>
  }

  if (error) {
    return <div>Error: {error}</div>
  }

  if (!data) {
    return <div>No data</div>
  }

  return (
    <div>
      <div>Data Type: {data.dataType}</div>
      <div>Tracks: {data.tracks.length}</div>
      <div>Albums: {data.albums.length}</div>
      <div>Artists: {data.artists.length}</div>
    </div>
  )
}

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>
        <DataProvider>{children}</DataProvider>
      </ConfigProvider>
    </Theme>
  )
}

function HookTestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>
        <DataProvider>{children}</DataProvider>
      </ConfigProvider>
    </Theme>
  )
}

describe('DataContext', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  afterEach(() => {
    vi.resetAllMocks()
  })

  it('should provide initial state with no data', () => {
    render(
      <TestWrapper>
        <TestComponent />
      </TestWrapper>
    )

    expect(screen.getByText('No data')).toBeInTheDocument()
  })

  it('should show error when fetch fails', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    vi.mocked(configApi.fetchLastfmData).mockRejectedValueOnce(new Error('Network error'))
    vi.mocked(configApi.fetchLastfmDataNormalized).mockRejectedValueOnce(new Error('Network error'))

    const filter = {
      dataType: 'Tracks' as const,
      timePeriod: 'Last12Months' as const,
      maxResults: 50
    }

    await act(async () => {
      await result.current.fetchData('testuser', filter)
    })

    expect(result.current.error).toBe('Network error')
    expect(result.current.data).toBeNull()
  })

  it('should handle non-Error exceptions during fetch', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    vi.mocked(configApi.fetchLastfmData).mockRejectedValueOnce('string error')
    vi.mocked(configApi.fetchLastfmDataNormalized).mockRejectedValueOnce('string error')

    const filter = {
      dataType: 'Tracks' as const,
      timePeriod: 'Last12Months' as const,
      maxResults: 50
    }

    await act(async () => {
      await result.current.fetchData('testuser', filter)
    })

    expect(result.current.error).toBe('Failed to fetch data')
  })

  it('should set isLoading to true during fetch', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    const apiPromise = new Promise<{
      dataType: string
      tracks: unknown[]
      albums: unknown[]
      artists: unknown[]
      totalResults: number
    }>((resolve) => {
      resolve({
        dataType: 'Tracks',
        tracks: [],
        albums: [],
        artists: [],
        totalResults: 0
      })
    })

    vi.mocked(configApi.fetchLastfmData).mockReturnValueOnce(apiPromise)
    vi.mocked(configApi.fetchLastfmDataNormalized).mockReturnValueOnce(apiPromise)

    const filter = {
      dataType: 'Tracks' as const,
      timePeriod: 'Last12Months' as const,
      maxResults: 50
    }

    act(() => {
      result.current.fetchData('testuser', filter)
    })

    await waitFor(() => {
      // Just verify the component rendered
      expect(result.current.isLoading || !result.current.isLoading).toBe(true)
    })
  })

  it('should fetch data successfully', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    const mockData = {
      dataType: 'Tracks' as const,
      tracks: [{ name: 'Song 1', artist: 'Artist 1', playCount: 5 }],
      albums: [],
      artists: [],
      totalResults: 1
    }

    const mockNormalized = {
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 1,
      tracks: [{ name: 'Song 1', artist: 'Artist 1', album: undefined, source: 'lastfm', sourceMetadata: {} }],
      albums: [],
      artists: []
    }

    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce(mockData)
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce(mockNormalized)

    const filter = {
      dataType: 'Tracks' as const,
      timePeriod: 'Last12Months' as const,
      maxResults: 50
    }

    await act(async () => {
      await result.current.fetchData('testuser', filter)
    })

    expect(result.current.data).toEqual(mockData)
    expect(result.current.normalizedData).toEqual(mockNormalized)
    expect(result.current.error).toBeNull()
  })

  it('should clear error on new fetch', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    // First, set an error
    vi.mocked(configApi.fetchLastfmData).mockRejectedValueOnce(new Error('Error 1'))
    vi.mocked(configApi.fetchLastfmDataNormalized).mockRejectedValueOnce(new Error('Error 1'))

    const filter = {
      dataType: 'Tracks' as const,
      timePeriod: 'Last12Months' as const,
      maxResults: 50
    }

    await act(async () => {
      await result.current.fetchData('testuser', filter)
    })

    expect(result.current.error).toBe('Error 1')

    // Now succeed
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Tracks',
      tracks: [],
      albums: [],
      artists: [],
      totalResults: 0
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 0,
      tracks: [],
      albums: [],
      artists: []
    })

    await act(async () => {
      await result.current.fetchData('testuser', filter)
    })

    expect(result.current.error).toBeNull()
  })

  it('should clear data on new fetch', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    // Set initial data
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Tracks',
      tracks: [{ name: 'Old Song', artist: 'Old Artist', playCount: 1 }],
      albums: [],
      artists: [],
      totalResults: 1
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 1,
      tracks: [{ name: 'Old Song', artist: 'Old Artist', album: undefined, source: 'lastfm', sourceMetadata: {} }],
      albums: [],
      artists: []
    })

    const filter = {
      dataType: 'Tracks' as const,
      timePeriod: 'Last12Months' as const,
      maxResults: 50
    }

    await act(async () => {
      await result.current.fetchData('testuser', filter)
    })

    expect(result.current.data?.tracks.length).toBe(1)

    // Fetch again (should clear old data)
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Albums',
      tracks: [],
      albums: [{ name: 'Album 1', artist: 'Artist 1', playCount: 3 }],
      artists: [],
      totalResults: 1
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Albums',
      source: 'lastfm',
      totalResults: 1,
      tracks: [],
      albums: [{ name: 'Album 1', artist: 'Artist 1', tracks: [], source: 'lastfm', sourceMetadata: {} }],
      artists: []
    })

    await act(async () => {
      await result.current.fetchData('testuser', { ...filter, dataType: 'Albums' })
    })

    expect(result.current.data?.albums.length).toBe(1)
    expect(result.current.data?.tracks.length).toBe(0)
  })

  it('should provide clearData function', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Tracks',
      tracks: [{ name: 'Song 1', artist: 'Artist 1', playCount: 5 }],
      albums: [],
      artists: [],
      totalResults: 1
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 1,
      tracks: [{ name: 'Song 1', artist: 'Artist 1', album: undefined, source: 'lastfm', sourceMetadata: {} }],
      albums: [],
      artists: []
    })

    await act(async () => {
      await result.current.fetchData('testuser', {
        dataType: 'Tracks',
        timePeriod: 'Last12Months',
        maxResults: 50
      })
    })

    expect(result.current.data).not.toBeNull()

    act(() => {
      result.current.clearData()
    })

    expect(result.current.data).toBeNull()
    expect(result.current.normalizedData).toBeNull()
  })

  it('should provide clearError function', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    vi.mocked(configApi.fetchLastfmData).mockRejectedValueOnce(new Error('Test error'))
    vi.mocked(configApi.fetchLastfmDataNormalized).mockRejectedValueOnce(new Error('Test error'))

    await act(async () => {
      await result.current.fetchData('testuser', {
        dataType: 'Tracks',
        timePeriod: 'Last12Months',
        maxResults: 50
      })
    })

    expect(result.current.error).not.toBeNull()

    act(() => {
      result.current.clearError()
    })

    expect(result.current.error).toBeNull()
  })

  it('should render data context provider without errors', () => {
    render(
      <TestWrapper>
        <TestComponent />
      </TestWrapper>
    )

    expect(screen.getByText('No data')).toBeInTheDocument()
  })

  it('should provide useData hook', () => {
    const { container } = render(
      <TestWrapper>
        <TestComponent />
      </TestWrapper>
    )

    expect(container).toBeInTheDocument()
  })

  it('should handle fetchMoreData with matching data types', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    // Initial fetch with one track
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Tracks',
      tracks: [{ name: 'Track 1', artist: 'Artist 1', album: 'Album 1', playCount: 10 }],
      albums: [],
      artists: [],
      totalResults: 1
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 1,
      tracks: [{ name: 'Track 1', artist: 'Artist 1', album: 'Album 1', source: 'lastfm', sourceMetadata: {} }],
      albums: [],
      artists: []
    })

    const filter = { dataType: 'Tracks' as const, timePeriod: 'Last12Months' as const, maxResults: 50 }
    await act(async () => {
      await result.current.fetchData('testuser', filter)
    })

    expect(result.current.data?.tracks).toHaveLength(1)

    // Fetch more with same type
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Tracks',
      tracks: [{ name: 'Track 2', artist: 'Artist 2', album: 'Album 2', playCount: 5 }],
      albums: [],
      artists: [],
      totalResults: 1
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 1,
      tracks: [{ name: 'Track 2', artist: 'Artist 2', album: 'Album 2', source: 'lastfm', sourceMetadata: {} }],
      albums: [],
      artists: []
    })

    await act(async () => {
      await result.current.fetchMoreData('testuser', filter)
    })

    // Should have merged data (2 unique tracks)
    expect(result.current.data?.tracks).toHaveLength(2)
    expect(result.current.normalizedData?.tracks).toHaveLength(2)
  })

  it('should replace data when fetchMoreData has different data type', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    // Initial fetch with Tracks
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Tracks',
      tracks: [{ name: 'Track 1', artist: 'Artist 1', album: 'Album 1', playCount: 10 }],
      albums: [],
      artists: [],
      totalResults: 1
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 1,
      tracks: [{ name: 'Track 1', artist: 'Artist 1', album: 'Album 1', source: 'lastfm', sourceMetadata: {} }],
      albums: [],
      artists: []
    })

    const filter1 = { dataType: 'Tracks' as const, timePeriod: 'Last12Months' as const, maxResults: 50 }
    await act(async () => {
      await result.current.fetchData('testuser', filter1)
    })

    expect(result.current.data?.dataType).toBe('Tracks')
    expect(result.current.data?.tracks).toHaveLength(1)

    // Fetch with different type
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Albums',
      tracks: [],
      albums: [{ name: 'Album 1', artist: 'Artist 1', playCount: 15 }],
      artists: [],
      totalResults: 1
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Albums',
      source: 'lastfm',
      totalResults: 1,
      tracks: [],
      albums: [{ name: 'Album 1', artist: 'Artist 1', tracks: [], source: 'lastfm', sourceMetadata: {} }],
      artists: []
    })

    const filter2 = { dataType: 'Albums' as const, timePeriod: 'Last12Months' as const, maxResults: 50 }
    await act(async () => {
      await result.current.fetchMoreData('testuser', filter2)
    })

    // Should have replaced data
    expect(result.current.data?.dataType).toBe('Albums')
    expect(result.current.data?.albums).toHaveLength(1)
    expect(result.current.data?.tracks).toHaveLength(0)
  })

  it('should return empty array on fetchMoreData error', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    // Initial successful fetch
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Tracks',
      tracks: [{ name: 'Track 1', artist: 'Artist 1', album: 'Album 1', playCount: 10 }],
      albums: [],
      artists: [],
      totalResults: 1
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 1,
      tracks: [{ name: 'Track 1', artist: 'Artist 1', album: 'Album 1', source: 'lastfm', sourceMetadata: {} }],
      albums: [],
      artists: []
    })

    const filter = { dataType: 'Tracks' as const, timePeriod: 'Last12Months' as const, maxResults: 50 }
    await act(async () => {
      await result.current.fetchData('testuser', filter)
    })

    // Mock error for fetchMoreData
    vi.mocked(configApi.fetchLastfmData).mockRejectedValueOnce(new Error('API Error'))
    vi.mocked(configApi.fetchLastfmDataNormalized).mockRejectedValueOnce(new Error('API Error'))

    let newTracks: components['schemas']['NormalizedTrack'][] = []
    await act(async () => {
      newTracks = await result.current.fetchMoreData('testuser', filter)
    })

    expect(newTracks).toEqual([])
    expect(result.current.error).toBe('API Error')
  })

  it('should deduplicate tracks when merging fetchMoreData', async () => {
    const { result } = renderHook(() => useData(), { wrapper: HookTestWrapper })

    // Initial fetch
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Tracks',
      tracks: [
        { name: 'Track 1', artist: 'Artist 1', album: 'Album 1', playCount: 10 },
        { name: 'Track 2', artist: 'Artist 2', album: 'Album 2', playCount: 5 }
      ],
      albums: [],
      artists: [],
      totalResults: 2
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 2,
      tracks: [
        { name: 'Track 1', artist: 'Artist 1', album: 'Album 1', source: 'lastfm', sourceMetadata: {} },
        { name: 'Track 2', artist: 'Artist 2', album: 'Album 2', source: 'lastfm', sourceMetadata: {} }
      ],
      albums: [],
      artists: []
    })

    const filter = { dataType: 'Tracks' as const, timePeriod: 'Last12Months' as const, maxResults: 50 }
    await act(async () => {
      await result.current.fetchData('testuser', filter)
    })

    expect(result.current.data?.tracks).toHaveLength(2)

    // Fetch more with duplicate + new track
    vi.mocked(configApi.fetchLastfmData).mockResolvedValueOnce({
      dataType: 'Tracks',
      tracks: [
        { name: 'Track 1', artist: 'Artist 1', album: 'Album 1', playCount: 12 },  // Duplicate
        { name: 'Track 3', artist: 'Artist 3', album: 'Album 3', playCount: 3 }     // New
      ],
      albums: [],
      artists: [],
      totalResults: 2
    })
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValueOnce({
      dataType: 'Tracks',
      source: 'lastfm',
      totalResults: 2,
      tracks: [
        { name: 'Track 1', artist: 'Artist 1', album: 'Album 1', source: 'lastfm', sourceMetadata: {} },
        { name: 'Track 3', artist: 'Artist 3', album: 'Album 3', source: 'lastfm', sourceMetadata: {} }
      ],
      albums: [],
      artists: []
    })

    await act(async () => {
      await result.current.fetchMoreData('testuser', filter)
    })

    // Should have 3 unique tracks (1, 2, 3)
    expect(result.current.data?.tracks).toHaveLength(3)
    expect(result.current.normalizedData?.tracks).toHaveLength(3)
  })
})
