import { Button, Flex, Text } from '@radix-ui/themes';
import { CircleCheckIcon, LinkIcon } from 'lucide-react';
import { useData } from '@/contexts/DataContext';
import { useMatch } from '@/contexts/MatchContext';
import { useAuth } from '@/contexts/AuthContext';

export function MatchArtistsButton() {
  const { normalizedData } = useData();
  const { matchArtists, isLoading, error, clearError } = useMatch();
  const { isAuthenticated } = useAuth();

  const hasArtists = normalizedData?.artists && normalizedData.artists.length > 0;

  if (!hasArtists || !isAuthenticated) {
    return null;
  }

  const handleMatch = async () => {
    clearError();
    await matchArtists(normalizedData.artists);
  };

  return (
    <Flex direction="column" gap="2">
      <Button
        onClick={handleMatch}
        disabled={isLoading}
        variant="solid"
        color="green"
        size="3"
      >
        {isLoading ? (
          <Flex align="center" gap="2">
            <CircleCheckIcon className="animate-spin" size={16} />
            <Text>Matching to Spotify...</Text>
          </Flex>
        ) : (
          <Flex align="center" gap="2">
            <LinkIcon size={16} />
            <Text>Match to Spotify</Text>
          </Flex>
        )}
      </Button>

      {error && (
        <Text color="red" size="2">
          {error}
        </Text>
      )}
    </Flex>
  );
}
