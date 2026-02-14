import { useEffect, useState } from 'react'
import { Button, Select, TextField, Card, Heading, Flex, Box, Text } from '@radix-ui/themes'
import { Sliders } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'

export function DiscogsFilterForm() {
  const { discogsFilter, updateDiscogsFilter } = useConfig()
  const [isExpanded, setIsExpanded] = useState(false)
  const [minReleaseYearInput, setMinReleaseYearInput] = useState('')
  const [maxReleaseYearInput, setMaxReleaseYearInput] = useState('')
  const [minYearAddedInput, setMinYearAddedInput] = useState('')
  const [maxYearAddedInput, setMaxYearAddedInput] = useState('')

  const currentYear = new Date().getFullYear()

  useEffect(() => {
    setMinReleaseYearInput(discogsFilter.minReleaseYear?.toString() ?? '')
    setMaxReleaseYearInput(discogsFilter.maxReleaseYear?.toString() ?? '')
    setMinYearAddedInput(discogsFilter.minYearAdded?.toString() ?? '')
    setMaxYearAddedInput(discogsFilter.maxYearAdded?.toString() ?? '')
  }, [
    discogsFilter.minReleaseYear,
    discogsFilter.maxReleaseYear,
    discogsFilter.minYearAdded,
    discogsFilter.maxYearAdded
  ])

  const applyYearUpdate = (
    value: string,
    onValid: (num: number | undefined) => void
  ) => {
    if (value === '') {
      onValid(undefined)
      return
    }

    const num = parseInt(value, 10)
    if (Number.isNaN(num)) {
      return
    }

    if (num < 1900 || num > currentYear) {
      return
    }

    onValid(num)
  }

  const handleMinReleaseYearChange = (value: string) => {
    setMinReleaseYearInput(value)
    applyYearUpdate(value, (num) => updateDiscogsFilter({ minReleaseYear: num }))
  }

  const handleMaxReleaseYearChange = (value: string) => {
    setMaxReleaseYearInput(value)
    applyYearUpdate(value, (num) => updateDiscogsFilter({ maxReleaseYear: num }))
  }

  const handleMediaFormatChange = (value: string) => {
    updateDiscogsFilter({ mediaFormat: value || undefined })
  }

  const handleMinYearAddedChange = (value: string) => {
    setMinYearAddedInput(value)
    applyYearUpdate(value, (num) => updateDiscogsFilter({ minYearAdded: num }))
  }

  const handleMaxYearAddedChange = (value: string) => {
    setMaxYearAddedInput(value)
    applyYearUpdate(value, (num) => updateDiscogsFilter({ maxYearAdded: num }))
  }

  const handleMaxTracksChange = (value: string) => {
    const num = parseInt(value, 10)
    if (!isNaN(num) && num > 0 && num <= 500) {
      updateDiscogsFilter({ maxTracks: num })
    }
  }

  const hasReleaseYearError =
    discogsFilter.minReleaseYear &&
    discogsFilter.maxReleaseYear &&
    discogsFilter.minReleaseYear > discogsFilter.maxReleaseYear

  const hasYearAddedError =
    discogsFilter.minYearAdded &&
    discogsFilter.maxYearAdded &&
    discogsFilter.minYearAdded > discogsFilter.maxYearAdded

  return (
    <Card>
      <Flex direction="column" gap="4">
        <Flex align="center" justify="between">
          <Flex align="center" gap="2">
            <Sliders className="h-5 w-5" />
            <Heading size="4" weight="medium">
              Filter Discogs Collection
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
            {/* Media Format Selection */}
            <Box>
              <Text as="label" size="2" weight="medium" className="block mb-2">
                Media Format (optional)
              </Text>
              <Select.Root value={discogsFilter.mediaFormat || undefined} onValueChange={handleMediaFormatChange}>
                <Select.Trigger placeholder="All formats" />
                <Select.Content>
                  <Select.Item value="Vinyl">Vinyl</Select.Item>
                  <Select.Item value="CD">CD</Select.Item>
                  <Select.Item value="Cassette">Cassette</Select.Item>
                  <Select.Item value="Digital">Digital</Select.Item>
                </Select.Content>
              </Select.Root>
              <Text size="1" color="gray" className="mt-2 block">
                Filter collection by media format
              </Text>
            </Box>

            {/* Release Year Range */}
            <Flex direction="column" gap="3">
              <Text size="2" weight="medium" className="block">
                Release Year (optional)
              </Text>
              <Flex gap="3">
                <Box className="flex-1">
                  <Text as="label" size="1" weight="medium" className="block mb-2">
                    From
                  </Text>
                  <TextField.Root
                    type="number"
                    min="1900"
                    max={currentYear}
                    value={minReleaseYearInput}
                    onChange={(e) => handleMinReleaseYearChange(e.target.value)}
                    placeholder="1900"
                  />
                </Box>
                <Box className="flex-1">
                  <Text as="label" size="1" weight="medium" className="block mb-2">
                    To
                  </Text>
                  <TextField.Root
                    type="number"
                    min="1900"
                    max={currentYear}
                    value={maxReleaseYearInput}
                    onChange={(e) => handleMaxReleaseYearChange(e.target.value)}
                    placeholder={currentYear.toString()}
                  />
                </Box>
              </Flex>
              {hasReleaseYearError && (
                <Text size="2" color="red">
                  Start year must be before end year
                </Text>
              )}
            </Flex>

            {/* Year Added to Collection Range */}
            <Flex direction="column" gap="3">
              <Text size="2" weight="medium" className="block">
                Year Added to Collection (optional)
              </Text>
              <Flex gap="3">
                <Box className="flex-1">
                  <Text as="label" size="1" weight="medium" className="block mb-2">
                    From
                  </Text>
                  <TextField.Root
                    type="number"
                    min="1900"
                    max={currentYear}
                    value={minYearAddedInput}
                    onChange={(e) => handleMinYearAddedChange(e.target.value)}
                    placeholder="1900"
                  />
                </Box>
                <Box className="flex-1">
                  <Text as="label" size="1" weight="medium" className="block mb-2">
                    To
                  </Text>
                  <TextField.Root
                    type="number"
                    min="1900"
                    max={currentYear}
                    value={maxYearAddedInput}
                    onChange={(e) => handleMaxYearAddedChange(e.target.value)}
                    placeholder={currentYear.toString()}
                  />
                </Box>
              </Flex>
              {hasYearAddedError && (
                <Text size="2" color="red">
                  Start year must be before end year
                </Text>
              )}
            </Flex>

            {/* Max Tracks */}
            <Box>
              <Text as="label" size="2" weight="medium" className="block mb-2">
                Maximum Tracks ({discogsFilter.maxTracks})
              </Text>
              <TextField.Root
                type="number"
                min="1"
                max="500"
                value={discogsFilter.maxTracks?.toString() ?? '100'}
                onChange={(e) => handleMaxTracksChange(e.target.value)}
                placeholder="100"
              />
              <Text size="1" color="gray" className="mt-2 block">
                Up to 500 tracks can be extracted from your collection
              </Text>
            </Box>
          </Flex>
        )}
      </Flex>
    </Card>
  )
}
