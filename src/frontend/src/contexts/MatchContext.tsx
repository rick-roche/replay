import {
  createContext,
  useContext,
  useState,
  useCallback,
  useRef,
  type ReactNode,
} from 'react';
import type { components } from '@/api/generated-client';
import { matchApi } from '@/api/match';

type NormalizedTrack = components['schemas']['NormalizedTrack']
type NormalizedAlbum = components['schemas']['NormalizedAlbum']
type NormalizedArtist = components['schemas']['NormalizedArtist']
type MatchedDataResponse = components['schemas']['MatchedDataResponse']
type MatchedAlbumsResponse = components['schemas']['MatchedAlbumsResponse']
type MatchedArtistsResponse = components['schemas']['MatchedArtistsResponse']
type SpotifyTrack = components['schemas']['SpotifyTrack']
type SpotifyAlbumInfo = components['schemas']['SpotifyAlbumInfo']
type SpotifyArtistInfo = components['schemas']['SpotifyArtistInfo']

interface MatchContextType {
  matchedData: MatchedDataResponse | null;
  matchedAlbums: MatchedAlbumsResponse | null;
  matchedArtists: MatchedArtistsResponse | null;
  isLoading: boolean;
  error: string | null;
  matchTracks: (tracks: NormalizedTrack[]) => Promise<void>;
  appendMatches: (tracks: NormalizedTrack[]) => Promise<void>;
  matchAlbums: (albums: NormalizedAlbum[]) => Promise<void>;
  matchArtists: (artists: NormalizedArtist[]) => Promise<void>;
  clearMatches: () => void;
  clearError: () => void;
  // Methods for handling unmatched tracks
  retryMatch: (trackIndex: number) => Promise<void>;
  removeTrack: (trackIndex: number) => void;
  moveTrack: (fromIndex: number, toIndex: number) => void;
  applyManualMatch: (trackIndex: number, spotifyTrack: SpotifyTrack) => void;
  searchTracks: (query: string) => Promise<SpotifyTrack[]>;
  searchAlbums: (query: string) => Promise<SpotifyAlbumInfo[]>;
  searchArtists: (query: string) => Promise<SpotifyArtistInfo[]>;
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
  const [matchedAlbums, setMatchedAlbums] = useState<MatchedAlbumsResponse | null>(
    null
  );
  const [matchedArtists, setMatchedArtists] = useState<MatchedArtistsResponse | null>(
    null
  );
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const generation = useRef(0);
  const retries = useRef(new WeakMap<object, number>());

  const matchTracks = useCallback(
    async (tracks: NormalizedTrack[]) => {
      const request = ++generation.current;
      setIsLoading(true);
      setError(null);

      try {
        const result = await matchApi.matchTracksToSpotify({ tracks });
        if (request !== generation.current) return;
        setMatchedAlbums(null);
        setMatchedArtists(null);
        setMatchedData(withCounts(result));
      } catch (err) {
        if (request !== generation.current) return;
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to match tracks';
        setError(errorMessage);
      } finally {
        if (request === generation.current) setIsLoading(false);
      }
    },
    []
  );

  const matchAlbums = useCallback(
    async (albums: NormalizedAlbum[]) => {
      const request = ++generation.current;
      setIsLoading(true);
      setError(null);

      try {
        const result = await matchApi.matchAlbumsToSpotify({ albums });
        if (request !== generation.current) return;
        setMatchedAlbums(result);
        setMatchedArtists(null);
        setMatchedData(withCounts({ tracks: result.albums.flatMap(album =>
          (album.match?.tracks ?? []).map(match => ({
            sourceTrack: { name: match.name, artist: match.artist, album: album.sourceAlbum.name, source: album.sourceAlbum.source, sourceMetadata: album.sourceAlbum.sourceMetadata },
            match,
          }))
        ) }));
      } catch (err) {
        if (request !== generation.current) return;
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to match albums';
        setError(errorMessage);
      } finally {
        if (request === generation.current) setIsLoading(false);
      }
    },
    []
  );

