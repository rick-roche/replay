import { useEffect, useState } from 'react'
import { Button, Card, Heading, Flex, Box, Text, Spinner } from '@radix-ui/themes'
import { AlertCircle, CheckCircle2, Download, PlusCircle } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'
import { useData } from '../contexts/DataContext'
import { useMatch } from '../contexts/MatchContext'
import { useDataSource } from '../contexts/DataSourceContext'
import { DataSource } from '../types/datasource'
import type { components } from '../api/generated-client'

type NormalizedTrack = components['schemas']['NormalizedTrack']
type NormalizedAlbum = components['schemas']['NormalizedAlbum']
type NormalizedArtist = components['schemas']['NormalizedArtist']

export function FetchDataButton() {
  const { lastfmConfig, lastfmFilter, discogsConfig, discogsFilter, setlistConfig, setlistFmFilter } = useConfig()
  const { isLoading, error, fetchData, fetchSetlistFmData, fetchDiscogsData, fetchMoreData, clearError, normalizedData } = useData()
  const { appendMatches } = useMatch()
  const { selectedSource } = useDataSource()
  const [infoMessage, setInfoMessage] = useState<string | null>(null)

  const isLastfmConfigured = lastfmConfig?.isConfigured
  const isDiscogsConfigured = discogsConfig?.isConfigured
  const isSetlistConfigured = setlistConfig?.isConfigured

  if (!isLastfmConfigured && !isDiscogsConfigured && !isSetlistConfigured) {
    return null
  }

  const handleFetch = async () => {
    clearError()
    setInfoMessage(null)
    if (selectedSource === DataSource.LASTFM && isLastfmConfigured) {
      await fetchData(lastfmConfig!.username, lastfmFilter)
    } else if (selectedSource === DataSource.DISCOGS && isDiscogsConfigured) {
      await fetchDiscogsData(discogsConfig!.username, discogsFilter)
    } else if (selectedSource === DataSource.SETLISTFM && isSetlistConfigured) {
      await fetchSetlistFmData(setlistConfig!.userId, setlistFmFilter)
    }
  }

  const handleFetchMore = async () => {
    clearError()
    setInfoMessage(null)
    if (selectedSource === DataSource.LASTFM && isLastfmConfigured) {
      const added = await fetchMoreData(lastfmConfig!.username, lastfmFilter)
      if (added.length === 0) {
        setInfoMessage('No new tracks found to add')
        return
      }
      await appendMatches(added)
      setInfoMessage(`${added.length} new tracks fetched and added`)
    } else if (selectedSource === DataSource.DISCOGS && isDiscogsConfigured) {
      setInfoMessage('Fetch more not available for Discogs')
    } else if (selectedSource === DataSource.SETLISTFM && isSetlistConfigured) {
      setInfoMessage('Fetch more not available for Setlist.fm')
    }
  }

  const isDisabled = isLoading || (!isLastfmConfigured && !isDiscogsConfigured && !isSetlistConfigured)
  const canFetchMore = selectedSource === DataSource.LASTFM && Boolean(normalizedData) && !isLoading

  const sourceName = 
    selectedSource === DataSource.LASTFM ? 'Last.fm' :
    selectedSource === DataSource.DISCOGS ? 'Discogs' :
    selectedSource === DataSource.SETLISTFM ? 'Setlist.fm' :
    'source'
  const dataTypeText = 
    selectedSource === DataSource.LASTFM ? lastfmFilter.dataType.toLowerCase() :
    selectedSource === DataSource.DISCOGS ? 'releases' :
    selectedSource === DataSource.SETLISTFM ? 'concerts' :
    'data'

  return (
    <Card>
      <Flex direction="column" gap="4">
        <Box>
          <Heading size="4" weight="medium" mb="2">
            Fetch Data
          </Heading>
          <Text size="2" color="gray">
            Retrieve {dataTypeText} from {sourceName}
          </Text>
        </Box>

        {error && (
          <Flex align="center" gap="2" className="text-red-500">
            <AlertCircle className="h-4 w-4" />
            <Text size="2">{error}</Text>
          </Flex>
        )}

        {infoMessage && (
          <Text size="2" color="gray">
            {infoMessage}
          </Text>
        )}

        <Button onClick={handleFetch} disabled={isDisabled}>
          {isLoading ? (
            <>
              <Spinner />
              Fetching...
            </>
          ) : (
            <>
              <Download className="h-4 w-4" />
              Fetch Data
            </>
          )}
        </Button>

        {canFetchMore && (
          <Button onClick={handleFetchMore} disabled={isDisabled} variant="soft">
            <PlusCircle className="h-4 w-4" />
            Fetch More
          </Button>
        )}
      </Flex>
    </Card>
  )
}

