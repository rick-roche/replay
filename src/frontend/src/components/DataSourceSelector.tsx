import { Radio, Disc3, ListMusic } from 'lucide-react'
import { Card, Flex, Heading, Text, Box, Grid } from '@radix-ui/themes'
import { useDataSource } from '../contexts/DataSourceContext'
import { DataSource, type DataSourceInfo } from '../types/datasource'

const dataSources: DataSourceInfo[] = [
  {
    id: DataSource.LASTFM,
    name: 'Last.fm',
    description: 'Create playlists from your listening history',
    icon: 'radio',
    enabled: true
  },
  {
    id: DataSource.DISCOGS,
    name: 'Discogs',
    description: 'Convert your collection into playlists',
    icon: 'disc',
    enabled: true
  },
  {
    id: DataSource.SETLISTFM,
    name: 'Setlist.fm',
    description: 'Relive concerts with setlist-based playlists',
    icon: 'listmusic',
    enabled: true
  }
]

const icons = {
  radio: Radio,
  disc: Disc3,
  listmusic: ListMusic
}

export function DataSourceSelector() {
  const { selectedSource, selectSource } = useDataSource()

  return (
    <Card>
      <Flex direction="column" gap="4">
        <Box>
          <Heading size="4" weight="medium" mb="2">
            Select Data Source
          </Heading>
          <Text size="2" color="gray">
            Choose where you want to pull music data from
          </Text>
        </Box>

        <Grid columns={{ initial: '1', sm: '3' }} gap="3">
          {dataSources.map((source) => {
            const Icon = icons[source.icon as keyof typeof icons]
            const isSelected = selectedSource === source.id
            const isDisabled = !source.enabled

            return (
              <button
                key={source.id}
                onClick={() => source.enabled && selectSource(source.id)}
                disabled={isDisabled}
                className={`
                  relative p-4 rounded-lg border-2 transition-all text-left
                  ${isSelected
                    ? 'border-green-500 bg-green-500/10'
                      : isDisabled
                        ? 'border-zinc-800 bg-zinc-900/50 opacity-50 cursor-not-allowed'
                        : 'border-zinc-800 hover:border-zinc-700 cursor-pointer'
                  }
                `}
              >
                <Flex direction="column" gap="2">
                  <Flex align="center" gap="2">
                    <Icon className={`h-5 w-5 ${isSelected ? 'text-green-500' : 'text-zinc-400'}`} />
                    <Text size="2" weight="medium">
                      {source.name}
                    </Text>
                  </Flex>
                  <Text size="1" color="gray">
                    {source.description}
                  </Text>
                  {isDisabled && (
                    <Text size="1" className="text-yellow-500 mt-1">
                      Coming Soon
                    </Text>
                  )}
                </Flex>
              </button>
            )
          })}
        </Grid>
      </Flex>
    </Card>
  )
}