  const matchArtists = useCallback(
    async (artists: NormalizedArtist[]) => {
      const request = ++generation.current;
      setIsLoading(true);
      setError(null);

      try {
        const result = await matchApi.matchArtistsToSpotify({ artists });
        if (request !== generation.current) return;
        setMatchedArtists(result);
        setMatchedAlbums(null);
        setMatchedData(withCounts({ tracks: result.artists.flatMap(artist =>
          (artist.match?.topTracks ?? []).map(match => ({
            sourceTrack: { name: match.name, artist: match.artist, source: artist.sourceArtist.source, sourceMetadata: artist.sourceArtist.sourceMetadata },
            match,
          }))
        ) }));
      } catch (err) {
        if (request !== generation.current) return;
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to match artists';
        setError(errorMessage);
      } finally {
        if (request === generation.current) setIsLoading(false);
      }
    },
    []
  );

  const appendMatches = useCallback(
    async (tracks: NormalizedTrack[]) => {
      if (tracks.length === 0) return;
      const request = ++generation.current;
      setIsLoading(true);
      setError(null);

      try {
        const result = await matchApi.matchTracksToSpotify({ tracks });
        if (request !== generation.current) return;
        setMatchedData(prev => {
          const existing = prev?.tracks ?? [];
          const seen = new Set(existing.map((t) => trackKey(t.sourceTrack)));

          const newTracks: MatchedDataResponse['tracks'] = [];
          for (const t of result.tracks ?? []) {
            const key = trackKey(t.sourceTrack);
            if (seen.has(key)) continue;
            seen.add(key);
            newTracks.push(t);
          }

          return withCounts({ tracks: [...existing, ...newTracks] });
        });
      } catch (err) {
        if (request !== generation.current) return;
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to match new tracks';
        setError(errorMessage);
      } finally {
        if (request === generation.current) setIsLoading(false);
      }
    },
    []
  );

  const retryMatch = useCallback(
    async (trackIndex: number) => {
      if (!matchedData?.tracks || !matchedData.tracks[trackIndex]) return;

      const track = matchedData.tracks[trackIndex];
      const request = generation.current;
      const retry = (retries.current.get(track.sourceTrack) ?? 0) + 1;
      retries.current.set(track.sourceTrack, retry);
      setError(null);

      try {
        // For now, retry with the same matching - could be improved with a dedicated retry endpoint
        const result = await matchApi.matchTracksToSpotify({
          tracks: [track.sourceTrack],
        });

        if (request !== generation.current || retries.current.get(track.sourceTrack) !== retry) return;
        const replacement = result.tracks?.[0];
        if (replacement) {
          // Source object identity survives reorder, but not removal/replacement.
          setMatchedData(prev => prev ? withCounts({ ...prev, tracks: prev.tracks.map(item =>
            item === track ? { ...replacement, sourceTrack: track.sourceTrack } : item
          ) }) : prev);
        }
      } catch (err) {
        if (request !== generation.current || retries.current.get(track.sourceTrack) !== retry) return;
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

      setMatchedData(prev => prev ? withCounts({ ...prev, tracks: prev.tracks.map(item => item !== track ? item : {
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
      }) }) : prev);
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

  const searchAlbums = useCallback(async (query: string) => {
    if (!query.trim()) return [];

    try {
      return await matchApi.searchAlbumsForManualMatch(query);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Search failed';
      setError(errorMessage);
      return [];
    }
  }, []);

  const searchArtists = useCallback(async (query: string) => {
    if (!query.trim()) return [];

    try {
      return await matchApi.searchArtistsForManualMatch(query);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Search failed';
      setError(errorMessage);
      return [];
    }
  }, []);

  const clearMatches = useCallback(() => {
    generation.current++;
    setIsLoading(false);
    setMatchedData(null);
    setMatchedAlbums(null);
    setMatchedArtists(null);
    setError(null);
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return (
    <MatchContext.Provider
      value={{
        matchedData,
        matchedAlbums,
        matchedArtists,
        isLoading,
        error,
        matchTracks,
        appendMatches,
        matchAlbums,
        matchArtists,
        clearMatches,
        clearError,
        retryMatch,
        removeTrack,
        moveTrack,
        applyManualMatch,
        searchTracks,
        searchAlbums,
        searchArtists,
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
