import { useState } from 'react';
import { Box, Card, Flex, Heading, Text, TextField, TextArea, Switch, Section } from '@radix-ui/themes';
import { Settings, ChevronDown, ChevronUp } from 'lucide-react';
import { usePlaylist } from '@/contexts/PlaylistContext';

export function PlaylistConfigForm() {
  const { config, updateName, updateDescription, updateIsPublic } = usePlaylist();
  const [isExpanded, setIsExpanded] = useState(true);

  return (
    <Card>
      <Flex direction="column" gap="3">
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="flex items-center justify-between text-left hover:opacity-80 transition-opacity"
        >
          <Flex align="center" gap="2">
            <Settings size={20} className="text-green-500" />
            <Heading size="4" weight="medium">
              Configure Playlist Details
            </Heading>
          </Flex>
          {isExpanded ? (
            <ChevronUp size={20} />
          ) : (
            <ChevronDown size={20} />
          )}
        </button>

        {isExpanded && (
          <Section>
            <Flex direction="column" gap="4">
              {/* Playlist Name */}
              <Box>
                <label htmlFor="playlist-name" className="block text-sm font-medium mb-2">
                  Playlist Name
                </label>
                <TextField.Root
                  id="playlist-name"
                  placeholder="My Re:Play Playlist"
                  value={config.name}
                  onChange={(e) => updateName(e.target.value)}
                  className="w-full"
                />
                <Text size="1" color="gray" mt="2">
                  Give your playlist a memorable name
                </Text>
              </Box>

              {/* Playlist Description */}
              <Box>
                <label htmlFor="playlist-desc" className="block text-sm font-medium mb-2">
                  Description
                </label>
                <TextArea
                  id="playlist-desc"
                  placeholder="Created with Re:Play"
                  value={config.description}
                  onChange={(e) => {
                    // Strip line breaks to match Spotify's limitation
                    const sanitized = e.target.value.replace(/[\r\n]+/g, ' ');
                    updateDescription(sanitized);
                  }}
                  className="w-full min-h-20"
                />
                <Text size="1" color="gray" mt="2">
                  Describe what makes this playlist special (optional)
                </Text>
              </Box>

              {/* Public/Private Toggle */}
              <Box>
                <Flex align="center" justify="between" mb="2">
                  <label htmlFor="playlist-public" className="text-sm font-medium">
                    Visibility
                  </label>
                  <Flex align="center" gap="2">
                    <Switch
                      id="playlist-public"
                      checked={config.isPublic}
                      onCheckedChange={updateIsPublic}
                    />
                    <Text size="2">{config.isPublic ? 'Public' : 'Private'}</Text>
                  </Flex>
                </Flex>
                <Text size="1" color="gray">
                  {config.isPublic
                    ? 'Anyone can see and follow this playlist'
                    : 'Only you can see this playlist'}
                </Text>
              </Box>
            </Flex>
          </Section>
        )}
      </Flex>
    </Card>
  );
}
