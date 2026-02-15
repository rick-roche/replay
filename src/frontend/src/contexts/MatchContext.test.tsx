import { renderHook, act, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MatchProvider, useMatch } from './MatchContext';
import { matchApi } from '@/api/match';
import type { components } from '@/api/generated-client';

type NormalizedTrack = components['schemas']['NormalizedTrack'];
type NormalizedAlbum = components['schemas']['NormalizedAlbum'];
type NormalizedArtist = components['schemas']['NormalizedArtist'];
type SpotifyTrack = components['schemas']['SpotifyTrack'];
type SpotifyAlbumInfo = components['schemas']['SpotifyAlbumInfo'];
type SpotifyArtistInfo = components['schemas']['SpotifyArtistInfo'];
type MatchedDataResponse = components['schemas']['MatchedDataResponse'];
type MatchedAlbumsResponse = components['schemas']['MatchedAlbumsResponse'];
type MatchedArtistsResponse = components['schemas']['MatchedArtistsResponse'];

vi.mock('@/api/match');

const buildNormalizedTrack = (name: string, artist = 'Artist'): NormalizedTrack => ({
  name,
  artist,
  album: 'Album',
  source: 'lastfm',
  sourceMetadata: {},
});

const buildNormalizedAlbum = (name: string, artist = 'Artist'): NormalizedAlbum => ({
  name,
  artist,
  source: 'lastfm',
  sourceMetadata: {},
});

