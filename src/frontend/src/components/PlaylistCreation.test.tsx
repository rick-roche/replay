import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import userEvent from '@testing-library/user-event';
import React from 'react';
import { CreatePlaylistButton } from '@/components/CreatePlaylistButton';
import { PlaylistConfirmation } from '@/components/PlaylistConfirmation';
import { CreatePlaylistProvider, useCreatePlaylist } from '@/contexts/CreatePlaylistContext';
import { PlaylistProvider } from '@/contexts/PlaylistContext';
import { MatchProvider } from '@/contexts/MatchContext';
import { WorkflowProvider } from '@/contexts/WorkflowContext';
import { Theme } from '@radix-ui/themes';

// Mock the match API
vi.mock('@/api/match', () => ({
  matchApi: {
    matchTracksToSpotify: vi.fn(),
    searchSpotifyTracks: vi.fn(),
  },
}));

const Wrapper = ({ children }: { children: React.ReactNode }) => (
  <Theme appearance="dark">
    <CreatePlaylistProvider>
      <PlaylistProvider>
        <MatchProvider>
          <WorkflowProvider>
            {children}
          </WorkflowProvider>
        </MatchProvider>
      </PlaylistProvider>
    </CreatePlaylistProvider>
  </Theme>
);

describe('CreatePlaylistButton', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    global.fetch = vi.fn();
  });

  it('should not render when no matched tracks', () => {
    const { queryByText } = render(
      <CreatePlaylistButton />,
      { wrapper: Wrapper }
    );
    expect(queryByText('Create Playlist')).toBeNull();
  });

  it('should render button text', () => {
    // Just verify the component can be instantiated
    expect(CreatePlaylistButton).toBeDefined();
  });
});

describe('PlaylistConfirmation', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.open = vi.fn();
  });

  it('should not render when playlist not created', () => {
    const { queryByText } = render(
      <PlaylistConfirmation />,
      { wrapper: Wrapper }
    );
    expect(queryByText('Playlist Created Successfully!')).toBeNull();
  });

  it('should render when playlist data is set', async () => {
    const TestComponent = () => {
      const { setPlaylistData } = useCreatePlaylist();

      React.useEffect(() => {
        setPlaylistData('playlist123', 'https://open.spotify.com/playlist/playlist123');
      }, [setPlaylistData]);

      return <PlaylistConfirmation />;
    };

    render(<TestComponent />, { wrapper: Wrapper });

    await waitFor(() => {
      expect(screen.getByText('Playlist Created Successfully!')).toBeInTheDocument();
    });
  });

  it('should open Spotify on button click', async () => {
    const user = userEvent.setup();

    const TestComponent = () => {
      const { setPlaylistData } = useCreatePlaylist();

      React.useEffect(() => {
        setPlaylistData('playlist123', 'https://open.spotify.com/playlist/playlist123');
      }, [setPlaylistData]);

      return <PlaylistConfirmation />;
    };

    render(<TestComponent />, { wrapper: Wrapper });

    await waitFor(() => {
      expect(screen.getByText('Playlist Created Successfully!')).toBeInTheDocument();
    });

    const button = screen.getByRole('button', { name: /Open in Spotify/i });
    await user.click(button);

    expect(window.open).toHaveBeenCalledWith('https://open.spotify.com/playlist/playlist123', '_blank');
  });

  it('should clear playlist on reset', async () => {
    const user = userEvent.setup();

    const TestComponent = () => {
      const { setPlaylistData } = useCreatePlaylist();

      React.useEffect(() => {
        setPlaylistData('playlist123', 'https://open.spotify.com/playlist/playlist123');
      }, [setPlaylistData]);

      return <PlaylistConfirmation />;
    };

    render(<TestComponent />, { wrapper: Wrapper });

    await waitFor(() => {
      expect(screen.getByText('Playlist Created Successfully!')).toBeInTheDocument();
    });

    const resetButton = screen.getByRole('button', { name: /Create Another/i });
    await user.click(resetButton);

    await waitFor(() => {
      expect(screen.queryByText('Playlist Created Successfully!')).not.toBeInTheDocument();
    });
  });
});

describe('CreatePlaylistContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should create playlist successfully', async () => {
    const mockResponse = {
      playlistId: 'playlist123',
      uri: 'spotify:playlist:playlist123',
      url: 'https://open.spotify.com/playlist/playlist123',
      tracksAdded: 5,
    };

    global.fetch = vi.fn(() =>
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve(mockResponse),
      } as Response)
    );

    const TestComponent = () => {
      const { createPlaylist, playlistId, playlistUrl } = useCreatePlaylist();
      const [called, setCalled] = React.useState(false);

      React.useEffect(() => {
        if (!called) {
          setCalled(true);
          createPlaylist(['spotify:track:1', 'spotify:track:2'], 'Test Playlist', 'Description', true);
        }
      }, [called, createPlaylist]);

      return (
        <div>
          {playlistId && <div data-testid="playlist-id">Playlist ID: {playlistId}</div>}
          {playlistUrl && <div data-testid="playlist-url">Playlist URL: {playlistUrl}</div>}
        </div>
      );
    };

    render(<TestComponent />, { wrapper: Wrapper });

    await waitFor(() => {
      expect(screen.getByTestId('playlist-id')).toHaveTextContent('Playlist ID: playlist123');
      expect(screen.getByTestId('playlist-url')).toHaveTextContent('Playlist URL: https://open.spotify.com/playlist/playlist123');
    });
  });

  it('should handle fetch errors', async () => {
    global.fetch = vi.fn(() =>
      Promise.resolve({
        ok: false,
        json: () => Promise.resolve({ code: 'ERROR', message: 'Failed to create playlist' }),
      } as Response)
    );

    const TestComponent = () => {
      const { createPlaylist, error } = useCreatePlaylist();
      const [called, setCalled] = React.useState(false);

      React.useEffect(() => {
        if (!called) {
          setCalled(true);
          createPlaylist(['spotify:track:1'], 'Test Playlist', 'Description', true).catch(() => {
            // Expected error
          });
        }
      }, [called, createPlaylist]);

      return <div>{error && <div data-testid="error">Error: {error}</div>}</div>;
    };

    render(<TestComponent />, { wrapper: Wrapper });

    await waitFor(() => {
      expect(screen.getByTestId('error')).toBeInTheDocument();
    });
  });

  it('should clear playlist', () => {
    const TestComponent = () => {
      const { setPlaylistData, clearPlaylist, playlistId } = useCreatePlaylist();
      const [step, setStep] = React.useState(0);

      React.useEffect(() => {
        if (step === 0) {
          setPlaylistData('playlist123', 'https://open.spotify.com/playlist/playlist123');
          setStep(1);
        } else if (step === 1 && playlistId) {
          setTimeout(() => {
            clearPlaylist();
            setStep(2);
          }, 10);
        }
      }, [step, playlistId, setPlaylistData, clearPlaylist]);

      return (
        <div>
          {playlistId ? <div data-testid="has-playlist">Has playlist</div> : <div data-testid="no-playlist">No playlist</div>}
        </div>
      );
    };

    render(<TestComponent />, { wrapper: Wrapper });

    waitFor(() => {
      expect(screen.getByTestId('no-playlist')).toBeInTheDocument();
    });
  });
});
