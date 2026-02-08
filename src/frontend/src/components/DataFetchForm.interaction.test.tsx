import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { FetchDataButton, DataResults } from './DataFetchForm'
import { ConfigProvider } from '@/contexts/ConfigContext'
import { DataProvider } from '@/contexts/DataContext'
import { DataSourceProvider } from '@/contexts/DataSourceContext'
import * as configApiModule from '@/api/config'
import type { components } from '@/api/generated-client'

const appendMatchesMock = vi.fn()
type MatchedDataResponse = components['schemas']['MatchedDataResponse']

const matchState: { matchedData: MatchedDataResponse | null; isLoading: boolean } = {
  matchedData: null,
  isLoading: false,
}

vi.mock('@/contexts/MatchContext', () => ({
  useMatch: () => ({
    appendMatches: appendMatchesMock,
    matchTracks: vi.fn(),
    clearMatches: vi.fn(),
    clearError: vi.fn(),
    retryMatch: vi.fn(),
    removeTrack: vi.fn(),
    moveTrack: vi.fn(),
    applyManualMatch: vi.fn(),
    searchTracks: vi.fn(),
    matchedData: matchState.matchedData,
    isLoading: matchState.isLoading,
    error: null,
  }),
}))

vi.mock('@/api/config', () => ({
  configApi: {
    fetchLastfmData: vi.fn(),
    // Provide a default resolved value for normalized fetch to avoid errors in tests
    fetchLastfmDataNormalized: vi.fn().mockResolvedValue({ dataType: 'Tracks', tracks: [], albums: [], artists: [] })
  }
}))

const configApi = configApiModule.configApi

function Wrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>
        <DataSourceProvider>
          <DataProvider>{children}</DataProvider>
        </DataSourceProvider>
      </ConfigProvider>
    </Theme>
  )
}

const setConfigured = (username = 'alice') => {
  localStorage.setItem('replay:lastfm_config', JSON.stringify({ username, playCount: 100, isConfigured: true }))
  localStorage.setItem('replay:selected_source', 'lastfm')
}

const setFilter = (dataType: 'Tracks' | 'Albums' | 'Artists' = 'Tracks') => {
  localStorage.setItem('replay:lastfm_filter', JSON.stringify({ dataType, timePeriod: 'Last12Months', maxResults: 50 }))
}

