import { useState } from 'react'
import { Button, Select, TextField, Card, Heading, Flex, Box, Text } from '@radix-ui/themes'
import { AlertCircle, Sliders } from 'lucide-react'
import { LastfmDataType, LastfmTimePeriod } from '../types/lastfm'
import { useConfig } from '../contexts/ConfigContext'

export function LastfmFilterForm() {
  const { lastfmFilter, updateFilter } = useConfig()
  const [isExpanded, setIsExpanded] = useState(false)

  const handleDataTypeChange = (value: string) => {
    updateFilter({ dataType: value as LastfmDataType })
  }

  const handleTimePeriodChange = (value: string) => {
    if (value === LastfmTimePeriod.Custom) {
      updateFilter({
        timePeriod: value as LastfmTimePeriod,
        customStartDate: new Date(new Date().setFullYear(new Date().getFullYear() - 1)).toISOString().split('T')[0],
        customEndDate: new Date().toISOString().split('T')[0]
      })
    } else {
      updateFilter({ timePeriod: value as LastfmTimePeriod })
    }
  }

  const handleMaxResultsChange = (value: string) => {
    const num = parseInt(value, 10)
    if (!isNaN(num) && num > 0 && num <= 500) {
      updateFilter({ maxResults: num })
    }
  }

  const handleCustomStartDateChange = (value: string) => {
    updateFilter({ customStartDate: value })
  }

  const handleCustomEndDateChange = (value: string) => {
    updateFilter({ customEndDate: value })
  }

  const isCustomPeriod = lastfmFilter.timePeriod === LastfmTimePeriod.Custom

  return (
    <Card>
      <Flex direction="column" gap="4">
        <Flex align="center" justify="between">
          <Flex align="center" gap="2">
            <Sliders className="h-5 w-5" />
            <Heading size="4" weight="medium">
              Filter Last.fm Data
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
            {/* Data Type Selection */}
            <Box>
              <Text as="label" size="2" weight="medium" className="block mb-2">
                What to fetch
              </Text>
              <Select.Root value={lastfmFilter.dataType} onValueChange={handleDataTypeChange}>
                <Select.Trigger />
                <Select.Content>
                  <Select.Item value={LastfmDataType.Tracks}>Top Tracks</Select.Item>
                  <Select.Item value={LastfmDataType.Albums}>Top Albums</Select.Item>
                  <Select.Item value={LastfmDataType.Artists}>Top Artists</Select.Item>
                </Select.Content>
              </Select.Root>
            </Box>

            {/* Time Period Selection */}
            <Box>
              <Text as="label" size="2" weight="medium" className="block mb-2">
                Time Period
              </Text>
              <Select.Root value={lastfmFilter.timePeriod} onValueChange={handleTimePeriodChange}>
                <Select.Trigger />
                <Select.Content>
                  <Select.Item value={LastfmTimePeriod.Last7Days}>Last 7 days</Select.Item>
                  <Select.Item value={LastfmTimePeriod.Last1Month}>Last month</Select.Item>
                  <Select.Item value={LastfmTimePeriod.Last3Months}>Last 3 months</Select.Item>
                  <Select.Item value={LastfmTimePeriod.Last6Months}>Last 6 months</Select.Item>
                  <Select.Item value={LastfmTimePeriod.Last12Months}>Last year</Select.Item>
                  <Select.Item value={LastfmTimePeriod.Overall}>All time</Select.Item>
                  <Select.Item value={LastfmTimePeriod.Custom}>Custom date range</Select.Item>
                </Select.Content>
              </Select.Root>
            </Box>

            {/* Custom Date Range */}
            {isCustomPeriod && (
              <Flex direction="column" gap="3">
                <Box>
                  <Text as="label" size="2" weight="medium" className="block mb-2">
                    Start Date
                  </Text>
                  <TextField.Root
                    type="date"
                    value={lastfmFilter.customStartDate || ''}
                    onChange={(e) => handleCustomStartDateChange(e.target.value)}
                  />
                </Box>
                <Box>
                  <Text as="label" size="2" weight="medium" className="block mb-2">
                    End Date
                  </Text>
                  <TextField.Root
                    type="date"
                    value={lastfmFilter.customEndDate || ''}
                    onChange={(e) => handleCustomEndDateChange(e.target.value)}
                  />
                </Box>
                {lastfmFilter.customStartDate && lastfmFilter.customEndDate && new Date(lastfmFilter.customStartDate) > new Date(lastfmFilter.customEndDate) && (
                  <Flex align="center" gap="2" className="text-red-500">
                    <AlertCircle className="h-4 w-4" />
                    <Text size="2">Start date must be before end date</Text>
                  </Flex>
                )}
              </Flex>
            )}

            {/* Max Results */}
            <Box>
              <Text as="label" size="2" weight="medium" className="block mb-2">
                Maximum Results ({lastfmFilter.maxResults})
              </Text>
              <TextField.Root
                type="number"
                min="1"
                max="500"
                value={lastfmFilter.maxResults.toString()}
                onChange={(e) => handleMaxResultsChange(e.target.value)}
                placeholder="50"
              />
              <Text size="1" color="gray" className="mt-2 block">
                Up to 500 items can be fetched at once
              </Text>
            </Box>
          </Flex>
        )}
      </Flex>
    </Card>
  )
}