const buildNormalizedArtist = (name: string): NormalizedArtist => ({
  name,
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

const buildAlbumMatch = (id: string, name: string, artist: string) => ({
  spotifyId: id,
  name,
  artist,
  releaseDate: '2020-01-01',
  uri: `spotify:album:${id}`,
  confidence: 85,
  method: 'Fuzzy' as const,
});

const buildArtistMatch = (id: string, name: string) => ({
  spotifyId: id,
  name,
  uri: `spotify:artist:${id}`,
  confidence: 95,
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

const responseWithAlbums = (
  albums: MatchedAlbumsResponse['albums']
): MatchedAlbumsResponse => ({
  albums,
});

const responseWithArtists = (
  artists: MatchedArtistsResponse['artists']
): MatchedArtistsResponse => ({
  artists,
});

describe('MatchContext', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('initializes with empty state', () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });
    expect(result.current.matchedData).toBeNull();
    expect(result.current.matchedAlbums).toBeNull();
    expect(result.current.matchedArtists).toBeNull();
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

  it('matches albums successfully', async () => {
    const album = buildNormalizedAlbum('Album 1', 'Artist 1');
    const apiResponse = responseWithAlbums([
      { sourceAlbum: album, match: buildAlbumMatch('a1', 'Album 1', 'Artist 1') },
    ]);

    vi.mocked(matchApi.matchAlbumsToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchAlbums([album]);
    });

    expect(result.current.matchedAlbums?.albums).toHaveLength(1);
  });

  it('matches artists successfully', async () => {
    const artist = buildNormalizedArtist('Artist 1');
    const apiResponse = responseWithArtists([
      { sourceArtist: artist, match: buildArtistMatch('ar1', 'Artist 1') },
    ]);

    vi.mocked(matchApi.matchArtistsToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchArtists([artist]);
    });

    expect(result.current.matchedArtists?.artists).toHaveLength(1);
  });

  it('handles match errors for tracks', async () => {
    vi.mocked(matchApi.matchTracksToSpotify).mockRejectedValue(new Error('boom'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    expect(result.current.matchedData).toBeNull();
    expect(result.current.error).toBe('boom');
  });

  it('handles match errors for albums', async () => {
    vi.mocked(matchApi.matchAlbumsToSpotify).mockRejectedValue(new Error('album error'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchAlbums([buildNormalizedAlbum('Album 1')]);
    });

    expect(result.current.matchedAlbums).toBeNull();
    expect(result.current.error).toBe('album error');
  });

  it('handles match errors for artists', async () => {
    vi.mocked(matchApi.matchArtistsToSpotify).mockRejectedValue(new Error('artist error'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchArtists([buildNormalizedArtist('Artist 1')]);
    });

    expect(result.current.matchedArtists).toBeNull();
    expect(result.current.error).toBe('artist error');
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
    expect(result.current.matchedAlbums).toBeNull();
    expect(result.current.matchedArtists).toBeNull();
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

  it('searches albums and handles failures', async () => {
    const searchResults: SpotifyAlbumInfo[] = [
      { id: 'a1', name: 'Found Album', artist: 'Artist', releaseDate: '2020-01-01', uri: 'spotify:album:a1' },
    ];

    vi.mocked(matchApi.searchAlbumsForManualMatch).mockResolvedValue(searchResults);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let results: SpotifyAlbumInfo[] = [];
    await act(async () => {
      results = await result.current.searchAlbums('query');
    });
    expect(results).toEqual(searchResults);

    vi.mocked(matchApi.searchAlbumsForManualMatch).mockRejectedValue(new Error('album search failed'));
    await act(async () => {
      results = await result.current.searchAlbums('query');
    });
    expect(results).toEqual([]);
    await waitFor(() => expect(result.current.error).toBe('album search failed'));
  });

  it('searches artists and handles failures', async () => {
    const searchResults: SpotifyArtistInfo[] = [
      { id: 'ar1', name: 'Found Artist', uri: 'spotify:artist:ar1' },
    ];

    vi.mocked(matchApi.searchArtistsForManualMatch).mockResolvedValue(searchResults);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let results: SpotifyArtistInfo[] = [];
    await act(async () => {
      results = await result.current.searchArtists('query');
    });
    expect(results).toEqual(searchResults);

    vi.mocked(matchApi.searchArtistsForManualMatch).mockRejectedValue(new Error('artist search failed'));
    await act(async () => {
      results = await result.current.searchArtists('query');
    });
    expect(results).toEqual([]);
    await waitFor(() => expect(result.current.error).toBe('artist search failed'));
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

  it('handles empty search query for tracks', async () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyTrack[] = [];
    await act(async () => {
      searchResults = await result.current.searchTracks('');
    });

    expect(searchResults).toEqual([]);
  });

  it('handles whitespace search query for tracks', async () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyTrack[] = [];
    await act(async () => {
      searchResults = await result.current.searchTracks('   ');
    });

    expect(searchResults).toEqual([]);
  });

  it('handles empty search query for albums', async () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyAlbumInfo[] = [];
    await act(async () => {
      searchResults = await result.current.searchAlbums('');
    });

    expect(searchResults).toEqual([]);
  });

  it('handles whitespace search query for albums', async () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyAlbumInfo[] = [];
    await act(async () => {
      searchResults = await result.current.searchAlbums('   ');
    });

    expect(searchResults).toEqual([]);
  });

  it('handles empty search query for artists', async () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyArtistInfo[] = [];
    await act(async () => {
      searchResults = await result.current.searchArtists('');
    });

    expect(searchResults).toEqual([]);
  });

  it('handles whitespace search query for artists', async () => {
    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyArtistInfo[] = [];
    await act(async () => {
      searchResults = await result.current.searchArtists('   ');
    });

    expect(searchResults).toEqual([]);
  });

  it('handles search errors for tracks', async () => {
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

  it('ignores removeTrack with invalid index', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    act(() => {
      result.current.removeTrack(99);
    });

    expect(result.current.matchedData?.tracks).toHaveLength(1);
  });

  it('ignores retryMatch with invalid track index', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: null },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    await act(async () => {
      await result.current.retryMatch(99);
    });

    expect(result.current.matchedData?.tracks[0].match).toBeNull();
  });

  it('handles appendMatches with empty array', async () => {
    const initialResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(initialResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    const initialLength = result.current.matchedData?.tracks.length ?? 0;

    await act(async () => {
      await result.current.appendMatches([]);
    });

    expect(result.current.matchedData?.tracks).toHaveLength(initialLength);
  });

  it('handles retryMatch error gracefully', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: null },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValueOnce(apiResponse);
    vi.mocked(matchApi.matchTracksToSpotify).mockRejectedValueOnce(new Error('Retry failed'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    await act(async () => {
      await result.current.retryMatch(0);
    });

    expect(result.current.error).toBe('Retry failed');
  });
  it('handles error objects without message property in search', async () => {
    vi.mocked(matchApi.searchTracksForManualMatch).mockRejectedValueOnce('Generic error');

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyTrack[] = [];
    await act(async () => {
      searchResults = await result.current.searchTracks('query');
    });

    expect(searchResults).toEqual([]);
    expect(result.current.error).toBe('Search failed');
  });

  it('handles album search generic errors', async () => {
    vi.mocked(matchApi.searchAlbumsForManualMatch).mockRejectedValueOnce('Network error');

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyAlbumInfo[] = [];
    await act(async () => {
      searchResults = await result.current.searchAlbums('query');
    });

    expect(searchResults).toEqual([]);
    expect(result.current.error).toBe('Search failed');
  });

  it('handles artist search generic errors', async () => {
    vi.mocked(matchApi.searchArtistsForManualMatch).mockRejectedValueOnce('API down');

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    let searchResults: SpotifyArtistInfo[] = [];
    await act(async () => {
      searchResults = await result.current.searchArtists('query');
    });

    expect(searchResults).toEqual([]);
    expect(result.current.error).toBe('Search failed');
  });

  it('handles generic error objects in track matching', async () => {
    vi.mocked(matchApi.matchTracksToSpotify).mockRejectedValueOnce({ code: 'ERROR' });

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    expect(result.current.error).toBe('Failed to match tracks');
  });

  it('handles generic error objects in album matching', async () => {
    vi.mocked(matchApi.matchAlbumsToSpotify).mockRejectedValueOnce({ code: 'ERROR' });

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchAlbums([buildNormalizedAlbum('Album 1')]);
    });

    expect(result.current.error).toBe('Failed to match albums');
  });

  it('handles generic error objects in artist matching', async () => {
    vi.mocked(matchApi.matchArtistsToSpotify).mockRejectedValueOnce({ code: 'ERROR' });

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchArtists([buildNormalizedArtist('Artist 1')]);
    });

    expect(result.current.error).toBe('Failed to match artists');
  });

  it('handles appendMatches error', async () => {
    const initialResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify)
      .mockResolvedValueOnce(initialResponse)
      .mockRejectedValueOnce(new Error('Append failed'));

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    await act(async () => {
      await result.current.appendMatches([buildNormalizedTrack('Track 2')]);
    });

    expect(result.current.error).toBe('Append failed');
  });

  it('handles error with generic object in appendMatches', async () => {
    const initialResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify)
      .mockResolvedValueOnce(initialResponse)
      .mockRejectedValueOnce({ message: 'error' });

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    await act(async () => {
      await result.current.appendMatches([buildNormalizedTrack('Track 2')]);
    });

    expect(result.current.error).toBe('Failed to match new tracks');
  });

  it('ignores moveTrack with invalid indices', async () => {
    const apiResponse = responseWithTracks([
      { sourceTrack: buildNormalizedTrack('Track 1'), match: buildMatch('t1', 'Track 1', 'Artist 1') },
    ]);
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue(apiResponse);

    const { result } = renderHook(() => useMatch(), { wrapper: MatchProvider });

    await act(async () => {
      await result.current.matchTracks([buildNormalizedTrack('Track 1')]);
    });

    act(() => {
      result.current.moveTrack(0, 99);
    });

    // Should handle gracefully
    expect(result.current.matchedData?.tracks).toBeDefined();
  });})
