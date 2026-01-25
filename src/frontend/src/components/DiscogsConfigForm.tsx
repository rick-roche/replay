import { useState } from 'react'
import { Button, TextField, Text, Flex, Box, Card, Heading, Spinner, Link } from '@radix-ui/themes'
import { AlertCircle, CheckCircle2, Edit2 } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'

export function DiscogsConfigForm() {
  const { discogsConfig, isLoading, error, configureDiscogs, clearError } = useConfig()
  const [identifier, setIdentifier] = useState('')
  const [isEditing, setIsEditing] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!identifier.trim()) return

    clearError()
    await configureDiscogs(identifier.trim())
    setIsEditing(false)
  }

  if (discogsConfig?.isConfigured && !isEditing) {
    return (
      <Card>
        <Flex direction="column" gap="4">
          <Box>
            <Heading size="4" weight="medium" mb="2">
              Discogs Profile
            </Heading>
          </Box>

          <Flex align="center" gap="2" className="text-green-500">
            <CheckCircle2 className="h-5 w-5" />
            <Flex direction="column" gap="1" width="100%">
              <Text size="2" weight="medium">
                {discogsConfig.username}
              </Text>
              <Text size="1" color="gray">
                {discogsConfig.releaseCount.toLocaleString()} releases linked
              </Text>
              <Text size="1" color="gray">
                <Link href={discogsConfig.collectionUrl} target="_blank" rel="noreferrer">
                  View collection
                </Link>
              </Text>
            </Flex>
          </Flex>

          <Button
            variant="outline"
            onClick={() => {
              setIsEditing(true)
              setIdentifier(discogsConfig.username)
            }}
          >
            <Edit2 className="h-4 w-4" />
            Change Profile
          </Button>
        </Flex>
      </Card>
    )
  }

  return (
    <Card>
      <Flex direction="column" gap="4">
        <Box>
          <Heading size="4" weight="medium" mb="2">
            Discogs Profile
          </Heading>
          <Text size="2" color="gray">
            Link your Discogs username or collection ID
          </Text>
        </Box>

        <form onSubmit={handleSubmit}>
          <Flex direction="column" gap="3">
            <Box>
              <TextField.Root
                placeholder="Enter Discogs username or collection ID"
                value={identifier}
                onChange={(e) => {
                  setIdentifier(e.target.value)
                  clearError()
                }}
                disabled={isLoading}
              />
            </Box>

            {error && (
              <Flex align="center" gap="2" className="text-red-500">
                <AlertCircle className="h-4 w-4" />
                <Text size="2">{error}</Text>
              </Flex>
            )}

            <Button
              type="submit"
              disabled={!identifier.trim() || isLoading}
            >
              {isLoading ? <Spinner /> : 'Connect Discogs'}
            </Button>
          </Flex>
        </form>
      </Flex>
    </Card>
  )
}
