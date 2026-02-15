import { useEffect, useRef } from 'react'
import { Card, Heading, Flex, Box, Text, Spinner } from '@radix-ui/themes'
import { AlertCircle, CheckCircle2 } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'
import { useData } from '../contexts/DataContext'
import { useMatch } from '../contexts/MatchContext'
import { useDataSource } from '../contexts/DataSourceContext'
import { DataSource } from '../types/datasource'

export function AutoFetcher() {
  const { lastfmConfig, lastfmFilter, discogsConfig, discogsFilter, setlistConfig, setlistFmFilter } = useConfig()
  const { isLoading: isFetchLoading, error: fetchError, fetchData, fetchSetlistFmData, fetchDiscogsData, normalizedData } = useData()
  const { isLoading: isMatchLoading, error: matchError, matchTracks, matchAlbums, matchArtists } = useMatch()
  const { selectedSource } = useDataSource()
  const hasTriggeredFetch = useRef(false)
  const hasTriggeredMatch = useRef(false)

  // Trigger fetch on mount if configured
  useEffect(() => {
    if (
      !hasTriggeredFetch.current &&
      selectedSource === DataSource.LASTFM &&
      lastfmConfig?.isConfigured
    ) {
      hasTriggeredFetch.current = true
      fetchData(lastfmConfig.username, lastfmFilter)
    }

    if (
      !hasTriggeredFetch.current &&
      selectedSource === DataSource.DISCOGS &&
      discogsConfig?.isConfigured
    ) {
      hasTriggeredFetch.current = true
      fetchDiscogsData(discogsConfig.username, discogsFilter)
    }

    if (
      !hasTriggeredFetch.current &&
      selectedSource === DataSource.SETLISTFM &&
      setlistConfig?.isConfigured
    ) {
      hasTriggeredFetch.current = true
      fetchSetlistFmData(setlistConfig.userId, setlistFmFilter)
    }
  }, [lastfmConfig, lastfmFilter, discogsConfig, discogsFilter, setlistConfig, setlistFmFilter, selectedSource, fetchData, fetchDiscogsData, fetchSetlistFmData])

  // Trigger match when data is fetched
  useEffect(() => {
    if (!hasTriggeredMatch.current && normalizedData && !isFetchLoading) {
      hasTriggeredMatch.current = true
      
      if (normalizedData.tracks && normalizedData.tracks.length > 0) {
        matchTracks(normalizedData.tracks)
      } else if (normalizedData.albums && normalizedData.albums.length > 0) {
        matchAlbums(normalizedData.albums)
      } else if (normalizedData.artists && normalizedData.artists.length > 0) {
        matchArtists(normalizedData.artists)
      }
    }
  }, [normalizedData, isFetchLoading, matchTracks, matchAlbums, matchArtists])

  const isLoading = isFetchLoading || isMatchLoading
  const error = fetchError || matchError

  if (isLoading) {
    const sourceName = 
      selectedSource === DataSource.LASTFM ? 'Last.fm' :
      selectedSource === DataSource.DISCOGS ? 'Discogs' :
      selectedSource === DataSource.SETLISTFM ? 'Setlist.fm' :
      'source'
    
    return (
      <Card>
        <Flex direction="column" align="center" gap="4" py="6">
          <Spinner />
          <Flex direction="column" align="center" gap="2">
            <Text size="2" weight="medium">
              {isFetchLoading ? `Fetching data from ${sourceName}` : 'Matching to Spotify'}
            </Text>
            <Text size="1" color="gray">
              This may take a moment...
            </Text>
          </Flex>
        </Flex>
      </Card>
    )
  }

  if (error) {
    return (
      <Card>
        <Flex direction="column" gap="4">
          <Flex align="center" gap="2" className="text-red-500">
            <AlertCircle className="h-5 w-5" />
            <Heading size="4" weight="medium">
              Error
            </Heading>
          </Flex>
          <Text size="2" color="red">
            {error}
          </Text>
        </Flex>
      </Card>
    )
  }

  if (normalizedData) {
    return (
      <Card>
        <Flex direction="column" gap="4">
          <Flex align="center" gap="2" className="text-green-500">
            <CheckCircle2 className="h-5 w-5" />
            <Heading size="4" weight="medium">
              Data Fetched & Matched
            </Heading>
          </Flex>
          <Box>
            <Text size="2" weight="medium" className="block mb-2">
              {normalizedData.totalResults} items processed
            </Text>
            <Text size="1" color="gray">
              Ready for curation
            </Text>
          </Box>
        </Flex>
      </Card>
    )
  }

  return null
}
