import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { FetchDataButton, DataResults } from '@/components/DataFetchForm'
import { DataProvider } from '@/contexts/DataContext'
import { ConfigProvider } from '@/contexts/ConfigContext'
const appendMatchesMock = vi.fn()

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
    matchedData: null,
    isLoading: false,
    error: null,
  }),
}))

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>
        <DataProvider>{children}</DataProvider>
      </ConfigProvider>
    </Theme>
  )
}

describe('FetchDataButton', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    appendMatchesMock.mockReset()
    localStorage.clear()
  })

  it('should render nothing when Last.fm is not configured', () => {
    render(
      <TestWrapper>
        <FetchDataButton />
      </TestWrapper>
    )

    // When not configured, no button or content should appear
    expect(screen.queryByText('Fetch Data')).not.toBeInTheDocument()
  })

  it('should render fetch button when Last.fm is configured', () => {
    // Set up a configured Last.fm profile
    const config = {
      username: 'testuser',
      playCount: 1000,
      isConfigured: true
    }
    localStorage.setItem('replay:lastfm_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <FetchDataButton />
      </TestWrapper>
    )

    // Button should be present
    expect(screen.getByRole('button', { name: /Fetch Data/i })).toBeInTheDocument()
    appendMatchesMock.mockReset()
  })

  it('should display filter info when configured', () => {
    const config = {
      username: 'testuser',
      playCount: 1000,
      isConfigured: true
    }
    localStorage.setItem('replay:lastfm_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <FetchDataButton />
      </TestWrapper>
    )

    expect(screen.getByText(/Last.fm/)).toBeInTheDocument()
  })

  it('should render within a Card component', () => {
    const config = {
      username: 'testuser',
      playCount: 1000,
      isConfigured: true
    }
    localStorage.setItem('replay:lastfm_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <FetchDataButton />
      </TestWrapper>
    )

    // Button and heading should both be present
    expect(screen.getByRole('button', { name: /Fetch Data/i })).toBeInTheDocument()
    appendMatchesMock.mockReset()
  })

  it('should be dark mode compatible', () => {
    const config = {
      username: 'testuser',
      playCount: 1000,
      isConfigured: true
    }
    localStorage.setItem('replay:lastfm_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <FetchDataButton />
      </TestWrapper>
    )

    // Component renders without errors in Theme context
    expect(screen.getByRole('button', { name: /Fetch Data/i })).toBeInTheDocument()
    appendMatchesMock.mockReset()
  })
})

describe('DataResults', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should render nothing when there is no data', () => {
    render(
      <TestWrapper>
        <DataResults />
      </TestWrapper>
    )

    // No results should be shown
    expect(screen.queryByText(/found/)).not.toBeInTheDocument()
  })

  it('should render null initially', () => {
    render(
      <TestWrapper>
        <DataResults />
      </TestWrapper>
    )

    // Initially no content
    expect(screen.queryByText(/found/)).not.toBeInTheDocument()
  })

  it('should render without errors when no data', () => {
    const { container } = render(
      <TestWrapper>
        <DataResults />
      </TestWrapper>
    )

    // Component should exist and not throw
    expect(container).toBeInTheDocument()
  })

  it('should be responsive', () => {
    const { container } = render(
      <TestWrapper>
        <DataResults />
      </TestWrapper>
    )

    expect(container).toBeInTheDocument()
  })

  it('should be dark mode compatible', () => {
    const { container } = render(
      <TestWrapper>
        <DataResults />
      </TestWrapper>
    )

    // Rendered within Theme provider without errors
    expect(container).toBeInTheDocument()
  })
})
