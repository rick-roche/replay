import { useState } from 'react';
import { Badge, Box, Card, Flex, Heading, Separator, Text } from '@radix-ui/themes';
import { AlertTriangleIcon, CheckCircle2Icon, GripVertical, LinkIcon, Loader2, RotateCcw, Search, Trash2 } from 'lucide-react';
import { useMatch } from '@/contexts/MatchContext';
import { ManualSearchModal } from './ManualSearchModal';
import type { components } from '@/api/generated-client';

type MatchedTrack = components['schemas']['MatchedTrack']
type MatchedAlbum = components['schemas']['MatchedAlbum']
type MatchedArtist = components['schemas']['MatchedArtist']
type SpotifyMatch = components['schemas']['SpotifyMatch']
type NormalizedTrack = components['schemas']['NormalizedTrack']
type MatchMethod = 'Exact' | 'Normalized' | 'Fuzzy' | 'AlbumBased'
type SpotifyTrack = components['schemas']['SpotifyTrack']

const SourceInfo = ({
  source,
  album,
  metadata,
}: {
  source: string;
  album?: string | null;
  metadata?: Record<string, unknown> | null;
}) => {
  const items: string[] = [];

  // Known metadata keys across sources
  const playCount = typeof metadata?.playCount === 'number' ? metadata?.playCount :
    (typeof metadata?.playCount === 'string' ? parseInt(metadata.playCount as string, 10) : undefined);
  const concertDate = typeof metadata?.concertDate === 'string' ? (metadata.concertDate as string) : undefined;
  const venue = typeof metadata?.venue === 'string' ? (metadata.venue as string) : undefined;
  const year = typeof metadata?.year === 'number' ? (metadata.year as number) :
    (typeof metadata?.year === 'string' ? parseInt(metadata.year as string, 10) : undefined);
  const format = typeof metadata?.format === 'string' ? (metadata.format as string) : undefined;
  const added = typeof metadata?.added === 'string' ? (metadata.added as string) : undefined;

  if (album) items.push(`Album: ${album}`);
  if (playCount !== undefined && !Number.isNaN(playCount)) items.push(`Play count: ${playCount}`);
  if (concertDate) items.push(`Concert: ${concertDate}${venue ? ` @ ${venue}` : ''}`);
  if (year) items.push(`Year: ${year}`);
  if (format) items.push(`Format: ${format}`);
  if (added) items.push(`Added: ${added}`);

  if (items.length === 0) return null;

  return (
    <Flex gap="2" align="center" ml="6">
      <Badge color="gray" variant="soft" size="1">
        {source}
      </Badge>
      <Text size="1" color="gray">
        {items.join(' · ')}
      </Text>
    </Flex>
  );
};

const MatchMethodBadge = ({ method }: { method: MatchMethod }) => {
  const colors: Record<MatchMethod, 'green' | 'blue' | 'cyan' | 'orange'> = {
    Exact: 'green',
    Normalized: 'blue',
    AlbumBased: 'cyan',
    Fuzzy: 'orange',
  };

  return (
    <Badge color={colors[method]} variant="soft" size="1">
      {method}
    </Badge>
  );
};

const ConfidenceBadge = ({ confidence }: { confidence: number | string }) => {
  const conf = typeof confidence === 'string' ? parseInt(confidence, 10) : confidence;
  
  const getColor = (c: number) => {
    if (c >= 90) return 'green' as const;
    if (c >= 70) return 'orange' as const;
    return 'red' as const;
  };

  return (
    <Badge color={getColor(conf)} variant="soft" size="1">
      {conf}%
    </Badge>
  );
};

interface UnmatchedTrackRowProps {
  trackIndex: number;
  matchedTrack: MatchedTrack;
}

