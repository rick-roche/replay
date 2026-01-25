import React, { createContext, useContext, useState, useCallback } from 'react';

interface PlaylistCreationState {
  playlistId: string | null;
  playlistUrl: string | null;
  isCreating: boolean;
  error: string | null;
}

interface CreatePlaylistContextType extends PlaylistCreationState {
  createPlaylist: (trackUris: string[], name: string, description: string | undefined, isPublic: boolean) => Promise<void>;
  clearPlaylist: () => void;
  setPlaylistData: (playlistId: string, playlistUrl: string) => void;
}

const CreatePlaylistContext = createContext<CreatePlaylistContextType | undefined>(undefined);

export const CreatePlaylistProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [state, setState] = useState<PlaylistCreationState>({
    playlistId: null,
    playlistUrl: null,
    isCreating: false,
    error: null,
  });

  const createPlaylist = useCallback(
    async (trackUris: string[], name: string, description: string | undefined, isPublic: boolean) => {
      setState(prev => ({ ...prev, isCreating: true, error: null }));

      try {
        const response = await fetch('/api/playlist/create', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          credentials: 'include', // Include cookies for session
          body: JSON.stringify({
            name,
            description,
            isPublic,
            trackUris,
          }),
        });

        if (!response.ok) {
          const errorData = await response.json();
          throw new Error(errorData.message || 'Failed to create playlist');
        }

        const data = await response.json();
        setState(prev => ({
          ...prev,
          playlistId: data.playlistId,
          playlistUrl: data.url,
          isCreating: false,
        }));
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : 'An error occurred while creating the playlist';
        setState(prev => ({
          ...prev,
          error: errorMessage,
          isCreating: false,
        }));
        throw err;
      }
    },
    []
  );

  const clearPlaylist = useCallback(() => {
    setState({
      playlistId: null,
      playlistUrl: null,
      isCreating: false,
      error: null,
    });
  }, []);

  const setPlaylistData = useCallback((playlistId: string, playlistUrl: string) => {
    setState(prev => ({
      ...prev,
      playlistId,
      playlistUrl,
    }));
  }, []);

  return (
    <CreatePlaylistContext.Provider value={{ ...state, createPlaylist, clearPlaylist, setPlaylistData }}>
      {children}
    </CreatePlaylistContext.Provider>
  );
};

export const useCreatePlaylist = () => {
  const context = useContext(CreatePlaylistContext);
  if (!context) {
    throw new Error('useCreatePlaylist must be used within CreatePlaylistProvider');
  }
  return context;
};
