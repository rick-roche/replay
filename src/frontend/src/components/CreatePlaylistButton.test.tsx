import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Theme } from '@radix-ui/themes'
import { CreatePlaylistButton } from '@/components/CreatePlaylistButton'

// Create mock context values
const mockCreatePlaylist = vi.fn()
const mockLockWorkflow = vi.fn()

let mockCreatePlaylistContext = {
  createPlaylist: mockCreatePlaylist,
  isCreating: false,
  error: null,
  playlistUrl: null
}

let mockPlaylistContext = {
  config: {
    name: 'Test Playlist',
    description: 'Test Description',
    isPublic: true
  },
  updateName: vi.fn(),
  updateDescription: vi.fn(),
  updateIsPublic: vi.fn()
}

let mockMatchContext = {
  matchedData: {
    tracks: [
      {
        name: 'Track 1',
        artist: 'Artist 1',
        match: {
          id: '1',
          uri: 'spotify:track:1',
          name: 'Track 1',
          artist: { name: 'Artist 1' }
        }
      }
    ]
  },
  matchedAlbums: null,
  matchedArtists: null,
  clearMatches: vi.fn()
}

let mockWorkflowContext = {
  currentStep: 'create',
  completedSteps: new Set(),
  goToStep: vi.fn(),
  canGoToStep: () => false,
  markStepComplete: vi.fn(),
  nextStep: vi.fn(),
  previousStep: vi.fn(),
  resetWorkflow: vi.fn(),
  lockWorkflow: mockLockWorkflow
}

beforeEach(() => {
  vi.clearAllMocks()
  localStorage.clear()
  
  mockCreatePlaylistContext = {
    createPlaylist: mockCreatePlaylist,
    isCreating: false,
    error: null,
    playlistUrl: null
  }
  
  mockPlaylistContext = {
    config: {
      name: 'Test Playlist',
      description: 'Test Description',
      isPublic: true
    },
    updateName: vi.fn(),
    updateDescription: vi.fn(),
    updateIsPublic: vi.fn()
  }
  
  mockMatchContext = {
    matchedData: {
      tracks: [
        {
          name: 'Track 1',
          artist: 'Artist 1',
          match: {
            id: '1',
            uri: 'spotify:track:1',
            name: 'Track 1',
            artist: { name: 'Artist 1' }
          }
        }
      ]
    },
    matchedAlbums: null,
    matchedArtists: null,
    clearMatches: vi.fn()
  }
  
  mockWorkflowContext = {
    currentStep: 'create',
    completedSteps: new Set(),
    goToStep: vi.fn(),
    canGoToStep: () => false,
    markStepComplete: vi.fn(),
    nextStep: vi.fn(),
    previousStep: vi.fn(),
    resetWorkflow: vi.fn(),
    lockWorkflow: mockLockWorkflow
  }
})

// Mock the contexts
vi.mock('@/contexts/CreatePlaylistContext', () => ({
  useCreatePlaylist: () => mockCreatePlaylistContext
}))

vi.mock('@/contexts/PlaylistContext', () => ({
  usePlaylist: () => mockPlaylistContext
}))

vi.mock('@/contexts/MatchContext', () => ({
  useMatch: () => mockMatchContext
}))

vi.mock('@/contexts/WorkflowContext', () => ({
  useWorkflow: () => mockWorkflowContext
}))

function TestWrapper({ children }: { children: React.ReactNode }) {
  return <Theme>{children}</Theme>
}

