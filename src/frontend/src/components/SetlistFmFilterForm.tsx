import { useState } from 'react'
import { Button, TextField, Card, Heading, Flex, Box, Text } from '@radix-ui/themes'
import { AlertCircle, Sliders } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'

export function SetlistFmFilterForm() {
  const { setlistFmFilter, updateSetlistFmFilter } = useConfig()
  const [isExpanded, setIsExpanded] = useState(false)

  const handleStartDateChange = (value: string) => {
    updateSetlistFmFilter({ startDate: value || undefined })
  }

  const handleEndDateChange = (value: string) => {
    updateSetlistFmFilter({ endDate: value || undefined })
  }

  const handleMaxConcertsChange = (value: string) => {
    const num = parseInt(value, 10)
    if (!isNaN(num) && num > 0 && num <= 100) {
      updateSetlistFmFilter({ maxConcerts: num })
    }
  }

  const handleMaxTracksChange = (value: string) => {
    const num = parseInt(value, 10)
    if (!isNaN(num) && num > 0 && num <= 500) {
      updateSetlistFmFilter({ maxTracks: num })
    }
  }

  const hasDateRangeError =
    setlistFmFilter.startDate &&
    setlistFmFilter.endDate &&
    new Date(setlistFmFilter.startDate) > new Date(setlistFmFilter.endDate)

  return (
    <Card>
      <Flex direction="column" gap="4">
        <Flex align="center" justify="between">
          <Flex align="center" gap="2">
            <Sliders className="h-5 w-5" />
            <Heading size="4" weight="medium">
              Filter Setlist.fm Concerts
            </Heading>
          </Flex>
          <Button
            variant="ghost"
            size="1"
            onClick={() => setIsExpanded(!isExpanded)}
          >
            {isExpanded ? 'Hide' : 'Show'}
          </Button>
        </Flex>

        {isExpanded && (
          <Flex direction="column" gap="4">
            {/* Date Range */}
            <Flex direction="column" gap="3">
              <Box>
                <Text as="label" size="2" weight="medium" className="block mb-2">
                  Start Date (optional)
                </Text>
                <TextField.Root
                  type="date"
                  value={setlistFmFilter.startDate || ''}
                  onChange={(e) => handleStartDateChange(e.target.value)}
                  placeholder="Leave empty for all concerts"
                />
                <Text size="1" color="gray" className="mt-1 block">
                  Leave empty to include all concerts from the beginning
                </Text>
              </Box>
              <Box>
                <Text as="label" size="2" weight="medium" className="block mb-2">
                  End Date (optional)
                </Text>
                <TextField.Root
                  type="date"
                  value={setlistFmFilter.endDate || ''}
                  onChange={(e) => handleEndDateChange(e.target.value)}
                  placeholder="Leave empty for up to today"
                />
                <Text size="1" color="gray" className="mt-1 block">
                  Leave empty to include concerts up to today
                </Text>
              </Box>
              {hasDateRangeError && (
                <Flex align="center" gap="2" className="text-red-500">
                  <AlertCircle className="h-4 w-4" />
                  <Text size="2">Start date must be before end date</Text>
                </Flex>
              )}
            </Flex>

            {/* Max Concerts */}
            <Box>
              <Text as="label" size="2" weight="medium" className="block mb-2">
                Maximum Number of Concerts ({setlistFmFilter.maxConcerts})
              </Text>
              <TextField.Root
                type="number"
                min="1"
                max="100"
                value={setlistFmFilter.maxConcerts?.toString() ?? '10'}
                onChange={(e) => handleMaxConcertsChange(e.target.value)}
                placeholder="10"
              />
              <Text size="1" color="gray" className="mt-2 block">
                Fetch tracks from up to 100 concerts
              </Text>
            </Box>

            {/* Max Tracks */}
            <Box>
              <Text as="label" size="2" weight="medium" className="block mb-2">
                Maximum Tracks ({setlistFmFilter.maxTracks})
              </Text>
              <TextField.Root
                type="number"
                min="1"
                max="500"
                value={setlistFmFilter.maxTracks?.toString() ?? '100'}
                onChange={(e) => handleMaxTracksChange(e.target.value)}
                placeholder="100"
              />
              <Text size="1" color="gray" className="mt-2 block">
                Up to 500 deduplicated tracks can be fetched
              </Text>
            </Box>
          </Flex>
        )}
      </Flex>
    </Card>
  )
}
