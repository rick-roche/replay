import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Theme } from '@radix-ui/themes'
import { SetlistConcertSelector } from '@/components/SetlistConcertSelector'

const fetchSetlistFmConcertsMock = vi.fn()
const setSelectedSetlistConcertIdsMock = vi.fn()
const clearSelectedSetlistConcertIdsMock = vi.fn()

let selectedConcertIds: string[] = []
let setlistConcertsPage: {
  concerts: Array<{ id: string; artist: string; date?: string; venue?: string; city?: string; country?: string }>
  totalConcerts: number
  pageNumber: number
  pageSize: number
  hasNextPage: boolean
  hasPreviousPage: boolean
} | null = null
let isLoadingConcerts = false
let errorMessage: string | null = null

vi.mock('@/contexts/ConfigContext', () => ({
  useConfig: () => ({
    setlistFmFilter: { maxConcerts: 10, maxTracks: 100 },
    getSelectedSetlistConcertIds: () => selectedConcertIds,
    setSelectedSetlistConcertIds: setSelectedSetlistConcertIdsMock,
    clearSelectedSetlistConcertIds: clearSelectedSetlistConcertIdsMock
  })
}))

vi.mock('@/contexts/DataContext', () => ({
  useData: () => ({
    setlistConcertsPage,
    isLoadingConcerts,
    error: errorMessage,
    fetchSetlistFmConcerts: fetchSetlistFmConcertsMock
  })
}))

function renderComponent() {
  return render(
    <Theme>
      <SetlistConcertSelector userId="user123" />
    </Theme>
  )
}

describe('SetlistConcertSelector', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    selectedConcertIds = []
    setlistConcertsPage = null
    isLoadingConcerts = false
    errorMessage = null
  })

  it('fetches concerts on mount', () => {
    renderComponent()

    expect(fetchSetlistFmConcertsMock).toHaveBeenCalledWith('user123', { maxConcerts: 10, maxTracks: 100 }, 1)
  })

  it('shows loading state', () => {
    isLoadingConcerts = true
    renderComponent()

    expect(screen.getByText('Loading concerts...')).toBeInTheDocument()
  })

  it('does not keep the previous page interactive while loading a new page', () => {
    isLoadingConcerts = true
    setlistConcertsPage = {
      concerts: [{ id: 'c1', artist: 'Band A' }],
      totalConcerts: 21,
      pageNumber: 1,
      pageSize: 20,
      hasNextPage: true,
      hasPreviousPage: false
    }

    renderComponent()

    expect(screen.queryByText('Band A')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Select Page' })).toBeDisabled()
  })

  it('renders empty state when no concerts are returned', () => {
    setlistConcertsPage = {
      concerts: [],
      totalConcerts: 0,
      pageNumber: 1,
      pageSize: 20,
      hasNextPage: false,
      hasPreviousPage: false
    }

    renderComponent()

    expect(screen.getByText('No concerts on this page match the current filters.')).toBeInTheDocument()
  })

  it('renders concerts and allows selecting one concert', async () => {
    const user = userEvent.setup()
    setlistConcertsPage = {
      concerts: [{ id: 'c1', artist: 'Band A', date: '01-01-2024', venue: 'Arena', city: 'Dublin', country: 'Ireland' }],
      totalConcerts: 1,
      pageNumber: 1,
      pageSize: 20,
      hasNextPage: false,
      hasPreviousPage: false
    }

    renderComponent()

    expect(screen.getByText('Band A')).toBeInTheDocument()
    await user.click(screen.getByRole('checkbox', { name: /Include concert Band A/i }))
    expect(setSelectedSetlistConcertIdsMock).toHaveBeenCalledWith('user123', ['c1'])
  })

  it('tracks concert selections case-insensitively', async () => {
    const user = userEvent.setup()
    selectedConcertIds = ['C1']
    setlistConcertsPage = {
      concerts: [{ id: 'c1', artist: 'Band A' }],
      totalConcerts: 1,
      pageNumber: 1,
      pageSize: 20,
      hasNextPage: false,
      hasPreviousPage: false
    }

    renderComponent()

    const checkbox = screen.getByRole('checkbox', { name: /Include concert Band A/i })
    expect(checkbox).toBeChecked()
    await user.click(checkbox)
    expect(setSelectedSetlistConcertIdsMock).toHaveBeenCalledWith('user123', [])
  })

  it('supports select page and clear all actions', async () => {
    const user = userEvent.setup()
    selectedConcertIds = ['c-existing']
    setlistConcertsPage = {
      concerts: [
        { id: 'c1', artist: 'Band A' },
        { id: 'c2', artist: 'Band B' }
      ],
      totalConcerts: 2,
      pageNumber: 1,
      pageSize: 20,
      hasNextPage: false,
      hasPreviousPage: false
    }

    renderComponent()

    await user.click(screen.getByRole('button', { name: 'Select Page' }))
    expect(setSelectedSetlistConcertIdsMock).toHaveBeenCalledWith('user123', ['c-existing', 'c1', 'c2'])

    await user.click(screen.getByRole('button', { name: 'Clear All' }))
    expect(clearSelectedSetlistConcertIdsMock).toHaveBeenCalledWith('user123')
  })

  it('supports pagination controls', async () => {
    const user = userEvent.setup()
    setlistConcertsPage = {
      concerts: [{ id: 'c1', artist: 'Band A' }],
      totalConcerts: 21,
      pageNumber: 1,
      pageSize: 20,
      hasNextPage: true,
      hasPreviousPage: false
    }

    renderComponent()

    await user.click(screen.getByRole('button', { name: 'Next' }))
    expect(fetchSetlistFmConcertsMock).toHaveBeenCalledWith('user123', { maxConcerts: 10, maxTracks: 100 }, 2)
  })

  it('keeps a way back after a page request fails', async () => {
    const user = userEvent.setup()
    setlistConcertsPage = {
      concerts: [{ id: 'c1', artist: 'Band A' }],
      totalConcerts: 21,
      pageNumber: 1,
      pageSize: 20,
      hasNextPage: true,
      hasPreviousPage: false
    }

    renderComponent()

    await user.click(screen.getByRole('button', { name: 'Next' }))

    expect(screen.getByRole('button', { name: 'Previous' })).toBeEnabled()
  })
})
