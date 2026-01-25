import { renderHook, act, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MatchProvider, useMatch } from './MatchContext';
import { matchApi } from '@/api/match';
import type { components } from '@/api/generated-client';

type NormalizedTrack = components['schemas']['NormalizedTrack'];
type SpotifyTrack = components['schemas']['SpotifyTrack'];
type MatchedDataResponse = components['schemas']['MatchedDataResponse'];

vi.mock('@/api/match');

const buildNormalizedTrack = (name: string, artist = 'Artist'): NormalizedTrack => ({
  name,
  artist,
  album: 'Album',
  source: 'lastfm',
  sourceMetadata: {},
});

const buildMatch = (id: string, name: string, artist: string) => ({
  spotifyId: id,
  name,
  artist,
  album: 'Album',
  uri: `spotify:track:${id}`,
  confidence: 90,
  method: 'Exact' as const,
});

const responseWithTracks = (
  tracks: MatchedDataResponse['tracks']
): MatchedDataResponse => ({
  tracks,
  totalTracks: tracks.length,
  matchedCount: tracks.filter((t) => t.match).length,
  unmatchedCount: tracks.filter((t) => !t.match).length,
});

describe('MatchContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('initializes with empty state', () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });
    expect(result.current.matchedData).toBeNull();
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('matches tracks successfully and sets counts', async () => {
    const normalized = buildNormalizedTrack('Track 1', 'Artist 1');
    const apiResponse = responseWithTracks([
      { sourceTrack: normalized, match: buildMatch('t1', 'Track 1', 'Artist 1') },
    ]);

    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([normalized]);
    });

    expect(result.current.matchedData?.tracks).toHaveLength(1);
    expect(result.current.matchedData?.matchedCount).toBe(1);
    expect(result.current.matchedData?.unmatchedCount).toBe(0);
  });

  it('handles match errors', async () => {
    vi.mocked(matchApi.matchTracksToSpotify).mockRejectedValue(new Error('boom'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    expect(result.current.matchedData).toBeNull();
    expect(result.current.error).toBe('boom');
  });

  it('clears matches and errors', async () => {
    const normalized = buildNormalizedTrack('Track 1');
    const apiResponse = responseWithTracks([
      { sourceTrack: normalized, match: buildMatch('t1', 'Track 1', 'Artist 1') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([normalized]);
    });

    act(() => result.current.clearError());
    expect(result.current.error).toBeNull();

    act(() => result.current.clearMatches());
    expect(result.current.matchedData).toBeNull();
  });

  it('searches tracks and handles failures', async () => {
    const searchResults: SpotifyTrack[] = [
      { id: 's1', name: 'Found', artist: 'Artist', album: 'Album', uri: 'spotify:track:s1' },
    ];

    vi.mocked(matchApi.searchTracksForManualMatch).mockResolvedValue(searchResults);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let results: SpotifyTrack[] = [];
    await act(async () => {
      results = await result.current.searchTracks('query');
    });
    expect(results).toEqual(searchResults);

    vi.mocked(matchApi.searchTracksForManualMatch).mockRejectedValue(new Error('fail'));
    await act(async () => {
      results = await result.current.searchTracks('query');
    });
    expect(results).toEqual([]);
    await waitFor(() => expect(result.current.error).toBe('fail'));
  });

  it('removes tracks and recalculates counts', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
      { sourceTrack: buildNormalizedTrack('Track 2'), match: null },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });
    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1'), buildNormalizedTrack('Track 2')]);
    });

    act(() => result.current.removeTrack(1));

    expect(result.current.matchedData?.tracks).toHaveLength(1);
    expect(result.current.matchedData?.matchedCount).toBe(1);
    expect(result.current.matchedData?.unmatchedCount).toBe(0);
  });

  it('applies manual matches and updates counts', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: null },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });
    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    const manualMatch: SpotifyTrack = {
      id: 'manual',
      name: 'Manual',
      artist: 'Artist',
      album: 'Album',
      uri: 'spotify:track:manual',
    };

    act(() => result.current.applyManualMatch(0, manualMatch));

    expect(result.current.matchedData?.tracks[0].match?.spotifyId).toBe('manual');
    expect(result.current.matchedData?.matchedCount).toBe(1);
    expect(result.current.matchedData?.unmatchedCount).toBe(0);
  });

  it('retries matching a single track', async () => {
    const unmatched = responseWithTracks([{ sourceTrack: buildNormalizedTrack('Track 1'), match: null }]);
    const retried = responseWithTracks([
      {
        sourceTrack: buildNormalizedTrack('Track 1'),
        match: buildMatch('retry', 'Retry Track', 'Artist'),
      },
    ]);

    vi.mocked(matchApi.matchTracksToSpotify)
      .mockResolvedValueOnce(unmatched)
      .mockResolvedValueOnce(retried);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });
    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    await act(async () => {
      await result.current.retryMatch(0);
    });

    await waitFor(() => expect(result.current.matchedData?.tracks[0].match?.spotifyId).toBe('retry'));
  });

  it('moves tracks to new positions', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
      { sourceTrack: buildNormalizedTrack('Track 2'), match: buildMatch('t2', 'Track 2', 'Artist 2') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });
    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1'), buildNormalizedTrack('Track 2')]);
    });

    act(() => result.current.moveTrack(1, 0));

    expect(result.current.matchedData?.tracks[0].sourceTrack.name).toBe('Track 2');
    expect(result.current.matchedData?.tracks[1].sourceTrack.name).toBe('Track 1');
  });

  it('appends matches without duplicating existing tracks', async () => {
    const initialResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
    ]);
    const additionalResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1b', 'Track 1', 'Artist 1') }, // duplicate source
      { sourceTrack: buildNormalizedTrack('Track 3'), match: buildMatch('t3', 'Track 3', 'Artist 3') },
    ]);

    vi.mocked(matchApi.matchTracksToSpotify)
      .mockResolvedValueOnce(initialResponse)
      .mockResolvedValueOnce(additionalResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    await act(async () => {
      await result.current.appendMatches([
        buildNormalizedTrack('Track 1'),
        buildNormalizedTrack('Track 3'),
      ]);
    });

    expect(result.current.matchedData?.tracks).toHaveLength(2);
    expect(result.current.matchedData?.matchedCount).toBe(2);
    expect(result.current.matchedData?.tracks[1].sourceTrack.name).toBe('Track 3');
  });

  it('clears all matches', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    expect(result.current.matchedData).not.toBeNull();

    act(() => {
      result.current.clearMatches();
    });

    expect(result.current.matchedData).toBeNull();
  });

  it('clears error state', async () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    vi.mocked(matchApi.matchTracksToSpotify).mockRejectedValueOnce(new Error('Match failed'));

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    expect(result.current.error).not.toBeNull();

    act(() => {
      result.current.clearError();
    });

    expect(result.current.error).toBeNull();
  });

  it('removes a track from matches', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
      { sourceTrack: buildNormalizedTrack('Track 2'), match: buildMatch('t2', 'Track 2', 'Artist 2') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1'), buildNormalizedTrack('Track 2')]);
    });

    expect(result.current.matchedData?.tracks).toHaveLength(2);

    act(() => {
      result.current.removeTrack(0);
    });

    expect(result.current.matchedData?.tracks).toHaveLength(1);
    expect(result.current.matchedData?.tracks[0].sourceTrack.name).toBe('Track 2');
  });

  it('handles empty search query', async () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyTrack[] = [];
    await act(async () => {
      searchResults = await result.current.searchTracks('');
    });

    expect(searchResults).toEqual([]);
  });

  it('handles whitespace search query', async () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyTrack[] = [];
    await act(async () => {
      searchResults = await result.current.searchTracks('   ');
    });

    expect(searchResults).toEqual([]);
  });

  it('handles search errors', async () => {
    vi.mocked(matchApi.searchTracksForManualMatch).mockRejectedValueOnce(new Error('Search failed'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyTrack[] = [];
    await act(async () => {
      searchResults = await result.current.searchTracks('query');
    });

    expect(searchResults).toEqual([]);
    expect(result.current.error).toBe('Search failed');
  });

  it('applies manual match to a track', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: null },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    expect(result.current.matchedData?.tracks[0].match).toBeNull();

    const spotifyTrack: SpotifyTrack = {
      id: 'manual-id',
      name: 'Manual Track',
      artist: 'Manual Artist',
      album: 'Manual Album',
      uri: 'spotify:track:manual-id'
    };

    act(() => {
      result.current.applyManualMatch(0, spotifyTrack);
    });

    expect(result.current.matchedData?.tracks[0].match?.spotifyId).toBe('manual-id');
    expect(result.current.matchedData?.tracks[0].match?.confidence).toBe(100);
  });

  it('ignores applyManualMatch with invalid track index', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: null },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    const spotifyTrack: SpotifyTrack = {
      id: 'manual-id',
      name: 'Manual Track',
      artist: 'Manual Artist',
      album: 'Manual Album',
      uri: 'spotify:track:manual-id'
    };

    act(() => {
      result.current.applyManualMatch(99, spotifyTrack);
    });

    // Should remain unchanged
    expect(result.current.matchedData?.tracks[0].match).toBeNull();
  });

  it('ignores moveTrack with same from and to index', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
      { sourceTrack: buildNormalizedTrack('Track 2'), match: buildMatch('t2', 'Track 2', 'Artist 2') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1'), buildNormalizedTrack('Track 2')]);
    });

    act(() => {
      result.current.moveTrack(0, 0);
    });

    expect(result.current.matchedData?.tracks[0].sourceTrack.name).toBe('Track 1');
    expect(result.current.matchedData?.tracks[1].sourceTrack.name).toBe('Track 2');
  });
});
