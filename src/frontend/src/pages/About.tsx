import { Music2, Github } from 'lucide-react'
import { Container, Flex, Heading, Text, Box, Card, Button, Section } from '@radix-ui/themes'
import packageJson from '../../package.json'

export function About() {
  return (
    <Container size="4">
      <Section size="3">
        <Flex direction="column" align="center" gap="4" className="max-w-3xl mx-auto">
          {/* Hero */}
          <Flex direction="column" align="center" gap="3" className="text-center">
            <Flex align="center" gap="3">
              <Music2 className="h-12 w-12 text-green-500" />
              <Heading size="9" weight="bold" className="bg-linear-to-r from-green-400 to-green-600 bg-clip-text text-transparent">
                Re:Play
              </Heading>
            </Flex>
            <Text size="5" color="gray">
              Your music, replayed your way
            </Text>
          </Flex>

          {/* Main Content */}
          <Card size="3">
            <Flex direction="column" gap="4">
              {/* About Section */}
              <Flex direction="column" gap="2">
                <Heading size="5" weight="bold">About Re:Play</Heading>
                <Text size="3" color="gray">
                  Re:Play transforms your music history into curated Spotify playlists. 
                  Connect your Last.fm listening data, Discogs collection, or Setlist.fm concert history 
                  to create personalized playlists that celebrate your musical journey.
                </Text>
                <Text size="3" color="gray">
                  Whether you want to revisit your favorite albums from a specific year, 
                  replay the setlist from a concert you attended, or rediscover artists 
                  you once loved, Re:Play makes it effortless.
                </Text>
              </Flex>

              {/* Version Info */}
              <Flex direction="column" gap="1">
                <Heading size="4" weight="medium">Version</Heading>
                <Text size="3" color="gray">
                  {packageJson.version}
                </Text>
              </Flex>

              {/* GitHub Link */}
              <Flex direction="column" gap="2">
                <Heading size="4" weight="medium">Open Source</Heading>
                <Text size="3" color="gray">
                  Re:Play is open source and available on GitHub. 
                  Contributions, issues, and feedback are welcome!
                </Text>
                <Box>
                  <Button asChild>
                    <a 
                      href="https://github.com/rick-roche/replay" 
                      target="_blank" 
                      rel="noopener noreferrer"
                    >
                      <Github className="h-4 w-4" />
                      View on GitHub
                    </a>
                  </Button>
                </Box>
              </Flex>

              {/* Features */}
              <Flex direction="column" gap="2">
                <Heading size="4" weight="medium">Features</Heading>
                <Flex direction="column" gap="2" asChild>
                  <ul className="list-disc list-inside space-y-2">
                    <li>
                      <Text size="3" color="gray">
                        Connect Last.fm to create playlists from your listening history
                      </Text>
                    </li>
                    <li>
                      <Text size="3" color="gray">
                        Import your Discogs collection and turn it into playable playlists
                      </Text>
                    </li>
                    <li>
                      <Text size="3" color="gray">
                        Relive concerts by creating playlists from Setlist.fm data
                      </Text>
                    </li>
                    <li>
                      <Text size="3" color="gray">
                        Advanced filtering and matching to Spotify's catalog
                      </Text>
                    </li>
                    <li>
                      <Text size="3" color="gray">
                        Customize playlist names, descriptions, and track selection
                      </Text>
                    </li>
                  </ul>
                </Flex>
              </Flex>
            </Flex>
          </Card>
        </Flex>
      </Section>
    </Container>
  )
}
