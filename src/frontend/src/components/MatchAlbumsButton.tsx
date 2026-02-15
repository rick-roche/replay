import { Button, Flex, Text } from '@radix-ui/themes';
import { CircleCheckIcon, LinkIcon } from 'lucide-react';
import { useData } from '@/contexts/DataContext';
import { useMatch } from '@/contexts/MatchContext';
import { useAuth } from '@/contexts/AuthContext';

export function MatchAlbumsButton() {
  const { normalizedData } = useData();
  const { matchAlbums, isLoading, error, clearError } = useMatch();
  const { isAuthenticated } = useAuth();

  const hasAlbums = normalizedData?.albums && normalizedData.albums.length > 0;

  if (!hasAlbums || !isAuthenticated) {
    return null;
  }

  const handleMatch = async () => {
    clearError();
    await matchAlbums(normalizedData.albums);
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
