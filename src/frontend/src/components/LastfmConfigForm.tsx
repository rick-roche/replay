import { useState } from 'react'
import { Button, TextField, Text, Flex, Box, Card, Heading, Spinner } from '@radix-ui/themes'
import { AlertCircle, CheckCircle2, Edit2 } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'

export function LastfmConfigForm() {
  const { lastfmConfig, isLoading, error, configureLastfm, clearError } = useConfig()
  const [username, setUsername] = useState('')
  const [isEditing, setIsEditing] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!username.trim()) return

    clearError()
    await configureLastfm(username.trim())
    setIsEditing(false)
  }

  if (lastfmConfig?.isConfigured && !isEditing) {
    return (
      <Card>
        <Flex direction="column" gap="4">
          <Box>
            <Heading size="4" weight="medium" mb="2">
              Last.fm Profile
            </Heading>
          </Box>

          <Flex align="center" gap="2" className="text-green-500">
            <CheckCircle2 className="h-5 w-5" />
            <Flex direction="column" gap="1" width="100%">
              <Text size="2" weight="medium">
                {lastfmConfig.username}
              </Text>
              <Text size="1" color="gray">
                {lastfmConfig.playCount.toLocaleString()} total scrobbles
              </Text>
            </Flex>
          </Flex>

          <Button
            variant="outline"
            onClick={() => {
              setIsEditing(true)
              setUsername(lastfmConfig.username)
            }}
          >
            <Edit2 className="h-4 w-4" />
            Change Username
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
            Last.fm Profile
          </Heading>
          <Text size="2" color="gray">
            Connect your Last.fm account to access your listening history
          </Text>
        </Box>

        <form onSubmit={handleSubmit}>
          <Flex direction="column" gap="3">
            <Box>
              <TextField.Root
                placeholder="Enter your Last.fm username"
                value={username}
                onChange={(e) => {
                  setUsername(e.target.value)
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
              disabled={!username.trim() || isLoading}
            >
              {isLoading ? <Spinner /> : 'Connect Last.fm'}
            </Button>
          </Flex>
        </form>
      </Flex>
    </Card>
  )
}
