import {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from 'react';
import type { components } from '@/api/generated-client';
import { matchApi } from '@/api/match';

type NormalizedTrack = components['schemas']['NormalizedTrack']
type MatchedDataResponse = components['schemas']['MatchedDataResponse']
type SpotifyTrack = components['schemas']['SpotifyTrack']

interface MatchContextType {
  matchedData: MatchedDataResponse | null;
  isLoading: boolean;
  error: string | null;
  matchTracks: (tracks: NormalizedTrack[]) => Promise<void>;
  clearMatches: () => void;
  clearError: () => void;
  // Methods for handling unmatched tracks
  retryMatch: (trackIndex: number) => Promise<void>;
  removeTrack: (trackIndex: number) => void;
  applyManualMatch: (trackIndex: number, spotifyTrack: SpotifyTrack) => void;
  searchTracks: (query: string) => Promise<SpotifyTrack[]>;
}

const MatchContext = createContext<MatchContextType | undefined>(undefined);

export function MatchProvider({ children }: { children: ReactNode }) {
  const [matchedData, setMatchedData] = useState<MatchedDataResponse | null>(
    null
  );
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const matchTracks = useCallback(
    async (tracks: NormalizedTrack[]) => {
      setIsLoading(true);
      setError(null);

      try {
        const result = await matchApi.matchTracksToSpotify({ tracks });
        setMatchedData(result);
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to match tracks';
        setError(errorMessage);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  const retryMatch = useCallback(
    async (trackIndex: number) => {
      if (!matchedData?.tracks || !matchedData.tracks[trackIndex]) return;

      const track = matchedData.tracks[trackIndex];
      setError(null);

      try {
        // For now, retry with the same matching - could be improved with a dedicated retry endpoint
        const result = await matchApi.matchTracksToSpotify({
          tracks: [track.sourceTrack],
        });

        if (result.tracks && result.tracks[0]) {
          const updatedTracks = [...matchedData.tracks];
          updatedTracks[trackIndex] = result.tracks[0];
          setMatchedData({
            ...matchedData,
            tracks: updatedTracks,
          });
        }
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to retry matching';
        setError(errorMessage);
      }
    },
    [matchedData]
  );

  const removeTrack = useCallback((trackIndex: number) => {
    setMatchedData((prev) => {
      if (!prev?.tracks) return prev;

      const updatedTracks = prev.tracks.filter((_, i) => i !== trackIndex);
      return {
        ...prev,
        tracks: updatedTracks,
      };
    });
  }, []);

  const applyManualMatch = useCallback(
    (trackIndex: number, spotifyTrack: SpotifyTrack) => {
      if (!matchedData?.tracks || !matchedData.tracks[trackIndex]) return;

      const track = matchedData.tracks[trackIndex];

      const updatedTracks = [...matchedData.tracks];
      updatedTracks[trackIndex] = {
        ...track,
        match: {
          spotifyId: spotifyTrack.id,
          name: spotifyTrack.name,
          artist: spotifyTrack.artist,
          album: spotifyTrack.album || null,
          uri: spotifyTrack.uri,
          confidence: 100, // Manual selections are high confidence
          method: 'Exact', // Mark as exact since user confirmed it
        },
      };

      setMatchedData({
        ...matchedData,
        tracks: updatedTracks,
      });
    },
    [matchedData]
  );

  const searchTracks = useCallback(async (query: string) => {
    if (!query.trim()) return [];

    try {
      return await matchApi.searchTracksForManualMatch(query);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Search failed';
      setError(errorMessage);
      return [];
    }
  }, []);

  const clearMatches = useCallback(() => {
    setMatchedData(null);
    setError(null);
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return (
    <MatchContext.Provider
      value={{
        matchedData,
        isLoading,
        error,
        matchTracks,
        clearMatches,
        clearError,
        retryMatch,
        removeTrack,
        applyManualMatch,
        searchTracks,
      }}
    >
      {children}
    </MatchContext.Provider>
  );
}

export function useMatch() {
  const context = useContext(MatchContext);
  if (!context) {
    throw new Error('useMatch must be used within a MatchProvider');
  }
  return context;
}
