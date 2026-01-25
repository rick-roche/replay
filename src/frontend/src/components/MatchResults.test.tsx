import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'

// Mock useMatch to provide deterministic matched data
vi.mock('@/contexts/MatchContext', () => {
  const matchedData = {
    tracks: [
      {
        sourceTrack: {
          name: 'Track A',
          artist: 'Artist X',
          album: 'Album Z',
          source: 'lastfm',
          sourceMetadata: { playCount: 42 },
        },
        match: {
          spotifyId: 'sp123',
          name: 'Track A',
          artist: 'Artist X',
          album: 'Album Z',
          uri: 'spotify:track:sp123',
          confidence: 95,
          method: 'Exact',
        },
      },
      {
        sourceTrack: {
          name: 'Unmatched Song',
          artist: 'Artist Y',
          album: null,
          source: 'lastfm',
          sourceMetadata: { playCount: 10 },
        },
        match: null,
      },
    ],
    totalTracks: 2,
    matchedCount: 1,
    unmatchedCount: 1,
  }
  return {
    useMatch: () => ({
      matchedData,
      isLoading: false,
      error: null,
      matchTracks: vi.fn(),
      clearMatches: vi.fn(),
      clearError: vi.fn(),
      retryMatch: vi.fn(),
      removeTrack: vi.fn(),
      moveTrack: vi.fn(),
      applyManualMatch: vi.fn(),
      searchTracks: vi.fn().mockResolvedValue([]),
    }),
  }
})

import { MatchResults } from './MatchResults'

function TestWrapper({ children }: { children: React.ReactNode }) {
  return <Theme>{children}</Theme>
}

describe('MatchResults', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders matched results with confidence and source metadata', () => {
    render(
      <TestWrapper>
        <MatchResults />
      </TestWrapper>
    )

    // Header
    expect(screen.getByText('Matching Complete')).toBeInTheDocument()

    // Matched line
    expect(screen.getByText(/Matched: Track A by Artist X/)).toBeInTheDocument()

    // Confidence badge
    expect(screen.getByText(/95%/)).toBeInTheDocument()

    // Source badge and metadata
    expect(screen.getAllByText('lastfm').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText(/Album: Album Z · Play count: 42/)).toBeInTheDocument()
  })

  it('renders unmatched track rows', () => {
    render(
      <TestWrapper>
        <MatchResults />
      </TestWrapper>
    )

    expect(screen.getByText('Unmatched Song')).toBeInTheDocument()
    expect(screen.getByText(/by Artist Y/)).toBeInTheDocument()
  })
})