const UnmatchedTrackRow = ({ trackIndex, matchedTrack }: UnmatchedTrackRowProps) => {
  const { sourceTrack } = matchedTrack;
  const { retryMatch, removeTrack, applyManualMatch, searchTracks } = useMatch();
  const [searchModalOpen, setSearchModalOpen] = useState(false);
  const [isRetrying, setIsRetrying] = useState(false);

  const handleRetry = async () => {
    setIsRetrying(true);
    try {
      await retryMatch(trackIndex);
    } finally {
      setIsRetrying(false);
    }
  };

  const handleManualSearch = (result: SpotifyTrack) => {
    applyManualMatch(trackIndex, result);
  };

  return (
    <>
      <Box>
        <Flex direction="column" gap="2" py="2">
          <Flex align="center" gap="2">
            <AlertTriangleIcon size={16} color="var(--red-9)" />
            <Text weight="medium" size="2">
              {sourceTrack.name}
            </Text>
            <Text size="2" color="gray">
              by {sourceTrack.artist}
            </Text>
          </Flex>
          <SourceInfo
            source={String(sourceTrack.source)}
            album={sourceTrack.album ?? null}
            metadata={(sourceTrack as unknown as { sourceMetadata?: Record<string, unknown> }).sourceMetadata ?? null}
          />
          <Flex ml="6" gap="2" align="center">
            <button
              onClick={handleRetry}
              disabled={isRetrying}
              className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-blue-400 hover:bg-blue-900/20 disabled:opacity-50"
              title="Try matching again with different strategies"
            >
              {isRetrying ? (
                <Loader2 size={14} className="animate-spin" />
              ) : (
                <RotateCcw size={14} />
              )}
              Retry
            </button>
            <button
              onClick={() => setSearchModalOpen(true)}
              className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-green-400 hover:bg-green-900/20"
              title="Search manually on Spotify"
            >
              <Search size={14} />
              Search
            </button>
            <button
              onClick={() => removeTrack(trackIndex)}
              className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-red-400 hover:bg-red-900/20"
              title="Remove this track"
            >
              <Trash2 size={14} />
              Remove
            </button>
          </Flex>
        </Flex>
        <Separator size="4" />
      </Box>

      <ManualSearchModal
        open={searchModalOpen}
        onClose={() => setSearchModalOpen(false)}
        onSelect={handleManualSearch}
        onSearch={searchTracks}
        initialQuery={`${sourceTrack.name} ${sourceTrack.artist}`.trim()}
      />
    </>
  );
};

const MatchedTrackRow = ({ matchedTrack, trackIndex }: { matchedTrack: MatchedTrack; trackIndex: number }) => {
  const { sourceTrack, match } = matchedTrack;

  const { removeTrack } = useMatch();

  return (
    <Box>
      <Flex direction="column" gap="2" py="2">
        <Flex align="center" gap="2">
          <CheckCircle2Icon size={16} color="var(--green-9)" />
          <Text weight="medium" size="2">
            {sourceTrack.name}
          </Text>
          <Text size="2" color="gray">
            by {sourceTrack.artist}
          </Text>
        </Flex>

        {match ? (
          <Flex gap="2" align="center" ml="6" wrap="wrap">
            <LinkIcon size={12} color="var(--gray-9)" />
            <Text size="1" color="gray">
              Matched: {match.name} by {match.artist}
            </Text>
            <MatchMethodBadge method={match.method as MatchMethod} />
            <ConfidenceBadge confidence={match.confidence} />
          </Flex>
        ) : (
          <Flex ml="6">
            <Text size="1" color="red">
              No match found
            </Text>
          </Flex>
        )}

        <SourceInfo
          source={String(sourceTrack.source)}
          album={sourceTrack.album ?? null}
          metadata={(sourceTrack as unknown as { sourceMetadata?: Record<string, unknown> }).sourceMetadata ?? null}
        />

        <Flex ml="6" gap="2" align="center">
          <button
            onClick={() => removeTrack(trackIndex)}
            className="inline-flex items-center gap-1 rounded px-2 py-1 text-xs font-medium text-red-400 hover:bg-red-900/20"
            title="Remove this track"
          >
            <Trash2 size={14} />
            Remove
          </button>
        </Flex>
      </Flex>
      <Separator size="4" />
    </Box>
  );
};

