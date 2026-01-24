import { Badge, Box, Card, Flex, Heading, Separator, Text } from '@radix-ui/themes';
import { AlertTriangleIcon, CheckCircle2Icon, LinkIcon } from 'lucide-react';
import { useMatch } from '@/contexts/MatchContext';
import type { components } from '@/api/generated-client';

type MatchedTrack = components['schemas']['MatchedTrack']
type MatchMethod = 'Exact' | 'Normalized' | 'Fuzzy' | 'AlbumBased'

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

const TrackRow = ({ matchedTrack }: { matchedTrack: MatchedTrack }) => {
  const { sourceTrack, match, isMatched } = matchedTrack;

  return (
    <Box>
      <Flex direction="column" gap="1" py="2">
        <Flex align="center" gap="2">
          {isMatched ? (
            <CheckCircle2Icon size={16} color="var(--green-9)" />
          ) : (
            <AlertTriangleIcon size={16} color="var(--red-9)" />
          )}
          <Text weight="medium" size="2">
            {sourceTrack.name}
          </Text>
          <Text size="2" color="gray">
            by {sourceTrack.artist}
          </Text>
        </Flex>

        {isMatched && match ? (
          <Flex gap="2" align="center" ml="6">
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
      </Flex>
      <Separator size="4" />
    </Box>
  );
};

export function MatchResults() {
  const { matchedData, isLoading } = useMatch();

  if (isLoading) {
    return (
      <Card>
        <Flex direction="column" gap="3">
          <Heading size="4">Matching tracks...</Heading>
          <Text size="2" color="gray">
            Please wait while we find your tracks on Spotify
          </Text>
        </Flex>
      </Card>
    );
  }

  if (!matchedData) {
    return null;
  }

  const { tracks, matchedCount, unmatchedCount, totalTracks } = matchedData;
  const matchedCountNum = typeof matchedCount === 'string' ? parseInt(matchedCount, 10) : matchedCount ?? 0;
  const unmatchedCountNum = typeof unmatchedCount === 'string' ? parseInt(unmatchedCount, 10) : unmatchedCount ?? 0;
  const totalTracksNum = typeof totalTracks === 'string' ? parseInt(totalTracks, 10) : totalTracks ?? 0;

  return (
    <Card>
      <Flex direction="column" gap="3">
        <Flex direction="column" gap="2">
          <Flex align="center" gap="2">
            <CheckCircle2Icon size={20} color="var(--green-9)" />
            <Heading size="5">Matching Complete</Heading>
          </Flex>
          <Flex gap="4">
            <Flex align="center" gap="2">
              <Text size="2" weight="bold">
                Total:
              </Text>
              <Badge color="gray" variant="soft">
                {totalTracksNum}
              </Badge>
            </Flex>
            <Flex align="center" gap="2">
              <Text size="2" weight="bold">
                Matched:
              </Text>
              <Badge color="green" variant="soft">
                {matchedCountNum}
              </Badge>
            </Flex>
            {unmatchedCountNum > 0 && (
              <Flex align="center" gap="2">
                <Text size="2" weight="bold">
                  Unmatched:
                </Text>
                <Badge color="red" variant="soft">
                  {unmatchedCountNum}
                </Badge>
              </Flex>
            )}
          </Flex>
        </Flex>

        <Separator size="4" />

        <Box>
          {(tracks ?? []).slice(0, 20).map((track, index) => (
            <TrackRow key={index} matchedTrack={track} />
          ))}
          {(tracks?.length ?? 0) > 20 && (
            <Text size="2" color="gray" mt="2">
              ...and {(tracks?.length ?? 0) - 20} more tracks
            </Text>
          )}
        </Box>
      </Flex>
    </Card>
  );
}
