import React, { useState } from 'react';
import { Button, Box, Flex, Text } from '@radix-ui/themes';
import { useCreatePlaylist } from '../contexts/CreatePlaylistContext';
import { usePlaylist } from '../contexts/PlaylistContext';
import { useMatch } from '../contexts/MatchContext';

export const CreatePlaylistButton: React.FC = () => {
  const { createPlaylist, isCreating, error } = useCreatePlaylist();
  const { config } = usePlaylist();
  const { matchedData } = useMatch();
  const [localError, setLocalError] = useState<string | null>(null);

  if (!matchedData?.tracks || matchedData.tracks.length === 0) {
    return null; // Don't show button if no matched tracks
  }

  const handleCreatePlaylist = async () => {
    setLocalError(null);

    // Validate required fields
    if (!config.name?.trim()) {
      setLocalError('Playlist name is required');
      return;
    }

    // Get only matched track URIs
    const trackUris = matchedData.tracks
      .filter(track => track.match) // Only matched tracks
      .map(track => track.match!.uri);

    if (trackUris.length === 0) {
      setLocalError('No matched tracks to add to playlist');
      return;
    }

    try {
      await createPlaylist(trackUris, config.name, config.description, config.isPublic);
    } catch {
      // Error is already set in the context
    }
  };

  const displayError = error || localError;
  const matchedCount = matchedData.tracks.filter(t => t.match).length;

  return (
    <Box className="space-y-4">
      <Box className="p-4 bg-gray-900 border border-gray-800 rounded-lg">
        <Text as="div" size="5" weight="bold" mb="2">
          Create Playlist
        </Text>
        <Text as="div" color="gray" size="2" mb="4">
          Ready to create a playlist with {matchedCount} matched tracks on Spotify?
        </Text>

        {displayError && (
          <Box className="mb-4 p-3 bg-red-900/30 border border-red-700 rounded">
            <Text color="red" size="1">
              {displayError}
            </Text>
          </Box>
        )}

        <Button
          onClick={handleCreatePlaylist}
          disabled={isCreating || matchedCount === 0}
          className="w-full bg-green-600 hover:bg-green-700 disabled:bg-gray-700"
          style={{
            width: '100%',
            backgroundColor: isCreating || matchedCount === 0 ? '#374151' : '#16a34a',
          }}
        >
          {isCreating ? (
            <Flex align="center" gap="2">
              <svg
                className="w-4 h-4 animate-spin"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                />
              </svg>
              Creating Playlist...
            </Flex>
          ) : (
            <Flex align="center" gap="2">
              <svg
                className="w-5 h-5"
                fill="currentColor"
                viewBox="0 0 24 24"
              >
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm3.5-9c.83 0 1.5-.67 1.5-1.5S16.33 8 15.5 8 14 8.67 14 9.5s.67 1.5 1.5 1.5zm-7 0c.83 0 1.5-.67 1.5-1.5S9.33 8 8.5 8 7 8.67 7 9.5 7.67 11 8.5 11zm3.5 6.5c2.33 0 4.31-1.46 5.11-3.5H6.89c.8 2.04 2.78 3.5 5.11 3.5z" />
              </svg>
              Create Playlist on Spotify
            </Flex>
          )}
        </Button>
      </Box>
    </Box>
  );
};
