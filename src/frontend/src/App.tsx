import { Music2, Disc3, ListMusic, Radio } from 'lucide-react'
import { Button, Container, Flex, Heading, Text, Box, Card, Grid, Section } from '@radix-ui/themes'

function App() {
  return (
    <Box className="min-h-screen">
      {/* Navigation */}
      <Box className="border-b border-zinc-800">
        <Container size="4">
          <Flex justify="between" align="center" py="4">
            <Flex align="center" gap="2">
              <Music2 className="h-8 w-8 text-green-500" />
              <Heading size="6" className="bg-linear-to-r from-green-400 to-green-600 bg-clip-text text-transparent">
                Re:Play
              </Heading>
            </Flex>
            <Flex align="center" gap="4">
              <Button variant="ghost" size="2">
                About
              </Button>
              <Button size="2">
                Connect Spotify
              </Button>
            </Flex>
          </Flex>
        </Container>
      </Box>

      {/* Hero Section */}
      <Container size="4">
        <Section size="3">
          <Flex direction="column" align="center" gap="6" className="text-center max-w-4xl mx-auto">
            <Flex direction="column" gap="4">
              <Heading size="9" weight="bold">
                Replay Your Music History
              </Heading>
              <Text size="5" color="gray" className="max-w-2xl">
                Transform your listening data, collection, and concert memories into curated Spotify playlists
              </Text>
            </Flex>

            <Flex gap="4">
              <Button size="3">
                <Music2 className="h-5 w-5" />
                Get Started
              </Button>
              <Button size="3" variant="outline">
                Learn More
              </Button>
            </Flex>

            {/* Features Grid */}
            <Grid columns={{ initial: '1', md: '3' }} gap="4" width="100%" pt="8">
              <FeatureCard
                icon={<Radio className="h-8 w-8" />}
                title="Last.fm"
                description="Turn your listening history into playlists based on any time period"
              />
              <FeatureCard
                icon={<Disc3 className="h-8 w-8" />}
                title="Discogs"
                description="Convert your physical and digital collection into playable playlists"
              />
              <FeatureCard
                icon={<ListMusic className="h-8 w-8" />}
                title="Setlist.fm"
                description="Relive concerts by creating playlists from shows you've attended"
              />
            </Grid>

            {/* How It Works */}
            <Flex direction="column" gap="6" width="100%" pt="8">
              <Heading size="7" weight="bold">
                How It Works
              </Heading>
              <Grid columns={{ initial: '1', md: '4' }} gap="4" className="text-left">
                <StepCard
                  number="1"
                  title="Connect"
                  description="Link your Spotify account and choose a data source"
                />
                <StepCard
                  number="2"
                  title="Select"
                  description="Apply filters or use presets to find the music you want"
                />
                <StepCard
                  number="3"
                  title="Curate"
                  description="Review and refine matched tracks to your liking"
                />
                <StepCard
                  number="4"
                  title="Create"
                  description="Generate and save your playlist to Spotify"
                />
              </Grid>
            </Flex>
          </Flex>
        </Section>
      </Container>

      {/* Footer */}
      <Box className="border-t border-zinc-800 mt-24">
        <Container size="4">
          <Flex 
            direction={{ initial: 'column', md: 'row' }} 
            justify="between" 
            align="center" 
            gap="4" 
            py="6"
          >
            <Flex align="center" gap="2">
              <Music2 className="h-4 w-4" />
              <Text size="2" color="gray">
                Re:Play — Your music, replayed your way
              </Text>
            </Flex>
            <Flex gap="6">
              <Text size="2" asChild>
                <a href="#" className="hover:text-white transition-colors">
                  Privacy
                </a>
              </Text>
              <Text size="2" asChild>
                <a href="#" className="hover:text-white transition-colors">
                  Terms
                </a>
              </Text>
              <Text size="2" asChild>
                <a href="#" className="hover:text-white transition-colors">
                  GitHub
                </a>
              </Text>
            </Flex>
          </Flex>
        </Container>
      </Box>
    </Box>
  )
}

function FeatureCard({ icon, title, description }: { icon: React.ReactNode; title: string; description: string }) {
  return (
    <Card>
      <Flex direction="column" gap="3">
        <Box className="text-green-500">{icon}</Box>
        <Heading size="4" weight="medium">{title}</Heading>
        <Text size="2" color="gray">{description}</Text>
      </Flex>
    </Card>
  )
}

function StepCard({ number, title, description }: { number: string; title: string; description: string }) {
  return (
    <Flex direction="column" gap="2">
      <Flex 
        align="center" 
        justify="center" 
        className="h-8 w-8 rounded-full bg-green-500/10 text-green-500 font-bold"
      >
        {number}
      </Flex>
      <Heading size="3" weight="medium">{title}</Heading>
      <Text size="2" color="gray">{description}</Text>
    </Flex>
  )
}

export default App
