import React from 'react';
import { Button, Box, Text, Flex } from '@radix-ui/themes';
import { useCreatePlaylist } from '../contexts/CreatePlaylistContext';
import { useWorkflow } from '../contexts/WorkflowContext';

export const PlaylistConfirmation: React.FC = () => {
  const { playlistUrl, playlistId, clearPlaylist } = useCreatePlaylist();
  const { resetWorkflow } = useWorkflow();

  if (!playlistUrl || !playlistId) {
    return null; // Only show if playlist was created
  }

  const handleOpenSpotify = () => {
    window.open(playlistUrl, '_blank');
  };

  const handleReset = () => {
    clearPlaylist();
    resetWorkflow();
  };

  return (
    <Box className="p-6 bg-gradient-to-r from-green-900/30 to-gray-900 border-2 border-green-600 rounded-lg">
      <Flex gap="4">
        <Box className="flex-shrink-0 pt-1">
          <svg
            className="w-6 h-6 text-green-400"
            fill="currentColor"
            viewBox="0 0 24 24"
          >
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
          </svg>
        </Box>

        <Box className="flex-1">
          <Text as="div" size="5" weight="bold" className="text-green-400 mb-2">
            Playlist Created Successfully!
          </Text>
          <Text as="div" color="gray" size="2" mb="4">
            Your playlist has been created on Spotify. Open it now to see your curated tracks.
          </Text>

          <Flex gap="3" mb="2" direction={{ initial: 'column', md: 'row' }}>
            <Button
              onClick={handleOpenSpotify}
              size="2"
              style={{ flex: 1, backgroundColor: '#16a34a' }}
            >
              <Flex align="center" gap="2">
                <svg
                  className="w-5 h-5"
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path d="M12 0C5.4 0 0 5.4 0 12s5.4 12 12 12 12-5.4 12-12S18.66 0 12 0zm5.521 17.34c-.24.359-.66.48-1.021.24-2.82-1.74-6.36-2.101-10.561-1.141-.418.122-.779-.179-.899-.539-.12-.421.18-.78.54-.9 4.56-1.021 8.52-.6 11.64 1.32.42.18.479.659.301 1.02zm1.44-3.3c-.301.42-.841.6-1.262.3-3.239-1.98-8.159-2.58-11.939-1.38-.479.12-1.02-.12-1.14-.6-.12-.48.12-1.021.6-1.141C9.6 9.9 15.079 10.561 18.18 12.84c.361.22.559.659.3 1.2zm.12-3.36C15.24 8.06 8.82 7.84 5.4 9.24c-.6.179-1.2-.181-1.38-.721-.18-.601.18-1.2.72-1.381 3.9-1.441 10.741-1.201 15.339 1.441.479.301.921.9.621 1.54-.301.621-.922.961-1.581.601z" />
                </svg>
                Open in Spotify
              </Flex>
            </Button>

            <button
              onClick={handleReset}
              className="px-4 py-2 text-gray-400 hover:text-gray-200 border border-gray-600 hover:border-gray-400 rounded transition-colors"
            >
              Create Another
            </button>
          </Flex>

          <Text as="div" size="1" color="gray" mt="3">
            Playlist ID: <code className="bg-gray-800 px-2 py-1 rounded text-yellow-400">{playlistId}</code>
          </Text>
        </Box>
      </Flex>
    </Box>
  );
};
