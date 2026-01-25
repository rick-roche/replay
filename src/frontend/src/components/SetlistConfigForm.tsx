import { useState } from 'react'
import { Button, TextField, Text, Flex, Box, Card, Heading, Spinner, Link } from '@radix-ui/themes'
import { AlertCircle, CheckCircle2, Edit2 } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'

export function SetlistConfigForm() {
  const { setlistConfig, isLoading, error, configureSetlistFm, clearError } = useConfig()
  const [usernameOrId, setUsernameOrId] = useState('')
  const [isEditing, setIsEditing] = useState(false)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!usernameOrId.trim()) return

    clearError()
    await configureSetlistFm(usernameOrId.trim())
    setIsEditing(false)
  }

  if (setlistConfig?.isConfigured && !isEditing) {
    return (
      <Card>
        <Flex direction="column" gap="4">
          <Box>
            <Heading size="4" weight="medium" mb="2">
              Setlist.fm Profile
            </Heading>
          </Box>

          <Flex align="center" gap="2" className="text-green-500">
            <CheckCircle2 className="h-5 w-5" />
            <Flex direction="column" gap="1" width="100%">
              <Text size="2" weight="medium">
                {setlistConfig.displayName}
              </Text>
              <Text size="1" color="gray">
                {setlistConfig.attendedConcerts} concerts attended
              </Text>
              {setlistConfig.profileUrl && (
                <Text size="1" color="gray">
                  <Link href={setlistConfig.profileUrl} target="_blank" rel="noreferrer">
                    View profile
                  </Link>
                </Text>
              )}
            </Flex>
          </Flex>

          <Button
            variant="outline"
            onClick={() => {
              setIsEditing(true)
              setUsernameOrId(setlistConfig.userId)
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
            Setlist.fm Profile
          </Heading>
          <Text size="2" color="gray">
            Link your Setlist.fm username or user ID
          </Text>
        </Box>

        <form onSubmit={handleSubmit}>
          <Flex direction="column" gap="3">
            <Box>
              <TextField.Root
                placeholder="Enter Setlist.fm username or user ID"
                value={usernameOrId}
                onChange={(e) => {
                  setUsernameOrId(e.target.value)
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
              disabled={!usernameOrId.trim() || isLoading}
            >
              {isLoading ? <Spinner /> : 'Connect Setlist.fm'}
            </Button>
          </Flex>
        </form>
      </Flex>
    </Card>
  )
}
