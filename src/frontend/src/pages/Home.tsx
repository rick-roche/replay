import { useEffect, useRef } from 'react'
import { Music2, Disc3, ListMusic, Radio } from 'lucide-react'
import { Button, Container, Flex, Heading, Text, Box, Card, Grid, Section } from '@radix-ui/themes'
import { Link } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { useDataSource } from '../contexts/DataSourceContext'
import { useData } from '../contexts/DataContext'
import { useMatch } from '../contexts/MatchContext'
import { useConfig } from '../contexts/ConfigContext'
import { useWorkflow, WorkflowStep } from '../contexts/WorkflowContext'
import { DataSource } from '../types/datasource'
import { WorkflowStepper } from '../components/WorkflowStepper'
import { WorkflowStepContainer } from '../components/WorkflowStepContainer'
import { DataSourceSelector } from '../components/DataSourceSelector'
import { LastfmConfigForm } from '../components/LastfmConfigForm'
import { DiscogsConfigForm } from '../components/DiscogsConfigForm'
import { SetlistConfigForm } from '../components/SetlistConfigForm'
import { LastfmFilterForm } from '../components/LastfmFilterForm'
import { DiscogsFilterForm } from '../components/DiscogsFilterForm'
import { SetlistFmFilterForm } from '../components/SetlistFmFilterForm'
import { FetchDataButton, DataResults } from '../components/DataFetchForm'
import { MatchTracksButton } from '../components/MatchTracksButton'
import { MatchAlbumsButton } from '../components/MatchAlbumsButton'
import { MatchArtistsButton } from '../components/MatchArtistsButton'
import { MatchResults } from '../components/MatchResults'
import { PlaylistConfigForm } from '../components/PlaylistConfigForm'
import { CreatePlaylistButton } from '../components/CreatePlaylistButton'
import { PlaylistConfirmation } from '../components/PlaylistConfirmation'
import { AutoFetcher } from '../components/AutoFetcher'
import { AdvancedOptions } from '../components/AdvancedOptions'

