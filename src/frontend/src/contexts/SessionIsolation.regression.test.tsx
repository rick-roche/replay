import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, it, vi } from 'vitest'
import { AuthProvider, useAuth } from './AuthContext'
import { ConfigProvider, useConfig } from './ConfigContext'
import { MatchProvider, useMatch } from './MatchContext'
import { authApi } from '../api/auth'
import { configApi } from '../api/config'
import { matchApi } from '../api/match'
vi.mock('../api/auth')
vi.mock('../api/config')
vi.mock('../api/match')
function UserState() {
  const { user, logout } = useAuth()
  const { configureLastfm, lastfmConfig } = useConfig()
  const { matchTracks, matchedData } = useMatch()
  return <>
    <button onClick={async () => { await configureLastfm('alice'); await matchTracks([{ name: 'Private track', artist: 'Artist', source: 'lastfm' }]) }}>Load private data</button>
    <button onClick={logout}>Logout</button>
    <output>{user?.id ?? 'anonymous'}|{lastfmConfig?.isConfigured ? 'configured' : 'unconfigured'}|{matchedData ? 'matched' : 'empty'}</output>
  </>
}
it('clears downstream contexts on logout without losing the saved profile input', async () => {
  localStorage.clear()
  vi.mocked(authApi.getSession).mockResolvedValue({ sessionId: 's', user: { id: 'alice', displayName: 'Alice', email: '', imageUrl: null }, expiresAt: new Date(Date.now() + 3600000).toISOString() })
  vi.mocked(authApi.logout).mockResolvedValue(undefined)
  vi.mocked(configApi.configureLastfm).mockResolvedValue({ username: 'alice', profileUrl: '', playCount: 1, isConfigured: true })
  vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue({ tracks: [{ sourceTrack: { name: 'Private track', artist: 'Artist', source: 'lastfm' } }] })
  render(<AuthProvider><ConfigProvider><MatchProvider><UserState /></MatchProvider></ConfigProvider></AuthProvider>)
  const user = userEvent.setup()
  await screen.findByText('alice|unconfigured|empty')
  await user.click(screen.getByRole('button', { name: 'Load private data' }))
  await screen.findByText('alice|configured|matched')
  await user.click(screen.getByRole('button', { name: 'Logout' }))
  await screen.findByText('anonymous|unconfigured|empty')
  expect(JSON.parse(localStorage.getItem('replay:lastfm_config')!)).toMatchObject({ username: 'alice', isConfigured: false })
})