export function DataResults() {
  const { normalizedData, isLoading } = useData()
  const { matchedData, matchedAlbums, matchedArtists, isLoading: isMatching } = useMatch()
  const { selectedSource } = useDataSource()
  const [isCollapsed, setIsCollapsed] = useState(false)
  const [hasAutoCollapsed, setHasAutoCollapsed] = useState(false)

  useEffect(() => {
    if ((!matchedData && !matchedAlbums && !matchedArtists) || isMatching) {
      setIsCollapsed(false)
      setHasAutoCollapsed(false)
      return
    }

    if (!hasAutoCollapsed) {
      setIsCollapsed(true)
      setHasAutoCollapsed(true)
    }
  }, [matchedData, matchedAlbums, matchedArtists, isMatching, hasAutoCollapsed])

  if (isLoading) {
    const sourceName =
      selectedSource === DataSource.SETLISTFM ? 'Setlist.fm' :
      selectedSource === DataSource.DISCOGS ? 'Discogs' :
      'Last.fm'
    return (
      <Card>
        <Flex direction="column" align="center" gap="4" py="6">
          <Spinner />
          <Text size="2" color="gray">
            Fetching data from {sourceName}...
          </Text>
        </Flex>
      </Card>
    )
  }

  // Use normalizedData for all sources
  const displayData = normalizedData
  if (!displayData) {
    return null
  }

  const getPlayCount = (item: Record<string, unknown>): number => {
    // For Last.fm data, playCount is directly on the object
    if ('playCount' in item && item.playCount !== undefined) {
      const count = item.playCount
      return typeof count === 'number' ? count : typeof count === 'string' ? Number(count) : 0
    }
    // For normalized data, it's in sourceMetadata
    if ('sourceMetadata' in item && item.sourceMetadata && typeof item.sourceMetadata === 'object') {
      const meta = item.sourceMetadata as Record<string, unknown>
      if ('playCount' in meta && meta.playCount !== undefined) {
        const count = meta.playCount
        return typeof count === 'number' ? count : typeof count === 'string' ? Number(count) : 0
      }
    }
    return 0
  }

  const dataTypeLower = displayData.dataType.toLowerCase()
  const dataCount =
    displayData.dataType === 'Tracks'
      ? displayData.tracks.length
      : displayData.dataType === 'Albums'
        ? displayData.albums.length
        : displayData.artists.length
  const summaryMessage =
    dataCount === 0
      ? `No ${dataTypeLower} found with the current filters.`
      : `${dataCount} ${dataTypeLower} found`

  return (
    <Card>
      <Flex direction="column" gap="4">
        <Flex align="center" justify="between">
          <Flex align="center" gap="2" className="text-green-500">
            <CheckCircle2 className="h-5 w-5" />
            <Heading size="4" weight="medium">
              {displayData.dataType} Found
            </Heading>
          </Flex>
          <Button
            variant="ghost"
            size="1"
            onClick={() => setIsCollapsed((prev) => !prev)}
          >
            {isCollapsed ? 'Show' : 'Hide'}
          </Button>
        </Flex>

        {isCollapsed ? (
          <Text size="2" color="gray">
            {summaryMessage}
          </Text>
        ) : (
          <>
            {displayData.dataType === 'Tracks' && displayData.tracks.length > 0 && (
              <Box>
                <Text size="2" weight="medium" className="block mb-2">
                  {displayData.tracks.length} tracks found
                </Text>
                <Flex direction="column" gap="2">
                  {displayData.tracks.slice(0, 10).map((track: NormalizedTrack, idx: number) => (
                    <Box
                      key={idx}
                      className="text-sm p-2 rounded border border-zinc-700"
                    >
                      <Flex justify="between" align="center">
                        <Flex direction="column" gap="1">
                          <Text size="2" weight="medium">
                            {track.name}
                          </Text>
                          <Text size="1" color="gray">
                            {track.artist}
                          </Text>
                        </Flex>
                        <Text size="1" color="gray">
                          {getPlayCount(track)} plays
                        </Text>
                      </Flex>
                    </Box>
                  ))}
                  {displayData.tracks.length > 10 && (
                    <Text size="1" color="gray">
                      ... and {displayData.tracks.length - 10} more
                    </Text>
                  )}
                </Flex>
              </Box>
            )}

            {displayData.dataType === 'Albums' && displayData.albums.length > 0 && (
              <Box>
                <Text size="2" weight="medium" className="block mb-2">
                  {displayData.albums.length} albums found
                </Text>
                <Flex direction="column" gap="2">
                  {displayData.albums.slice(0, 10).map((album: NormalizedAlbum, idx: number) => (
                    <Box
                      key={idx}
                      className="text-sm p-2 rounded border border-zinc-700"
                    >
                      <Flex justify="between" align="center">
                        <Flex direction="column" gap="1">
                          <Text size="2" weight="medium">
                            {album.name}
                          </Text>
                          <Text size="1" color="gray">
                            {album.artist}
                          </Text>
                        </Flex>
                        <Text size="1" color="gray">
                          {getPlayCount(album)} plays
                        </Text>
                      </Flex>
                    </Box>
                  ))}
                  {displayData.albums.length > 10 && (
                    <Text size="1" color="gray">
                      ... and {displayData.albums.length - 10} more
                    </Text>
                  )}
                </Flex>
              </Box>
            )}

            {displayData.dataType === 'Artists' && displayData.artists.length > 0 && (
              <Box>
                <Text size="2" weight="medium" className="block mb-2">
                  {displayData.artists.length} artists found
                </Text>
                <Flex direction="column" gap="2">
                  {displayData.artists.slice(0, 10).map((artist: NormalizedArtist, idx: number) => (
                    <Box
                      key={idx}
                      className="text-sm p-2 rounded border border-zinc-700"
                    >
                      <Flex justify="between" align="center">
                        <Text size="2" weight="medium">
                          {artist.name}
                        </Text>
                        <Text size="1" color="gray">
                          {getPlayCount(artist)} plays
                        </Text>
                      </Flex>
                    </Box>
                  ))}
                  {displayData.artists.length > 10 && (
                    <Text size="1" color="gray">
                      ... and {displayData.artists.length - 10} more
                    </Text>
                  )}
                </Flex>
              </Box>
            )}

            {((displayData.dataType === 'Tracks' && displayData.tracks.length === 0) ||
              (displayData.dataType === 'Albums' && displayData.albums.length === 0) ||
              (displayData.dataType === 'Artists' && displayData.artists.length === 0)) && (
              <Text size="2" color="gray">
                No {displayData.dataType.toLowerCase()} found with the current filters.
              </Text>
            )}
          </>
        )}
      </Flex>
    </Card>
  )
}