export function MatchResults() {
  const { matchedData, matchedAlbums, matchedArtists, isLoading, moveTrack } = useMatch();
  const [draggingIndex, setDraggingIndex] = useState<number | null>(null);

  const handleDrop = (toIndex: number) => {
    if (draggingIndex === null) return;
    moveTrack(draggingIndex, toIndex);
    setDraggingIndex(null);
  };

  if (isLoading) {
    return (
      <Card>
        <Flex direction="column" gap="3">
          <Heading size="4">Matching...</Heading>
          <Text size="2" color="gray">
            Please wait while we match your data to Spotify
          </Text>
        </Flex>
      </Card>
    );
  }

  // Extract tracks from matched data - could be from tracks, albums, or artists
  let tracks: MatchedTrack[] = [];
  let totalMatchedTracks = 0;
  let totalUnmatchedItems = 0;
  let matchName = 'Matching Complete';

  if (matchedData?.tracks) {
    // Direct track matches
    tracks = matchedData.tracks;
    totalMatchedTracks = typeof matchedData.matchedCount === 'string' 
      ? parseInt(matchedData.matchedCount, 10) 
      : matchedData.matchedCount ?? 0;
    totalUnmatchedItems = typeof matchedData.unmatchedCount === 'string'
      ? parseInt(matchedData.unmatchedCount, 10)
      : matchedData.unmatchedCount ?? 0;
    matchName = 'Tracks Matched';
  } else if (matchedAlbums?.albums) {
    // Extract tracks from matched albums
    matchedAlbums.albums.forEach((album: MatchedAlbum) => {
      if (album.match?.tracks) {
        album.match.tracks.forEach((track: SpotifyMatch) => {
          const sourceTrack: NormalizedTrack = {
            name: track.name,
            artist: track.artist,
            album: album.match?.name || undefined,
            source: 'album',
            sourceMetadata: {},
          };
          tracks.push({
            sourceTrack,
            match: track,
            isMatched: true,
          } as MatchedTrack);
        });
      }
    });
    totalMatchedTracks = typeof matchedAlbums.matchedCount === 'string'
      ? parseInt(matchedAlbums.matchedCount, 10)
      : matchedAlbums.matchedCount ?? 0;
    totalUnmatchedItems = typeof matchedAlbums.unmatchedCount === 'string'
      ? parseInt(matchedAlbums.unmatchedCount, 10)
      : matchedAlbums.unmatchedCount ?? 0;
    matchName = 'Albums Matched';
  } else if (matchedArtists?.artists) {
    // Extract tracks from matched artists
    matchedArtists.artists.forEach((artist: MatchedArtist) => {
      if (artist.match?.topTracks) {
        artist.match.topTracks.forEach((track: SpotifyMatch) => {
          const sourceTrack: NormalizedTrack = {
            name: track.name,
            artist: track.artist,
            source: 'artist',
            sourceMetadata: {},
          };
          tracks.push({
            sourceTrack,
            match: track,
            isMatched: true,
          } as MatchedTrack);
        });
      }
    });
    totalMatchedTracks = typeof matchedArtists.matchedCount === 'string'
      ? parseInt(matchedArtists.matchedCount, 10)
      : matchedArtists.matchedCount ?? 0;
    totalUnmatchedItems = typeof matchedArtists.unmatchedCount === 'string'
      ? parseInt(matchedArtists.unmatchedCount, 10)
      : matchedArtists.unmatchedCount ?? 0;
    matchName = 'Artists Matched';
  }

  if (tracks.length === 0) {
    return null;
  }

  const totalTracksNum = tracks.length;

  return (
    <Card>
      <Flex direction="column" gap="3">
        <Flex direction="column" gap="2">
          <Flex align="center" gap="2">
            <CheckCircle2Icon size={20} color="var(--green-9)" />
            <Heading size="5">{matchName}</Heading>
          </Flex>
          <Flex gap="4">
            <Flex align="center" gap="2">
              <Text size="2" weight="bold">
                Total Tracks:
              </Text>
              <Badge color="gray" variant="soft">
                {totalTracksNum}
              </Badge>
            </Flex>
            <Flex align="center" gap="2">
              <Text size="2" weight="bold">
                Matched Items:
              </Text>
              <Badge color="green" variant="soft">
                {totalMatchedTracks}
              </Badge>
            </Flex>
            {totalUnmatchedItems > 0 && (
              <Flex align="center" gap="2">
                <Text size="2" weight="bold">
                  Unmatched:
                </Text>
                <Badge color="red" variant="soft">
                  {totalUnmatchedItems}
                </Badge>
              </Flex>
            )}
          </Flex>
        </Flex>

        <Separator size="4" />

        <Box>
          <Text size="1" color="gray" mb="2">
            Drag the handle to reorder tracks. Remove any you do not want in the playlist.
          </Text>
          {(tracks ?? []).map((track, index) => {
            const isDragging = draggingIndex === index;

            return (
              <Box
                key={index}
                draggable
                onDragStart={() => setDraggingIndex(index)}
                onDragOver={(e) => e.preventDefault()}
                onDrop={() => handleDrop(index)}
                onDragEnd={() => setDraggingIndex(null)}
                className={`rounded-md ${isDragging ? 'bg-green-900/10 border border-green-700' : ''}`}
              >
                <Flex gap="3" align="start" py="1" px="1">
                  <span className="flex items-center text-gray-500 cursor-grab" aria-hidden>
                    <GripVertical size={16} />
                  </span>
                  <Box style={{ flex: 1 }}>
                    {track.match ? (
                      <MatchedTrackRow matchedTrack={track} trackIndex={index} />
                    ) : (
                      <UnmatchedTrackRow trackIndex={index} matchedTrack={track} />
                    )}
                  </Box>
                </Flex>
              </Box>
            );
          })}
        </Box>
      </Flex>
    </Card>
  );
}
