import { renderHook, act, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MatchProvider, useMatch } from './MatchContext';
import { matchApi } from '@/api/match';
import type { components } from '@/api/generated-client';

type NormalizedTrack = components['schemas']['NormalizedTrack'];
type SpotifyTrack = components['schemas']['SpotifyTrack'];
type MatchedDataResponse = components['schemas']['MatchedDataResponse'];

vi.mock('@/api/match');

const mockMatchedResponse: MatchedDataResponse = {
  tracks: [
    {
      original: { artist: 'Artist 1', track: 'Track 1' },
      match: {
        id: 'track1',
        uri: 'spotify:track:track1',
        name: 'Track 1',
        artists: [{ name: 'Artist 1' }],
        album: { name: 'Album 1', images: [] },
        duration_ms: 180000,
      },
      confidence: 1.0,
      strategy: 'exact',
    },
  ],
  totalTracks: 1,
  matchedCount: 1,
  matchRate: 1.0,
};

const mockSearchResults: SpotifyTrack[] = [
  {
    id: 'search1',
    uri: 'spotify:track:search1',
    name: 'Search Result',
    artists: [{ name: 'Artist' }],
    album: { name: 'Album', images: [] },
    duration_ms: 200000,
  },
];

describe('MatchContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should initialize with null matched data', () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    expect(result.current.matchedData).toBeNull();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('should match tracks successfully', async () => {
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(mockMatchedResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    const tracks: NormalizedTrack[] = [{ artist: 'Artist 1', track: 'Track 1' }];

    await act(async () => {
      await result.current.matchTracks(tracks);
    });

    expect(result.current.matchedData).toEqual(mockMatchedResponse);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('should handle match error', async () => {
    const errorMessage = 'Failed to match tracks';
    vi.mocked(matchApi.matchTracksToSpotify).mockRejectedValue(new Error(errorMessage));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    const tracks: NormalizedTrack[] = [{ artist: 'Artist 1', track: 'Track 1' }];

    await act(async () => {
      await result.current.matchTracks(tracks);
    });

    expect(result.current.matchedData).toBeNull();
    expect(result.current.error).toBe(errorMessage);
    expect(result.current.isLoading).toBe(false);
  });

  it('should clear matches', async () => {
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(mockMatchedResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    const tracks: NormalizedTrack[] = [{ artist: 'Artist 1', track: 'Track 1' }];

    await act(async () => {
      await result.current.matchTracks(tracks);
    });

    expect(result.current.matchedData).not.toBeNull();

    act(() => {
      result.current.clearMatches();
    });

    expect(result.current.matchedData).toBeNull();
    expect(result.current.error).toBeNull();
  });

  it('should clear error', async () => {
    vi.mocked(matchApi.matchTracksToSpotify).mockRejectedValue(new Error('Test error'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([{ artist: 'Artist', track: 'Track' }]);
    });

    expect(result.current.error).toBe('Test error');

    act(() => {
      result.current.clearError();
    });

    expect(result.current.error).toBeNull();
  });

  it('should search tracks successfully', async () => {
    vi.mocked(matchApi.searchTracksForManualMatch).mockResolvedValue(mockSearchResults);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyTrack[] = [];

    await act(async () => {
      searchResults = await result.current.searchTracks('test query');
    });

    expect(searchResults).toEqual(mockSearchResults);
    expect(matchApi.searchTracksForManualMatch).toHaveBeenCalledWith('test query');
  });

  it('should handle search error', async () => {
    vi.mocked(matchApi.searchTracksForManualMatch).mockRejectedValue(new Error('Search failed'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let results: SpotifyTrack[] = [];

    await act(async () => {
      results = await result.current.searchTracks('test query');
    });

    expect(results).toEqual([]);
    await waitFor(() => {
      expect(result.current.error).toBe('Search failed');
    });
  });

  it('should remove track from matched data', async () => {
    const multiTrackResponse: MatchedDataResponse = {
      tracks: [
        {
          original: { artist: 'Artist 1', track: 'Track 1' },
          match: {
            id: 'track1',
            uri: 'spotify:track:track1',
            name: 'Track 1',
            artists: [{ name: 'Artist 1' }],
            album: { name: 'Album 1', images: [] },
            duration_ms: 180000,
          },
          confidence: 1.0,
          strategy: 'exact',
        },
        {
          original: { artist: 'Artist 2', track: 'Track 2' },
          match: null,
          confidence: 0,
          strategy: null,
        },
      ],
      totalTracks: 2,
      matchedCount: 1,
      matchRate: 0.5,
    };

    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(multiTrackResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([
        { artist: 'Artist 1', track: 'Track 1' },
        { artist: 'Artist 2', track: 'Track 2' },
      ]);
    });

    expect(result.current.matchedData?.tracks).toHaveLength(2);

    act(() => {
      result.current.removeTrack(1);
    });

    expect(result.current.matchedData?.tracks).toHaveLength(1);
    expect(result.current.matchedData?.tracks[0].original.artist).toBe('Artist 1');
  });

  it('should apply manual match to track', async () => {
    const unmatchedResponse: MatchedDataResponse = {
      tracks: [
        {
          original: { artist: 'Artist 1', track: 'Track 1' },
          match: null,
          confidence: 0,
          strategy: null,
        },
      ],
      totalTracks: 1,
      matchedCount: 0,
      matchRate: 0,
    };

    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(unmatchedResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([{ artist: 'Artist 1', track: 'Track 1' }]);
    });

    expect(result.current.matchedData?.tracks[0].match).toBeNull();

    const manualMatch: SpotifyTrack = {
      id: 'manual1',
      uri: 'spotify:track:manual1',
      name: 'Manual Match',
      artists: [{ name: 'Artist 1' }],
      album: { name: 'Album', images: [] },
      duration_ms: 200000,
    };

    act(() => {
      result.current.applyManualMatch(0, manualMatch);
    });

    expect(result.current.matchedData?.tracks[0].match).toBeDefined();
    expect(result.current.matchedData?.tracks[0].match?.spotifyId).toBe('manual1');
    expect(result.current.matchedData?.tracks[0].match?.uri).toBe('spotify:track:manual1');
    expect(result.current.matchedData?.tracks[0].match?.confidence).toBe(100);
    expect(result.current.matchedData?.tracks[0].match?.method).toBe('Exact');
  });

  it('should retry matching for a specific track', async () => {
    const unmatchedResponse: MatchedDataResponse = {
      tracks: [
        {
          original: { artist: 'Artist 1', track: 'Track 1' },
          match: null,
          confidence: 0,
          strategy: null,
        },
      ],
      totalTracks: 1,
      matchedCount: 0,
      matchRate: 0,
    };

    const retryResponse: MatchedDataResponse = {
      tracks: [
        {
          original: { artist: 'Artist 1', track: 'Track 1' },
          match: {
            id: 'retry1',
            uri: 'spotify:track:retry1',
            name: 'Retry Match',
            artists: [{ name: 'Artist 1' }],
            album: { name: 'Album', images: [] },
            duration_ms: 180000,
          },
          confidence: 0.8,
          strategy: 'fuzzy',
        },
      ],
      totalTracks: 1,
      matchedCount: 1,
      matchRate: 1.0,
    };

    vi.mocked(matchApi.matchTracksToSpotify)
      .mockResolvedValueOnce(unmatchedResponse)
      .mockResolvedValueOnce(retryResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([{ artist: 'Artist 1', track: 'Track 1' }]);
    });

    expect(result.current.matchedData?.tracks[0].match).toBeNull();

    await act(async () => {
      await result.current.retryMatch(0);
    });

    await waitFor(() => {
      expect(result.current.matchedData?.tracks[0].match).not.toBeNull();
      expect(result.current.matchedData?.tracks[0].match?.name).toBe('Retry Match');
    });
  });

  it('should handle retry match error', async () => {
    const unmatchedResponse: MatchedDataResponse = {
      tracks: [
        {
          original: { artist: 'Artist 1', track: 'Track 1' },
          match: null,
          confidence: 0,
          strategy: null,
        },
      ],
      totalTracks: 1,
      matchedCount: 0,
      matchRate: 0,
    };

    vi.mocked(matchApi.matchTracksToSpotify)
      .mockResolvedValueOnce(unmatchedResponse)
      .mockRejectedValueOnce(new Error('Retry failed'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([{ artist: 'Artist 1', track: 'Track 1' }]);
    });

    await act(async () => {
      await result.current.retryMatch(0);
    });

    expect(result.current.error).toBe('Retry failed');
  });
});
