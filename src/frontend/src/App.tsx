import { Music2, LogOut, User as UserIcon } from 'lucide-react'
import { Button, Container, Flex, Heading, Text, Box, Avatar, DropdownMenu, Spinner } from '@radix-ui/themes'
import { Routes, Route, Link } from 'react-router-dom'
import { useAuth } from './contexts/AuthContext'
import { Home } from './pages/Home'
import { About } from './pages/About'

function App() {
  const { user, isLoading, isAuthenticated, login, logout } = useAuth()

  return (
    <Box className="min-h-screen">
      {/* Navigation */}
      <Box className="border-b border-zinc-800">
        <Container size="4">
          <Flex justify="between" align="center" py="4">
            <Flex align="center" gap="2" asChild>
              <Link to="/">
                <Music2 className="h-8 w-8 text-green-500" />
                <Heading size="6" className="bg-linear-to-r from-green-400 to-green-600 bg-clip-text text-transparent">
                  Re:Play
                </Heading>
              </Link>
            </Flex>
            <Flex align="center" gap="4">
              {isLoading ? (
                <Spinner />
              ) : isAuthenticated && user ? (
                <DropdownMenu.Root>
                  <DropdownMenu.Trigger>
                    <Button variant="ghost" size="2">
                      <Flex align="center" gap="2">
                        <Avatar
                          size="1"
                          src={user.imageUrl || undefined}
                          fallback={user.displayName.charAt(0)}
                        />
                        <Text size="2">{user.displayName}</Text>
                      </Flex>
                    </Button>
                  </DropdownMenu.Trigger>
                  <DropdownMenu.Content>
                    <DropdownMenu.Item onClick={() => logout()}>
                      <Flex align="center" gap="2">
                        <LogOut className="h-4 w-4" />
                        Logout
                      </Flex>
                    </DropdownMenu.Item>
                  </DropdownMenu.Content>
                </DropdownMenu.Root>
              ) : (
                <Button size="2" onClick={login}>
                  <UserIcon className="h-4 w-4" />
                  Connect Spotify
                </Button>
              )}
            </Flex>
          </Flex>
        </Container>
      </Box>

      {/* Main Content */}
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/about" element={<About />} />
      </Routes>

      {/* Footer */}
      <Box className="border-t border-zinc-800 mt-24">
        <Container size="4">
          <Flex 
            direction={{ initial: 'column', md: 'row' }} 
            justify="between" 
            align="center" 
            gap="4" 
            py="6"
          >
            <Flex align="center" gap="2">
              <Music2 className="h-4 w-4" />
              <Text size="2" color="gray">
                Re:Play — Your music, replayed your way
              </Text>
            </Flex>
            <Flex gap="6">
              <Text size="2" asChild>
                <Link 
                  to="/about"
                  className="hover:text-white transition-colors"
                >
                  About
                </Link>
              </Text>
              <Text size="2" asChild>
                <a 
                  href="https://github.com/rick-roche/replay" 
                  target="_blank" 
                  rel="noopener noreferrer"
                  className="hover:text-white transition-colors"
                >
                  GitHub
                </a>
              </Text>
            </Flex>
          </Flex>
        </Container>
      </Box>
    </Box>
  )
}

export default App
