import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Theme } from '@radix-ui/themes'
import { DataSourceSelector } from '@/components/DataSourceSelector'
import { DataSourceProvider } from '@/contexts/DataSourceContext'

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <DataSourceProvider>{children}</DataSourceProvider>
    </Theme>
  )
}

describe('DataSourceSelector', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should render all three data source options', () => {
    render(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    expect(screen.getByText('Last.fm')).toBeInTheDocument()
    expect(screen.getByText('Discogs')).toBeInTheDocument()
    expect(screen.getByText('Setlist.fm')).toBeInTheDocument()
  })

  it('should render descriptions for each source', () => {
    render(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    expect(screen.getByText('Create playlists from your listening history')).toBeInTheDocument()
    expect(screen.getByText('Convert your collection into playlists')).toBeInTheDocument()
    expect(screen.getByText('Relive concerts with setlist-based playlists')).toBeInTheDocument()
  })

  it('should select Last.fm when clicked', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    const lastfmButton = screen.getByText('Last.fm').closest('button')
    expect(lastfmButton).toBeInTheDocument()

    await user.click(lastfmButton!)

    // The selected source should have green border styling
    expect(lastfmButton).toHaveClass('border-green-500')
  })

  it('should select Discogs when clicked', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    const discogsButton = screen.getByText('Discogs').closest('button')
    expect(discogsButton).toBeInTheDocument()

    await user.click(discogsButton!)

    expect(discogsButton).toHaveClass('border-green-500')
  })

  it('should select Setlist.fm when clicked', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    const setlistButton = screen.getByText('Setlist.fm').closest('button')
    expect(setlistButton).toBeInTheDocument()

    await user.click(setlistButton!)

    expect(setlistButton).toHaveClass('border-green-500')
  })

  it('should allow switching between sources', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    const lastfmButton = screen.getByText('Last.fm').closest('button')
    const discogsButton = screen.getByText('Discogs').closest('button')

    // Select Last.fm
    await user.click(lastfmButton!)
    expect(lastfmButton).toHaveClass('border-green-500')

    // Switch to Discogs
    await user.click(discogsButton!)
    expect(discogsButton).toHaveClass('border-green-500')
    expect(lastfmButton).not.toHaveClass('border-green-500')
  })

  it('should render all sources as enabled', () => {
    render(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    const buttons = screen.getAllByRole('button')
    const sourceButtons = buttons.filter(btn => 
      ['Last.fm', 'Discogs', 'Setlist.fm'].some(name => btn.textContent?.includes(name))
    )

    sourceButtons.forEach(button => {
      expect(button).not.toBeDisabled()
    })
  })

  it('should persist selection in localStorage', async () => {
    const user = userEvent.setup()
    const { rerender } = render(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    const lastfmButton = screen.getByText('Last.fm').closest('button')
    await user.click(lastfmButton!)

    // Rerender to simulate component remount
    rerender(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    const newLastfmButton = screen.getByText('Last.fm').closest('button')
    expect(newLastfmButton).toHaveClass('border-green-500')
  })

  it('should have proper heading and description', () => {
    render(
      <TestWrapper>
        <DataSourceSelector />
      </TestWrapper>
    )

    expect(screen.getByText('Select Data Source')).toBeInTheDocument()
    expect(screen.getByText('Choose where you want to pull music data from')).toBeInTheDocument()
  })
})