describe('DataFetchForm interactions', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    appendMatchesMock.mockReset()
    localStorage.clear()
    matchState.matchedData = null
    matchState.isLoading = false
  })

  it('shows loading state and disables button during fetch', async () => {
    setConfigured()
    setFilter('Tracks')

    vi.mocked(configApi.fetchLastfmDataNormalized).mockImplementation(() => new Promise(() => {}))

    render(
      <Wrapper>
        <FetchDataButton />
      </Wrapper>
    )

    fireEvent.click(screen.getByRole('button', { name: /Fetch Data/i }))
    expect(screen.getByText(/Fetching.../)).toBeInTheDocument()
    // During loading, the accessible name is "Fetching..."
    expect(screen.getByRole('button', { name: /Fetching.../i })).toBeDisabled()
  })

  it('renders tracks results with truncation message', async () => {
    setConfigured()
    setFilter('Tracks')

    const tracks = Array.from({ length: 15 }, (_, i) => ({ name: `t${i}`, artist: `a${i}`, playCount: i + 1, source: 'lastfm', sourceMetadata: { playCount: i + 1 } }))
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValue({ dataType: 'Tracks', tracks, albums: [], artists: [] })

    render(
      <Wrapper>
        <FetchDataButton />
        <DataResults />
      </Wrapper>
    )

    fireEvent.click(screen.getByRole('button', { name: /Fetch Data/i }))

    await waitFor(() => {
      expect(screen.getByText(/Tracks Found/)).toBeInTheDocument()
    })

    expect(screen.getByText(/15 tracks found/)).toBeInTheDocument()
    expect(screen.getByText(/\.+ and 5 more/)).toBeInTheDocument()
  })

  it('renders albums results', async () => {
    setConfigured()
    setFilter('Albums')

    const albums = [{ name: 'al', artist: 'ar', playCount: 4, source: 'lastfm', sourceMetadata: { playCount: 4 } }]
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValue({ dataType: 'Albums', tracks: [], albums, artists: [] })

    render(
      <Wrapper>
        <FetchDataButton />
        <DataResults />
      </Wrapper>
    )

    fireEvent.click(screen.getByRole('button', { name: /Fetch Data/i }))

    await waitFor(() => {
      expect(screen.getByText(/Albums Found/)).toBeInTheDocument()
    })

    expect(screen.getByText(/1 albums found/)).toBeInTheDocument()
    expect(screen.getByText(/4 plays/)).toBeInTheDocument()
  })

  it('renders artists results', async () => {
    setConfigured()
    setFilter('Artists')

    const artists = [{ name: 'artist', playCount: 7, source: 'lastfm', sourceMetadata: { playCount: 7 } }]
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValue({ dataType: 'Artists', tracks: [], albums: [], artists })

    render(
      <Wrapper>
        <FetchDataButton />
        <DataResults />
      </Wrapper>
    )

    fireEvent.click(screen.getByRole('button', { name: /Fetch Data/i }))

    await waitFor(() => {
      expect(screen.getByText(/Artists Found/)).toBeInTheDocument()
    })

    expect(screen.getByText(/1 artists found/)).toBeInTheDocument()
    expect(screen.getByText(/7 plays/)).toBeInTheDocument()
  })

  it('shows no results message for empty arrays', async () => {
    setConfigured()
    setFilter('Tracks')

    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValue({ dataType: 'Tracks', tracks: [], albums: [], artists: [] })

    render(
      <Wrapper>
        <FetchDataButton />
        <DataResults />
      </Wrapper>
    )

    fireEvent.click(screen.getByRole('button', { name: /Fetch Data/i }))

    await waitFor(() => {
      expect(screen.getByText(/No tracks found/)).toBeInTheDocument()
    })
  })

  it('shows error message when fetch fails', async () => {
    setConfigured()
    setFilter('Tracks')

    vi.mocked(configApi.fetchLastfmDataNormalized).mockRejectedValue(new Error('Boom'))

    render(
      <Wrapper>
        <FetchDataButton />
      </Wrapper>
    )

    fireEvent.click(screen.getByRole('button', { name: /Fetch Data/i }))

    await waitFor(() => {
      expect(screen.getByText('Boom')).toBeInTheDocument()
    })
  })

  it('fetches more data, dedupes, and appends matches', async () => {
    setConfigured()
    setFilter('Tracks')

    const firstTracks = [
      { name: 't1', artist: 'a1', playCount: 1, source: 'lastfm', sourceMetadata: { playCount: 1 } },
      { name: 't2', artist: 'a2', playCount: 2, source: 'lastfm', sourceMetadata: { playCount: 2 } },
    ]
    const moreTracks = [
      { name: 't2', artist: 'a2', playCount: 2, source: 'lastfm', sourceMetadata: { playCount: 2 } }, // duplicate
      { name: 't3', artist: 'a3', playCount: 3, source: 'lastfm', sourceMetadata: { playCount: 3 } },
    ]

    vi.mocked(configApi.fetchLastfmDataNormalized)
      .mockResolvedValueOnce({ dataType: 'Tracks', tracks: firstTracks, albums: [], artists: [] })
      .mockResolvedValueOnce({ dataType: 'Tracks', tracks: moreTracks, albums: [], artists: [] })

    render(
      <Wrapper>
        <FetchDataButton />
        <DataResults />
      </Wrapper>
    )

    // Initial fetch
    fireEvent.click(screen.getByRole('button', { name: /Fetch Data/i }))
    await waitFor(() => {
      expect(screen.getByText(/Tracks Found/)).toBeInTheDocument()
    })

    // Fetch more
    fireEvent.click(screen.getByRole('button', { name: /Fetch More/i }))

    await waitFor(() => {
      expect(screen.getByText(/3 tracks found/)).toBeInTheDocument()
    })

    // Only the new track should be appended to matching
    expect(appendMatchesMock).toHaveBeenCalledTimes(1)
    const calledWith = appendMatchesMock.mock.calls[0][0]
    expect(calledWith).toHaveLength(1)
    expect(calledWith[0].name).toBe('t3')
  })

  it('collapses results when matching is complete', async () => {
    setConfigured()
    setFilter('Tracks')

    matchState.matchedData = {
      tracks: [
        {
          sourceTrack: {
            name: 't0',
            artist: 'a0',
            source: 'lastfm',
            sourceMetadata: {},
          },
          match: null,
        },
      ],
      totalTracks: 1,
      matchedCount: 0,
      unmatchedCount: 1,
    }

    const tracks = Array.from({ length: 3 }, (_, i) => ({ name: `t${i}`, artist: `a${i}`, playCount: i + 1, source: 'lastfm', sourceMetadata: { playCount: i + 1 } }))
    vi.mocked(configApi.fetchLastfmDataNormalized).mockResolvedValue({ dataType: 'Tracks', tracks, albums: [], artists: [] })

    render(
      <Wrapper>
        <FetchDataButton />
        <DataResults />
      </Wrapper>
    )

    fireEvent.click(screen.getByRole('button', { name: /Fetch Data/i }))

    await waitFor(() => {
      expect(screen.getByText(/Tracks Found/)).toBeInTheDocument()
    })

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /Show/i })).toBeInTheDocument()
    })
    expect(screen.queryByText('t0')).not.toBeInTheDocument()
  })
})
