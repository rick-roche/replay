import { useState } from 'react'
import { Box, Button, Card, Flex, Heading, Switch, Text } from '@radix-ui/themes'
import { ChevronDown, ChevronUp } from 'lucide-react'
import { useConfig } from '../contexts/ConfigContext'

export function AdvancedOptions() {
  const [isOpen, setIsOpen] = useState(false)
  const { autoFetch, setAutoFetch } = useConfig()

  return (
    <Card>
      <Flex direction="column" gap="4">
        <Button
          variant="ghost"
          onClick={() => setIsOpen(!isOpen)}
          style={{ width: '100%', justifyContent: 'space-between' }}
        >
          <Heading size="4" weight="medium">
            Advanced Options
          </Heading>
          {isOpen ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
        </Button>

        {isOpen && (
          <Flex direction="column" gap="3">
            <Box className="border-t border-zinc-800 pt-3">
              <Flex direction="column" gap="2">
                <Flex align="center" justify="between">
                  <Flex direction="column" gap="1">
                    <Text size="2" weight="medium">
                      Auto-Fetch & Match
                    </Text>
                    <Text size="1" color="gray">
                      {autoFetch
                        ? 'Fetch and match will happen automatically after configuring filters'
                        : 'You can manually trigger fetch and match steps'}
                    </Text>
                  </Flex>
                  <Switch
                    checked={autoFetch}
                    onCheckedChange={setAutoFetch}
                  />
                </Flex>
              </Flex>
            </Box>
          </Flex>
        )}
      </Flex>
    </Card>
  )
}
