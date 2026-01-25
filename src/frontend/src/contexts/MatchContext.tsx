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
  appendMatches: (tracks: NormalizedTrack[]) => Promise<void>;
  clearMatches: () => void;
  clearError: () => void;
  // Methods for handling unmatched tracks
  retryMatch: (trackIndex: number) => Promise<void>;
  removeTrack: (trackIndex: number) => void;
  moveTrack: (fromIndex: number, toIndex: number) => void;
  applyManualMatch: (trackIndex: number, spotifyTrack: SpotifyTrack) => void;
  searchTracks: (query: string) => Promise<SpotifyTrack[]>;
}

const MatchContext = createContext<MatchContextType | undefined>(undefined);

const withCounts = (data: MatchedDataResponse): MatchedDataResponse => {
  const tracks = data.tracks ?? [];
  const matchedCount = tracks.filter((track) => Boolean(track.match)).length;
  const unmatchedCount = tracks.length - matchedCount;

  return {
    ...data,
    tracks,
    matchedCount,
    unmatchedCount,
    totalTracks: tracks.length,
  };
};

const trackKey = (t: NormalizedTrack) =>
  `${(t.name ?? '').toLowerCase()}|${(t.artist ?? '').toLowerCase()}|${(t.album ?? '').toLowerCase()}`;

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
        setMatchedData(withCounts(result));
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

  const appendMatches = useCallback(
    async (tracks: NormalizedTrack[]) => {
      if (tracks.length === 0) return;
      setIsLoading(true);
      setError(null);

      try {
        const result = await matchApi.matchTracksToSpotify({ tracks });
        const existing = matchedData?.tracks ?? [];
        const seen = new Set(existing.map((t) => trackKey(t.sourceTrack)));

        const newTracks: MatchedDataResponse['tracks'] = [];
        for (const t of result.tracks ?? []) {
          const key = trackKey(t.sourceTrack);
          if (seen.has(key)) continue;
          seen.add(key);
          newTracks.push(t);
        }

        const merged: MatchedDataResponse = {
          tracks: [...existing, ...newTracks],
        };
        setMatchedData(withCounts(merged));
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to match new tracks';
        setError(errorMessage);
      } finally {
        setIsLoading(false);
      }
    },
    [matchedData]
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
          setMatchedData(
            withCounts({
              ...matchedData,
              tracks: updatedTracks,
            })
          );
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
      return withCounts({
        ...prev,
        tracks: updatedTracks,
      });
    });
  }, []);

  const moveTrack = useCallback((fromIndex: number, toIndex: number) => {
    setMatchedData((prev) => {
      if (!prev?.tracks) return prev;
      if (fromIndex === toIndex) return prev;

      const updatedTracks = [...prev.tracks];
      const [moved] = updatedTracks.splice(fromIndex, 1);
      if (!moved) return prev;
      updatedTracks.splice(toIndex, 0, moved);

      return withCounts({
        ...prev,
        tracks: updatedTracks,
      });
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

      setMatchedData(
        withCounts({
          ...matchedData,
          tracks: updatedTracks,
        })
      );
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
        appendMatches,
        clearMatches,
        clearError,
        retryMatch,
        removeTrack,
        moveTrack,
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
