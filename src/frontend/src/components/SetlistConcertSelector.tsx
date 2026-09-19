import { useEffect, useMemo, useState } from 'react'
import { Button, Card, Flex, Heading, Text, Box, Spinner } from '@radix-ui/themes'
import { AlertCircle } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'
import { useData } from '../contexts/DataContext'

interface SetlistConcertSelectorProps {
  userId: string
  onFetchTracks?: () => void
  isFetching?: boolean
  isFetchDisabled?: boolean
}

export function SetlistConcertSelector({
  userId,
  onFetchTracks,
  isFetching = false,
  isFetchDisabled = false
}: SetlistConcertSelectorProps) {
  const {
    setlistFmFilter,
    getSelectedSetlistConcertIds,
    setSelectedSetlistConcertIds,
    clearSelectedSetlistConcertIds
  } = useConfig()
  const { setlistConcertsPage, isLoadingConcerts, error, fetchSetlistFmConcerts } = useData()
  const [pageNumber, setPageNumber] = useState(1)
  const [appliedFilter, setAppliedFilter] = useState(setlistFmFilter)

  const selectedIds = useMemo(() => getSelectedSetlistConcertIds(userId), [getSelectedSetlistConcertIds, userId])
  // Normalize to lowercase for case-insensitive membership checks (IDs stored with original casing)
  const selectedIdSet = useMemo(() => new Set(selectedIds.map((id) => id.toLowerCase())), [selectedIds])

  const appliedFilterKey = useMemo(() => JSON.stringify(appliedFilter), [appliedFilter])
  const currentFilterKey = useMemo(() => JSON.stringify(setlistFmFilter), [setlistFmFilter])
  const isFilterStale = appliedFilterKey !== currentFilterKey

  useEffect(() => {
    void Promise.resolve(fetchSetlistFmConcerts(userId, appliedFilter, pageNumber)).catch(() => undefined)
  }, [pageNumber, userId, appliedFilter, fetchSetlistFmConcerts])

  const concerts = isLoadingConcerts ? [] : setlistConcertsPage?.concerts ?? []
  const maxConcerts = Number(setlistFmFilter.maxConcerts ?? 10)
  const canSelectMore = selectedIds.length < maxConcerts

  const toggleConcert = (concertId: string, isChecked: boolean) => {
    if (isChecked && canSelectMore) {
      setSelectedSetlistConcertIds(userId, [...selectedIds, concertId])
      return
    }

    setSelectedSetlistConcertIds(
      userId,
      selectedIds.filter((id) => id.toLowerCase() !== concertId.toLowerCase())
    )
  }

  const selectPageConcerts = () => {
    const pageIds = concerts.map((concert) => concert.id).filter(Boolean)
    const nextIds = [...selectedIds]
    const nextIdSet = new Set(selectedIds.map((id) => id.toLowerCase()))
    for (const id of pageIds) {
      const normalizedId = id.toLowerCase()
      if (nextIds.length >= maxConcerts || nextIdSet.has(normalizedId)) continue

      nextIds.push(id)
      nextIdSet.add(normalizedId)
    }

    setSelectedSetlistConcertIds(userId, nextIds)
  }

  const handleRefresh = () => {
    setAppliedFilter({ ...setlistFmFilter })
    setPageNumber(1)
  }

  return (
    <div id="setlist-concert-selector" tabIndex={-1} className="scroll-mt-6">
      <Card>
        <Flex direction="column" gap="4">
          <Flex align="center" justify="between" gap="3" wrap="wrap">
            <Box>
              <Heading size="4" weight="medium">
                Select Concerts
              </Heading>
              <Text size="1" color="gray" aria-live="polite">
                {selectedIds.length} selected
              </Text>
            </Box>
            {onFetchTracks && (
              <Button type="button" onClick={onFetchTracks} disabled={isFetchDisabled || isFetching}>
                {isFetching ? 'Fetching...' : 'Fetch Tracks'}
              </Button>
            )}
          </Flex>
          {onFetchTracks && isFetchDisabled && (
            <Text size="1" color="gray">
              {selectedIds.length === 0
                ? 'Select at least one concert to fetch tracks.'
                : selectedIds.length > maxConcerts
                  ? `Reduce the selection to ${maxConcerts} concerts or fewer to fetch tracks.`
                  : 'Fetching is currently unavailable.'}
            </Text>
          )}

          <Flex gap="2" wrap="wrap">
            <Button type="button" variant="soft" onClick={selectPageConcerts} disabled={isLoadingConcerts || concerts.length === 0 || !canSelectMore}>
              Select Page
            </Button>
            <Button
              type="button"
              variant="soft"
              color="gray"
              onClick={() => clearSelectedSetlistConcertIds(userId)}
              disabled={selectedIds.length === 0}
            >
              Clear All
            </Button>
            <Button
              type="button"
              variant="soft"
              onClick={handleRefresh}
              disabled={isLoadingConcerts}
            >
              Refresh Concerts
            </Button>
          </Flex>

          {isFilterStale && (
            <Text size="1" className="text-amber-400">
              Filters updated. Refresh concerts to apply the changes.
            </Text>
          )}

          <Text size="1" color="gray">
            Select up to {maxConcerts} concerts. The maximum track limit still applies.
          </Text>

        {isLoadingConcerts && (
          <Flex align="center" gap="2">
            <Spinner />
            <Text size="2" color="gray">
              Loading concerts...
            </Text>
          </Flex>
        )}

        {error && (
          <Flex align="center" gap="2" className="text-red-500">
            <AlertCircle className="h-4 w-4" />
            <Text size="2">{error}</Text>
          </Flex>
        )}

          {!isLoadingConcerts && concerts.length === 0 && (
            <Text size="2" color="gray">
             No concerts on this page match the current filters.
            </Text>
          )}

        {concerts.length > 0 && (
          <Flex direction="column" gap="2">
            {concerts.map((concert) => {
              const isSelected = selectedIdSet.has(concert.id.toLowerCase())
              const descriptor = [concert.date, concert.venue, concert.city, concert.country]
                .filter(Boolean)
                .join(' - ')

              return (
                <Box key={concert.id} className="rounded border border-zinc-700 p-3">
                  <label className="flex cursor-pointer items-start gap-3">
                    <input
                      type="checkbox"
                      checked={isSelected}
                      disabled={isLoadingConcerts || (!isSelected && !canSelectMore)}
                      onChange={(event) => toggleConcert(concert.id, event.target.checked)}
                      aria-label={`Include concert ${concert.artist} ${descriptor}`}
                    />
                    <Flex direction="column" gap="1">
                      <Text size="2" weight="medium">
                        {concert.artist}
                      </Text>
                      <Text size="1" color="gray">
                        {descriptor || 'Unknown concert details'}
                      </Text>
                    </Flex>
                  </label>
                </Box>
              )
            })}
          </Flex>
        )}

          <Flex justify="between" align="center" gap="2">
            <Button
              type="button"
              variant="soft"
              onClick={() => setPageNumber((value) => Math.max(1, value - 1))}
              disabled={isLoadingConcerts || isFilterStale || pageNumber <= 1}
            >
              Previous
            </Button>
            <Text size="2" color="gray">
              Page {setlistConcertsPage?.pageNumber ?? pageNumber}
            </Text>
            <Button
              type="button"
              variant="soft"
              onClick={() => setPageNumber((value) => value + 1)}
              disabled={isLoadingConcerts || isFilterStale || !(setlistConcertsPage?.hasNextPage ?? false)}
            >
              Next
            </Button>
          </Flex>
        </Flex>
      </Card>
    </div>
  )
}
