import { useEffect, useState } from 'react'
import { Button, Card, Heading, Flex, Box, Text, Spinner } from '@radix-ui/themes'
import { AlertCircle, CheckCircle2, Download, PlusCircle } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'
import { useData } from '../contexts/DataContext'
import { useMatch } from '../contexts/MatchContext'

export function FetchDataButton() {
  const { lastfmConfig, lastfmFilter } = useConfig()
  const { isLoading, error, fetchData, fetchMoreData, clearError, data } = useData()
  const { appendMatches } = useMatch()
  const [infoMessage, setInfoMessage] = useState<string | null>(null)

  if (!lastfmConfig?.isConfigured) {
    return null
  }

  const handleFetch = async () => {
    clearError()
    setInfoMessage(null)
    await fetchData(lastfmConfig.username, lastfmFilter)
  }

  const handleFetchMore = async () => {
    clearError()
    setInfoMessage(null)
    const added = await fetchMoreData(lastfmConfig.username, lastfmFilter)
    if (added.length === 0) {
      setInfoMessage('No new tracks found to add')
      return
    }
    await appendMatches(added)
    setInfoMessage(`${added.length} new tracks fetched and added`)
  }

  const isDisabled = isLoading || !lastfmConfig.isConfigured
  const canFetchMore = Boolean(data) && !isLoading

  return (
    <Card>
      <Flex direction="column" gap="4">
        <Box>
          <Heading size="4" weight="medium" mb="2">
            Fetch Data
          </Heading>
          <Text size="2" color="gray">
            Retrieve {lastfmFilter.dataType.toLowerCase()} from Last.fm
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
  const { data, isLoading } = useData()
  const { matchedData, isLoading: isMatching } = useMatch()
  const [isCollapsed, setIsCollapsed] = useState(false)
  const [hasAutoCollapsed, setHasAutoCollapsed] = useState(false)

  useEffect(() => {
    if (!matchedData || isMatching) {
      setIsCollapsed(false)
      setHasAutoCollapsed(false)
      return
    }

    if (!hasAutoCollapsed) {
      setIsCollapsed(true)
      setHasAutoCollapsed(true)
    }
  }, [matchedData, isMatching, hasAutoCollapsed])

  if (isLoading) {
    return (
      <Card>
        <Flex direction="column" align="center" gap="4" py="6">
          <Spinner />
          <Text size="2" color="gray">
            Fetching data from Last.fm...
          </Text>
        </Flex>
      </Card>
    )
  }

  if (!data) {
    return null
  }

  const dataTypeLower = data.dataType.toLowerCase()
  const dataCount =
    data.dataType === 'Tracks'
      ? data.tracks.length
      : data.dataType === 'Albums'
        ? data.albums.length
        : data.artists.length
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
              {data.dataType} Found
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
            {data.dataType === 'Tracks' && data.tracks.length > 0 && (
              <Box>
                <Text size="2" weight="medium" className="block mb-2">
                  {data.tracks.length} tracks found
                </Text>
                <Flex direction="column" gap="2">
                  {data.tracks.slice(0, 10).map((track, idx) => (
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
                          {track.playCount} plays
                        </Text>
                      </Flex>
                    </Box>
                  ))}
                  {data.tracks.length > 10 && (
                    <Text size="1" color="gray">
                      ... and {data.tracks.length - 10} more
                    </Text>
                  )}
                </Flex>
              </Box>
            )}

            {data.dataType === 'Albums' && data.albums.length > 0 && (
              <Box>
                <Text size="2" weight="medium" className="block mb-2">
                  {data.albums.length} albums found
                </Text>
                <Flex direction="column" gap="2">
                  {data.albums.slice(0, 10).map((album, idx) => (
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
                          {album.playCount} plays
                        </Text>
                      </Flex>
                    </Box>
                  ))}
                  {data.albums.length > 10 && (
                    <Text size="1" color="gray">
                      ... and {data.albums.length - 10} more
                    </Text>
                  )}
                </Flex>
              </Box>
            )}

            {data.dataType === 'Artists' && data.artists.length > 0 && (
              <Box>
                <Text size="2" weight="medium" className="block mb-2">
                  {data.artists.length} artists found
                </Text>
                <Flex direction="column" gap="2">
                  {data.artists.slice(0, 10).map((artist, idx) => (
                    <Box
                      key={idx}
                      className="text-sm p-2 rounded border border-zinc-700"
                    >
                      <Flex justify="between" align="center">
                        <Text size="2" weight="medium">
                          {artist.name}
                        </Text>
                        <Text size="1" color="gray">
                          {artist.playCount} plays
                        </Text>
                      </Flex>
                    </Box>
                  ))}
                  {data.artists.length > 10 && (
                    <Text size="1" color="gray">
                      ... and {data.artists.length - 10} more
                    </Text>
                  )}
                </Flex>
              </Box>
            )}

            {((data.dataType === 'Tracks' && data.tracks.length === 0) ||
              (data.dataType === 'Albums' && data.albums.length === 0) ||
              (data.dataType === 'Artists' && data.artists.length === 0)) && (
              <Text size="2" color="gray">
                No {data.dataType.toLowerCase()} found with the current filters.
              </Text>
            )}
          </>
        )}
      </Flex>
    </Card>
  )
}
