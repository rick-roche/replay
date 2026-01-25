import { useEffect, useRef } from 'react'
import { Card, Heading, Flex, Box, Text, Spinner } from '@radix-ui/themes'
import { AlertCircle, CheckCircle2 } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'
import { useData } from '../contexts/DataContext'
import { useMatch } from '../contexts/MatchContext'
import { useDataSource } from '../contexts/DataSourceContext'
import { DataSource } from '../types/datasource'

export function AutoFetcher() {
  const { lastfmConfig, lastfmFilter } = useConfig()
  const { isLoading: isFetchLoading, error: fetchError, fetchData, data, normalizedData } = useData()
  const { isLoading: isMatchLoading, error: matchError, matchTracks } = useMatch()
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
  }, [lastfmConfig, lastfmFilter, selectedSource, fetchData])

  // Trigger match when data is fetched
  useEffect(() => {
    if (
      !hasTriggeredMatch.current &&
      normalizedData &&
      normalizedData.tracks &&
      normalizedData.tracks.length > 0 &&
      !isFetchLoading
    ) {
      hasTriggeredMatch.current = true
      matchTracks(normalizedData.tracks)
    }
  }, [normalizedData, isFetchLoading, matchTracks])

  const isLoading = isFetchLoading || isMatchLoading
  const error = fetchError || matchError

  if (isLoading) {
    return (
      <Card>
        <Flex direction="column" align="center" gap="4" py="6">
          <Spinner />
          <Flex direction="column" align="center" gap="2">
            <Text size="2" weight="medium">
              {isFetchLoading ? 'Fetching data from ' + (selectedSource === DataSource.LASTFM ? 'Last.fm' : 'source') : 'Matching to Spotify'}
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

  if (data && normalizedData) {
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