describe('CreatePlaylistButton', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    
    mockCreatePlaylistContext = {
      createPlaylist: mockCreatePlaylist,
      isCreating: false,
      error: null,
      playlistUrl: null
    }
    
    mockPlaylistContext = {
      config: {
        name: 'Test Playlist',
        description: 'Test Description',
        isPublic: true
      },
      updateName: vi.fn(),
      updateDescription: vi.fn(),
      updateIsPublic: vi.fn()
    }
    
    mockMatchContext = {
      matchedData: {
        tracks: [
          {
            name: 'Track 1',
            artist: 'Artist 1',
            match: {
              id: '1',
              uri: 'spotify:track:1',
              name: 'Track 1',
              artist: { name: 'Artist 1' }
            }
          }
        ]
      },
      matchedAlbums: null,
      matchedArtists: null,
      clearMatches: vi.fn()
    }
    
    mockWorkflowContext = {
      currentStep: 'create',
      completedSteps: new Set(),
      goToStep: vi.fn(),
      canGoToStep: () => false,
      markStepComplete: vi.fn(),
      nextStep: vi.fn(),
      previousStep: vi.fn(),
      resetWorkflow: vi.fn(),
      lockWorkflow: mockLockWorkflow
    }
  })

  it('should not render when no matched data exists', () => {
    mockMatchContext.matchedData = null
    
    const { container } = render(
      <TestWrapper>
        <CreatePlaylistButton />
      </TestWrapper>
    )

    expect(container.firstChild?.childNodes.length).toBe(0)
  })

  it('should not render when matched tracks array is empty', () => {
    mockMatchContext.matchedData = { tracks: [] }
    
    const { container } = render(
      <TestWrapper>
        <CreatePlaylistButton />
      </TestWrapper>
    )

    expect(container.firstChild?.childNodes.length).toBe(0)
  })

  it('should render create playlist button with matched tracks', () => {
    render(
      <TestWrapper>
        <CreatePlaylistButton />
      </TestWrapper>
    )

    expect(screen.getByRole('button', { name: /Create Playlist on Spotify/i })).toBeInTheDocument()
    expect(screen.getByText(/1 matched tracks/i)).toBeInTheDocument()
  })

  it('should call lockWorkflow after successful playlist creation', async () => {
    const user = userEvent.setup()
    mockCreatePlaylist.mockResolvedValue(undefined)
    
    render(
      <TestWrapper>
        <CreatePlaylistButton />
      </TestWrapper>
    )

    const button = screen.getByRole('button')
    await user.click(button)

    await waitFor(() => {
      expect(mockCreatePlaylist).toHaveBeenCalledWith(
        ['spotify:track:1'],
        'Test Playlist',
        'Test Description',
        true
      )
      expect(mockLockWorkflow).toHaveBeenCalled()
    })
  })

  it('should not call lockWorkflow if playlist creation fails', async () => {
    const user = userEvent.setup()
    mockCreatePlaylist.mockRejectedValue(new Error('Failed to create playlist'))
    
    render(
      <TestWrapper>
        <CreatePlaylistButton />
      </TestWrapper>
    )

    const button = screen.getByRole('button')
    await user.click(button)

    await waitFor(() => {
      expect(mockCreatePlaylist).toHaveBeenCalled()
      expect(mockLockWorkflow).not.toHaveBeenCalled()
    })
  })

  it('should validate playlist name is required', async () => {
    const user = userEvent.setup()
    mockPlaylistContext.config.name = ''
    
    render(
      <TestWrapper>
        <CreatePlaylistButton />
      </TestWrapper>
    )

    const button = screen.getByRole('button')
    await user.click(button)

    expect(screen.getByText('Playlist name is required')).toBeInTheDocument()
    expect(mockCreatePlaylist).not.toHaveBeenCalled()
    expect(mockLockWorkflow).not.toHaveBeenCalled()
  })

  it('should disable button when no matched tracks exist', () => {
    mockMatchContext.matchedData = {
      tracks: [
        {
          name: 'Track 1',
          artist: 'Artist 1',
          match: null // No match
        }
      ]
    }
    
    render(
      <TestWrapper>
        <CreatePlaylistButton />
      </TestWrapper>
    )

    const button = screen.getByRole('button')
    expect(button).toBeDisabled()
    expect(screen.getByText(/0 matched tracks/i)).toBeInTheDocument()
  })

  it('should disable button while creating', () => {
    mockCreatePlaylistContext.isCreating = true
    
    render(
      <TestWrapper>
        <CreatePlaylistButton />
      </TestWrapper>
    )

    const button = screen.getByRole('button')
    expect(button).toBeDisabled()
    expect(screen.getByText('Creating Playlist...')).toBeInTheDocument()
  })

  it('should display error from context', () => {
    mockCreatePlaylistContext.error = 'Failed to create playlist'
    
    render(
      <TestWrapper>
        <CreatePlaylistButton />
      </TestWrapper>
    )

    expect(screen.getByText('Failed to create playlist')).toBeInTheDocument()
  })
})
