import React, { createContext, useContext, useState, useCallback, useRef } from 'react';
import { handleApiError } from '../api/errors';
import { client } from '../api/client';
import type { components } from '../api/generated-client';

interface PlaylistCreationState {
  playlistId: string | null;
  playlistUrl: string | null;
  isCreating: boolean;
  error: string | null;
  isComplete: boolean;
  tracksAdded: number;
  creationUnconfirmed: boolean;
}

interface CreatePlaylistContextType extends PlaylistCreationState {
  createPlaylist: (trackUris: string[], name: string, description: string | undefined, isPublic: boolean) => Promise<boolean>;
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
    isComplete: true,
    tracksAdded: 0,
    creationUnconfirmed: false,
  });
  const generation = useRef(0);
  const creationStarted = useRef(false);

  const createPlaylist = useCallback(
    async (trackUris: string[], name: string, description: string | undefined, isPublic: boolean) => {
      if (creationStarted.current) return false;
      creationStarted.current = true;
      const request = generation.current;
      let unconfirmed = true;
      setState(prev => ({ ...prev, isCreating: true, error: null }));

      try {
        const { data: created, error: failure, response } = await client.POST('/api/playlist/create', {
          body: {
            name,
            description,
            isPublic,
            trackUris,
          },
        });

        // Partial creation is returned as 502 with the created playlist, not ApiError.
        const data: unknown = created ?? failure;
        if (request !== generation.current) return false;
        if (typeof data === 'object' && data !== null && 'code' in data && typeof data.code === 'string') {
          unconfirmed = data.code === 'PLAYLIST_CREATION_UNCONFIRMED';
        }
        if (!isPlaylistResult(data)) handleApiError(data, 'Failed to create playlist');
        if (!response.ok && data.isComplete !== false) handleApiError(data, 'Failed to create playlist');
        setState(prev => ({
          ...prev,
          playlistId: data.playlistId,
          playlistUrl: data.url,
          isCreating: false,
          isComplete: data.isComplete !== false,
          tracksAdded: Number(data.tracksAdded) || 0,
          error: data.isComplete === false ? data.errorMessage || 'Only part of the playlist could be created. Review it in Spotify before starting another.' : null,
        }));
        return true;
      } catch (err) {
        if (request !== generation.current) return false;
        // A lost response cannot tell us whether Spotify applied this POST.
        creationStarted.current = unconfirmed;
        const errorMessage = unconfirmed
          ? 'Spotify may have created this playlist. Check your Spotify library before starting over to avoid duplicates.'
          : err instanceof Error ? err.message : 'An error occurred while creating the playlist';
        setState(prev => ({
          ...prev,
          error: errorMessage,
          isCreating: false,
          creationUnconfirmed: unconfirmed,
        }));
        throw err;
      }
    },
    []
  );

  const clearPlaylist = useCallback(() => {
    generation.current++;
    creationStarted.current = false;
    setState({
      playlistId: null,
      playlistUrl: null,
      isCreating: false,
      error: null,
      isComplete: true,
      tracksAdded: 0,
      creationUnconfirmed: false,
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

type PlaylistResult = Pick<components['schemas']['PlaylistCreationResponse'], 'playlistId' | 'url'> & Partial<components['schemas']['PlaylistCreationResponse']>;

function isPlaylistResult(value: unknown): value is PlaylistResult {
  return typeof value === 'object' && value !== null &&
    'playlistId' in value && typeof value.playlistId === 'string' &&
    'url' in value && typeof value.url === 'string' &&
    (!('isComplete' in value) || typeof value.isComplete === 'boolean') &&
    (!('tracksAdded' in value) || typeof value.tracksAdded === 'number' || typeof value.tracksAdded === 'string') &&
    (!('errorMessage' in value) || value.errorMessage === null || typeof value.errorMessage === 'string');
}

export const useCreatePlaylist = () => {
  const context = useContext(CreatePlaylistContext);
  if (!context) {
    throw new Error('useCreatePlaylist must be used within CreatePlaylistProvider');
  }
  return context;
};
