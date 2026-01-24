import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { FetchDataButton, DataResults } from './DataFetchForm'
import { ConfigProvider } from '@/contexts/ConfigContext'
import { DataProvider } from '@/contexts/DataContext'
import * as configApiModule from '@/api/config'

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
        <DataProvider>{children}</DataProvider>
      </ConfigProvider>
    </Theme>
  )
}

const setConfigured = (username = 'alice') => {
  localStorage.setItem('replay:lastfm_config', JSON.stringify({ username, playCount: 100, isConfigured: true }))
}

const setFilter = (dataType: 'Tracks' | 'Albums' | 'Artists' = 'Tracks') => {
  localStorage.setItem('replay:lastfm_filter', JSON.stringify({ dataType, timePeriod: 'Last12Months', maxResults: 50 }))
}

describe('DataFetchForm interactions', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('shows loading state and disables button during fetch', async () => {
    setConfigured()
    setFilter('Tracks')

    vi.mocked(configApi.fetchLastfmData).mockImplementation(() => new Promise(() => {}))

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

    const tracks = Array.from({ length: 15 }, (_, i) => ({ name: `t${i}`, artist: `a${i}`, playCount: i + 1 }))
    vi.mocked(configApi.fetchLastfmData).mockResolvedValue({ dataType: 'Tracks', tracks, albums: [], artists: [] })

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

    const albums = [{ name: 'al', artist: 'ar', playCount: 4 }]
    vi.mocked(configApi.fetchLastfmData).mockResolvedValue({ dataType: 'Albums', tracks: [], albums, artists: [] })

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

    const artists = [{ name: 'artist', playCount: 7 }]
    vi.mocked(configApi.fetchLastfmData).mockResolvedValue({ dataType: 'Artists', tracks: [], albums: [], artists })

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

    vi.mocked(configApi.fetchLastfmData).mockResolvedValue({ dataType: 'Tracks', tracks: [], albums: [], artists: [] })

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

    vi.mocked(configApi.fetchLastfmData).mockRejectedValue(new Error('Boom'))

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
})
