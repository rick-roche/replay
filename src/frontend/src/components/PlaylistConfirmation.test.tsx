import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Theme } from '@radix-ui/themes'
import { PlaylistConfirmation } from './PlaylistConfirmation'

// Create mock context values
const mockClearPlaylist = vi.fn()
const mockResetWorkflow = vi.fn()

let mockCreatePlaylistContext = {
  createPlaylist: vi.fn(),
  isCreating: false,
  error: null,
  playlistUrl: 'https://open.spotify.com/playlist/123' as string | null,
  playlistId: '123' as string | null,
  clearPlaylist: mockClearPlaylist
}

let mockWorkflowContext = {
  currentStep: 'create',
  completedSteps: new Set(),
  goToStep: vi.fn(),
  canGoToStep: () => false,
  markStepComplete: vi.fn(),
  nextStep: vi.fn(),
  previousStep: vi.fn(),
  resetWorkflow: mockResetWorkflow,
  lockWorkflow: vi.fn()
}

beforeEach(() => {
  vi.clearAllMocks()
  
  mockCreatePlaylistContext = {
    createPlaylist: vi.fn(),
    isCreating: false,
    error: null,
    playlistUrl: 'https://open.spotify.com/playlist/123' as string | null,
    playlistId: '123' as string | null,
    clearPlaylist: mockClearPlaylist
  }
  
  mockWorkflowContext = {
    currentStep: 'create',
    completedSteps: new Set(),
    goToStep: vi.fn(),
    canGoToStep: () => false,
    markStepComplete: vi.fn(),
    nextStep: vi.fn(),
    previousStep: vi.fn(),
    resetWorkflow: mockResetWorkflow,
    lockWorkflow: vi.fn()
  }

  // Mock window.open
  window.open = vi.fn()
})

// Mock the contexts
vi.mock('@/contexts/CreatePlaylistContext', () => ({
  useCreatePlaylist: () => mockCreatePlaylistContext
}))

vi.mock('@/contexts/WorkflowContext', () => ({
  useWorkflow: () => mockWorkflowContext
}))

function TestWrapper({ children }: { children: React.ReactNode }) {
  return <Theme>{children}</Theme>
}

describe('PlaylistConfirmation', () => {
  it('should not render when playlistUrl is null', () => {
    mockCreatePlaylistContext.playlistUrl = null
    mockCreatePlaylistContext.playlistId = '123'
    
    const { container } = render(
      <TestWrapper>
        <PlaylistConfirmation />
      </TestWrapper>
    )

    expect(container.firstChild?.childNodes.length).toBe(0)
  })

  it('should not render when playlistId is null', () => {
    mockCreatePlaylistContext.playlistUrl = 'https://open.spotify.com/playlist/123'
    mockCreatePlaylistContext.playlistId = null
    
    const { container } = render(
      <TestWrapper>
        <PlaylistConfirmation />
      </TestWrapper>
    )

    expect(container.firstChild?.childNodes.length).toBe(0)
  })

  it('should render confirmation message when playlist is created', () => {
    render(
      <TestWrapper>
        <PlaylistConfirmation />
      </TestWrapper>
    )

    expect(screen.getByText('Playlist Created Successfully!')).toBeInTheDocument()
    expect(screen.getByText(/Your playlist has been created on Spotify/i)).toBeInTheDocument()
  })

  it('should display playlist ID', () => {
    render(
      <TestWrapper>
        <PlaylistConfirmation />
      </TestWrapper>
    )

    expect(screen.getByText(/Playlist ID:/i)).toBeInTheDocument()
    expect(screen.getByText('123')).toBeInTheDocument()
  })

  it('should open Spotify when clicking "Open in Spotify" button', async () => {
    const user = userEvent.setup()
    
    render(
      <TestWrapper>
        <PlaylistConfirmation />
      </TestWrapper>
    )

    const openButton = screen.getByRole('button', { name: /Open in Spotify/i })
    await user.click(openButton)

    expect(window.open).toHaveBeenCalledWith('https://open.spotify.com/playlist/123', '_blank')
  })

  it('should call clearPlaylist and resetWorkflow when clicking "Create Another" button', async () => {
    const user = userEvent.setup()
    
    render(
      <TestWrapper>
        <PlaylistConfirmation />
      </TestWrapper>
    )

    const resetButton = screen.getByRole('button', { name: /Create Another/i })
    await user.click(resetButton)

    expect(mockClearPlaylist).toHaveBeenCalled()
    expect(mockResetWorkflow).toHaveBeenCalled()
  })

  it('should have both action buttons visible', () => {
    render(
      <TestWrapper>
        <PlaylistConfirmation />
      </TestWrapper>
    )

    expect(screen.getByRole('button', { name: /Open in Spotify/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Create Another/i })).toBeInTheDocument()
  })

  it('should show success indicator icon', () => {
    const { container } = render(
      <TestWrapper>
        <PlaylistConfirmation />
      </TestWrapper>
    )

    const successIcon = container.querySelector('svg.text-green-400')
    expect(successIcon).toBeInTheDocument()
  })
})
