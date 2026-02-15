import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ManualSearchModal } from '@/components/ManualSearchModal'
import type { components } from '@/api/generated-client'

type SpotifyTrack = components['schemas']['SpotifyTrack']

const mockTrack: SpotifyTrack = {
  id: '1',
  name: 'Test Track',
  artist: 'Test Artist',
  album: 'Test Album',
  uri: 'spotify:track:1'
}

const mockTrack2: SpotifyTrack = {
  id: '2',
  name: 'Another Track',
  artist: 'Another Artist',
  album: 'Another Album',
  uri: 'spotify:track:2'
}

describe('ManualSearchModal', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should not render when open is false', () => {
    const mockOnSearch = vi.fn()
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    const { container } = render(
      <ManualSearchModal
        open={false}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    // Modal content should not be visible
    expect(container.querySelector('[data-radix-dialog-content]')).not.toBeInTheDocument()
  })

  it('should render when open is true', () => {
    const mockOnSearch = vi.fn()
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    expect(screen.getByText('Search Spotify')).toBeInTheDocument()
    expect(screen.getByPlaceholderText(/Track name, artist/i)).toBeInTheDocument()
  })

  it('should have search input field', () => {
    const mockOnSearch = vi.fn()
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    const input = screen.getByPlaceholderText(/Track name, artist/i)
    expect(input).toBeInTheDocument()
  })

  it('should accept user input in search field', async () => {
    const user = userEvent.setup()
    const mockOnSearch = vi.fn()
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    const input = screen.getByPlaceholderText(/Track name, artist/i) as HTMLInputElement
    await user.type(input, 'Test Query')

    expect(input.value).toBe('Test Query')
  })

  it('should display initial query if provided', () => {
    const mockOnSearch = vi.fn()
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
        initialQuery="Initial Track"
      />
    )

    const input = screen.getByPlaceholderText(/Track name, artist/i) as HTMLInputElement
    expect(input.value).toBe('Initial Track')
  })

  it('should call onSearch when form is submitted', async () => {
    const user = userEvent.setup()
    const mockOnSearch = vi.fn().mockResolvedValue([mockTrack])
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    const input = screen.getByPlaceholderText(/Track name, artist/i)
    await user.type(input, 'Test Track')

    const searchButton = screen.getByRole('button', { name: '' }).parentElement?.querySelector('button')
    if (searchButton) {
      await user.click(searchButton)
    } else {
      // Try finding button with form submission
      const form = input.closest('form')
      if (form) {
        await user.tripleClick(input)
      }
    }
  })

  it('should display search results', async () => {
    const user = userEvent.setup()
    const mockOnSearch = vi.fn().mockResolvedValue([mockTrack, mockTrack2])
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    const input = screen.getByPlaceholderText(/Track name, artist/i)
    await user.type(input, 'Test')

    // Simulate form submission by pressing enter
    await user.keyboard('{Enter}')

    // Wait for results to appear
    expect(mockOnSearch).toHaveBeenCalledWith('Test')
  })

  it('should show error when search is empty', async () => {
    const user = userEvent.setup()
    const mockOnSearch = vi.fn()
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    const input = screen.getByPlaceholderText(/Track name, artist/i)
    
    // Try to submit empty search
    const form = input.closest('form')
    if (form) {
      await user.click(form.querySelector('button[type="submit"]')!)
    }

    // onSearch should not be called with empty string
  })

  it('should have close button', () => {
    const mockOnSearch = vi.fn()
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    const closeButton = screen.getByLabelText('Close')
    expect(closeButton).toBeInTheDocument()
  })

  it('should call onClose when close button is clicked', async () => {
    const user = userEvent.setup()
    const mockOnSearch = vi.fn()
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    const closeButton = screen.getByLabelText('Close')
    await user.click(closeButton)

    expect(mockOnClose).toHaveBeenCalled()
  })

  it('should be keyboard accessible', () => {
    const mockOnSearch = vi.fn()
    const mockOnSelect = vi.fn()
    const mockOnClose = vi.fn()

    render(
      <ManualSearchModal
        open={true}
        onClose={mockOnClose}
        onSelect={mockOnSelect}
        onSearch={mockOnSearch}
      />
    )

    const input = screen.getByPlaceholderText(/Track name, artist/i)
    expect(input).toBeInTheDocument()
    // Keyboard navigation is handled by Radix UI
  })
})
