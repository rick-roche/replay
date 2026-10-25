import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { Theme } from '@radix-ui/themes'
import { matchApi } from '../api/match'
import { MatchProvider, useMatch } from '../contexts/MatchContext'
import { PlaylistProvider } from '../contexts/PlaylistContext'
import { WorkflowProvider, useWorkflow, WorkflowStep, STEP_ORDER } from '../contexts/WorkflowContext'
import { CreatePlaylistProvider, useCreatePlaylist } from '../contexts/CreatePlaylistContext'
import { MatchResults } from './MatchResults'
import { CreatePlaylistButton } from './CreatePlaylistButton'
import { PlaylistConfirmation } from './PlaylistConfirmation'
vi.mock('../api/match')
const source = { name: 'Collection', artist: 'Artist', source: 'lastfm', sourceMetadata: {}, tracks: [] }
const tracks = ['One', 'Two', 'Three'].map(name => ({ name, artist: 'Artist', spotifyId: name, uri: `spotify:track:${name}`, confidence: 100, method: 'Exact' as const }))
function Flow({ mode, showResults = true }: { mode: 'albums' | 'artists'; showResults?: boolean }) {
  const { matchAlbums, matchArtists } = useMatch()
  return <><button onClick={() => mode === 'albums' ? matchAlbums([source]) : matchArtists([source])}>Match</button>{showResults && <MatchResults />}<CreatePlaylistButton /><PlaylistConfirmation /></>
}
function NavigationControls() {
  const { markStepComplete, goToStep, currentStep, canGoToStep } = useWorkflow()
  const { clearPlaylist } = useCreatePlaylist()
  return <>
    <button onClick={() => STEP_ORDER.forEach(markStepComplete)}>Complete steps</button>
    <button onClick={() => goToStep(WorkflowStep.CREATE)}>Visit create</button>
    <button onClick={() => { goToStep(WorkflowStep.CONFIGURE); clearPlaylist() }}>Back to configure and clear</button>
    <button disabled={!canGoToStep(WorkflowStep.FETCH_AND_MATCH)} onClick={() => goToStep(WorkflowStep.FETCH_AND_MATCH)}>Continue after reset</button>
    <output>{currentStep}</output>
  </>
}
function renderFlow(mode: 'albums' | 'artists', navigation = false) {
  // Navigation races need real creation/workflow providers, not the unrelated curation table.
  return render(<Theme><MatchProvider><PlaylistProvider><WorkflowProvider><CreatePlaylistProvider>{navigation && <NavigationControls />}<Flow mode={mode} showResults={!navigation} /></CreatePlaylistProvider></WorkflowProvider></PlaylistProvider></MatchProvider></Theme>)
}
describe('curating expanded matches into a playlist', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    vi.mocked(matchApi.matchAlbumsToSpotify).mockResolvedValue({ albums: [{ sourceAlbum: source, match: { ...tracks[0], tracks } }] })
    vi.mocked(matchApi.matchArtistsToSpotify).mockResolvedValue({ artists: [{ sourceArtist: source, match: { ...tracks[0], topTracks: tracks } }] })
  })
  it.each(['response', 'transport'] as const)('does not lock reset workflow after stale create %s completes', async outcome => {
    let resolve!: (response: Response) => void
    let reject!: (error: Error) => void
    global.fetch = vi.fn().mockReturnValue(new Promise<Response>((done, fail) => { resolve = done; reject = fail }))
    const user = userEvent.setup()
    renderFlow('albums', true)
    await user.click(screen.getByRole('button', { name: 'Match' }))
    await user.click(screen.getByRole('button', { name: 'Complete steps' }))
    await user.click(screen.getByRole('button', { name: 'Visit create' }))
    await user.click(screen.getByRole('button', { name: /Create Playlist on Spotify/ }))
    await user.click(screen.getByRole('button', { name: 'Back to configure and clear' }))
    expect(screen.getByRole('status')).toHaveTextContent('configure')
    expect(screen.getByRole('button', { name: 'Continue after reset' })).toBeEnabled()
    await act(async () => {
      if (outcome === 'response') resolve(Response.json({ playlistId: 'old', url: 'https://open.spotify.com/playlist/old', tracksAdded: 3 }))
      else reject(new Error('Connection lost'))
    })
    expect(screen.getByRole('button', { name: 'Continue after reset' })).toBeEnabled()
    await user.click(screen.getByRole('button', { name: 'Continue after reset' }))
    expect(screen.getByRole('status')).toHaveTextContent('fetch-and-match')
    expect(screen.queryByText('Playlist Created Successfully!')).not.toBeInTheDocument()
  })
  it.each(['albums', 'artists'] as const)('creates %s using the curated order and prevents duplicate creation', async mode => {
    let submitted: unknown
    global.fetch = vi.fn(async (input: RequestInfo | URL) => {
      if (!(input instanceof Request)) throw new Error('Expected generated client request')
      submitted = await input.json()
      return Response.json({ playlistId: 'created', url: 'https://open.spotify.com/playlist/created', tracksAdded: 2, isComplete: true })
    })
    const user = userEvent.setup()
    renderFlow(mode)
    await user.click(screen.getByRole('button', { name: 'Match' }))
    const moveUp = await screen.findByRole('button', { name: 'Move Three up' })
    moveUp.focus()
    await user.keyboard('{Enter}')
    const one = screen.getByText('One').closest('.rt-Box')!
    await user.click(within(one).getByRole('button', { name: 'Remove' }))
    await user.click(screen.getByRole('button', { name: /Create Playlist on Spotify/ }))
    await screen.findByText('Playlist Created Successfully!')
    expect(submitted).toMatchObject({ trackUris: ['spotify:track:Three', 'spotify:track:Two'] })
    expect(screen.getByRole('button', { name: /Create Playlist on Spotify/ })).toBeDisabled()
  })
  it('shows partial progress and a usable playlist without offering blind duplicate creation', async () => {
    global.fetch = vi.fn().mockResolvedValue(Response.json({ playlistId: 'partial', url: 'https://open.spotify.com/playlist/partial', tracksAdded: 1, isComplete: false, errorCode: 'SPOTIFY_ADD_TRACKS_FAILED', errorMessage: 'Spotify stopped accepting tracks' }, { status: 502 }))
    const user = userEvent.setup()
    renderFlow('albums')
    await user.click(screen.getByRole('button', { name: 'Match' }))
    await user.click(await screen.findByRole('button', { name: /Create Playlist on Spotify/ }))
    await waitFor(() => expect(screen.getByText(/1 track.*added/i)).toBeInTheDocument())
    expect(screen.getByRole('link', { name: /Open.*Spotify/i })).toHaveAttribute('href', 'https://open.spotify.com/playlist/partial')
    expect(screen.queryByText('Playlist Created Successfully!')).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Create Playlist on Spotify/ })).toBeDisabled()
  })
  it.each(['unconfirmed', 'transport'])('requires deliberate reset after %s creation instead of blind retry', async failure => {
    global.fetch = failure === 'transport'
      ? vi.fn().mockRejectedValue(new Error('Connection lost'))
      : vi.fn().mockResolvedValue(Response.json({ code: 'PLAYLIST_CREATION_UNCONFIRMED', message: 'Spotify may have created the playlist; check your library.' }, { status: 502 }))
    const user = userEvent.setup()
    renderFlow('albums')
    await user.click(screen.getByRole('button', { name: 'Match' }))
    await user.click(await screen.findByRole('button', { name: /Create Playlist on Spotify/ }))
    expect(await screen.findByText(/check.*Spotify library/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Create Playlist on Spotify/ })).toBeDisabled()
    expect(screen.queryByRole('link', { name: /Spotify/i })).not.toBeInTheDocument()
    expect(screen.queryByText(/Playlist ID:/)).not.toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: /Start over after checking Spotify/i }))
    expect(screen.getByRole('button', { name: /Create Playlist on Spotify/ })).toBeEnabled()
  })
})