export function Home() {
  const { user, isAuthenticated, login } = useAuth()
  const { selectedSource } = useDataSource()
  const { matchedData, matchedAlbums, matchedArtists, clearMatches } = useMatch()
  const { clearData } = useData()
  const clearDataRef = useRef(clearData)
  const { autoFetch } = useConfig()
  const { markStepComplete, nextStep, currentStep } = useWorkflow()
  const hasAutoAdvancedRef = useRef(false)

  // Keep a stable reference to the latest clearData implementation
  useEffect(() => {
    clearDataRef.current = clearData
  }, [clearData])

  // Clear matched and normalized data when navigating away from fetch+match
  // so they don't persist when reconfiguring and fetching again
  useEffect(() => {
    if (currentStep !== WorkflowStep.FETCH_AND_MATCH && currentStep !== WorkflowStep.CURATE && currentStep !== WorkflowStep.CREATE) {
      clearMatches()
      clearDataRef.current()
    }
  }, [currentStep, clearMatches])

  // Auto-advance to curate when fetch+match completes with results for the first time
  useEffect(() => {
    const hasResults =
      (matchedData && matchedData.tracks && matchedData.tracks.length > 0) ||
      (matchedAlbums && matchedAlbums.albums && matchedAlbums.albums.length > 0) ||
      (matchedArtists && matchedArtists.artists && matchedArtists.artists.length > 0)

    if (hasResults && currentStep === WorkflowStep.FETCH_AND_MATCH && !hasAutoAdvancedRef.current) {
      hasAutoAdvancedRef.current = true
      markStepComplete(WorkflowStep.FETCH_AND_MATCH)
      nextStep()
    }

    // Reset the ref if we go back to fetch+match step with no results
    if (!hasResults && currentStep === WorkflowStep.FETCH_AND_MATCH) {
      hasAutoAdvancedRef.current = false
    }
  }, [matchedData, matchedAlbums, matchedArtists, currentStep, markStepComplete, nextStep])

  // Handle workflow progression
  const handleSourceSelected = () => {
    if (selectedSource) {
      markStepComplete(WorkflowStep.SELECT_SOURCE)
      nextStep()
    }
  }

  const handleConfigured = () => {
    markStepComplete(WorkflowStep.CONFIGURE)
    nextStep()
  }

  const handleFetchedAndMatched = () => {
    if (matchedData && matchedData.tracks && matchedData.tracks.length > 0) {
      markStepComplete(WorkflowStep.FETCH_AND_MATCH)
      nextStep()
    }
  }

  const handleCurated = () => {
    markStepComplete(WorkflowStep.CURATE)
    nextStep()
  }

  return (
    <Container size="4">
      <Section size="3">
        <Flex direction="column" align="center" gap="6" className="text-center max-w-4xl mx-auto">
          {isAuthenticated && user ? (
            <Flex direction="column" gap="6" width="100%">
              <Box>
                <Heading size="8" weight="bold" mb="2">
                  Welcome back, {user.displayName}!
                </Heading>
                <Text size="4" color="gray">
                  Ready to create some playlists from your music history?
                </Text>
              </Box>

              {/* Workflow Stepper */}
              <WorkflowStepper />

              {/* Step 1: Select Source */}
              <WorkflowStepContainer
                step={WorkflowStep.SELECT_SOURCE}
                title="Select Data Source"
                onComplete={handleSourceSelected}
                canComplete={selectedSource !== null}
              >
                <DataSourceSelector />
              </WorkflowStepContainer>

              {/* Step 2: Configure */}
              {selectedSource && (
                <WorkflowStepContainer
                  step={WorkflowStep.CONFIGURE}
                  title="Configure"
                  onComplete={handleConfigured}
                  canComplete={true}
                >
                  <Flex direction="column" gap="4">
                    {selectedSource === DataSource.LASTFM && (
                      <>
                        <LastfmConfigForm />
                        <LastfmFilterForm />
                      </>
                    )}
                    {selectedSource === DataSource.DISCOGS && (
                      <>
                        <DiscogsConfigForm />
                        <DiscogsFilterForm />
                      </>
                    )}
                    {selectedSource === DataSource.SETLISTFM && (
                      <>
                        <SetlistConfigForm />
                        <SetlistFmFilterForm />
                      </>
                    )}
                    <AdvancedOptions />
                  </Flex>
                </WorkflowStepContainer>
              )}

              {/* Step 3: Fetch & Match */}
              {selectedSource && (
                <WorkflowStepContainer
                  step={WorkflowStep.FETCH_AND_MATCH}
                  title="Fetch & Match"
                  onComplete={handleFetchedAndMatched}
                  canComplete={
                    (matchedData !== null && (matchedData.tracks?.length ?? 0) > 0) ||
                    (matchedAlbums !== null && (matchedAlbums.albums?.length ?? 0) > 0) ||
                    (matchedArtists !== null && (matchedArtists.artists?.length ?? 0) > 0)
                  }
                >
                  <Flex direction="column" gap="4">
                    {selectedSource === DataSource.LASTFM && (
                      <>
                        {autoFetch ? (
                          <>
                            <AutoFetcher />
                            <DataResults />
                            <MatchResults />
                          </>
                        ) : (
                          <>
                            <FetchDataButton />
                            <DataResults />
                            <MatchTracksButton />
                            <MatchAlbumsButton />
                            <MatchArtistsButton />
                            <MatchResults />
                          </>
                        )}
                      </>
                    )}
                    {selectedSource === DataSource.SETLISTFM && (
                      <>
                        {autoFetch ? (
                          <>
                            <AutoFetcher />
                            <DataResults />
                            <MatchResults />
                          </>
                        ) : (
                          <>
                            <FetchDataButton />
                            <DataResults />
                            <MatchTracksButton />
                            <MatchAlbumsButton />
                            <MatchArtistsButton />
                            <MatchResults />
                          </>
                        )}
                      </>
                    )}
                    {selectedSource === DataSource.DISCOGS && (
                      <>
                        {autoFetch ? (
                          <>
                            <AutoFetcher />
                            <DataResults />
                            <MatchResults />
                          </>
                        ) : (
                          <>
                            <FetchDataButton />
                            <DataResults />
                            <MatchTracksButton />
                            <MatchAlbumsButton />
                            <MatchArtistsButton />
                            <MatchResults />
                          </>
                        )}
                      </>
                    )}
                  </Flex>
                </WorkflowStepContainer>
              )}

              {/* Step 4: Curate */}
              {(matchedData || matchedAlbums || matchedArtists) && (
                <WorkflowStepContainer
                  step={WorkflowStep.CURATE}
                  title="Curate Playlist"
                  onComplete={handleCurated}
                  canComplete={true}
                >
                  <Flex direction="column" gap="4">
                    <MatchResults />
                  </Flex>
                </WorkflowStepContainer>
              )}

              {/* Step 5: Create */}
              {(matchedData || matchedAlbums || matchedArtists) && (
                <WorkflowStepContainer
                  step={WorkflowStep.CREATE}
                  title="Create Playlist"
                >
                  <Flex direction="column" gap="4">
                    <PlaylistConfigForm />
                    <CreatePlaylistButton />
                    <PlaylistConfirmation />
                  </Flex>
                </WorkflowStepContainer>
              )}
            </Flex>
          ) : (
            <>
              <Flex direction="column" gap="4">
                <Heading size="9" weight="bold">
                  Replay Your Music History
                </Heading>
                <Text size="5" color="gray" className="max-w-2xl">
                  Transform your listening data, collection, and concert memories into curated Spotify playlists
                </Text>
              </Flex>

              <Flex gap="4">
                <Button size="3" onClick={login}>
                  <Music2 className="h-5 w-5" />
                  Get Started
                </Button>
                <Button size="3" variant="outline" asChild>
                  <Link to="/about">Learn More</Link>
                </Button>
              </Flex>
            </>
          )}

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
