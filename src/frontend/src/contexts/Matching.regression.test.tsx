import { act, renderHook } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { MatchProvider, useMatch } from './MatchContext'
import { matchApi } from '../api/match'
import type { components } from '../api/generated-client'

vi.mock('../api/match')
type Response = components['schemas']['MatchedDataResponse']
const source = (name: string) => ({ name, artist: 'Artist', source: 'lastfm', sourceMetadata: {} })
const match = (name: string) => ({ spotifyId: name, name, artist: 'Artist', uri: `spotify:track:${name}`, confidence: 100, method: 'Exact' as const })
function deferred<T>() {
  let resolve!: (value: T) => void
  const promise = new Promise<T>(done => { resolve = done })
  return { promise, resolve }
}

describe('editable matching results', () => {
  it.each(['albums', 'artists'] as const)('normalizes %s into editable playlist tracks', async mode => {
    const tracks = [match('One'), match('Two')]
    vi.mocked(matchApi.matchAlbumsToSpotify).mockResolvedValue({ albums: [{ sourceAlbum: { ...source('Album'), tracks: [] }, match: { ...match('Album'), tracks } }] })
    vi.mocked(matchApi.matchArtistsToSpotify).mockResolvedValue({ artists: [{ sourceArtist: source('Artist'), match: { ...match('Artist'), topTracks: tracks } }] })
    const { result } = renderHook(useMatch, { wrapper: MatchProvider })
    await act(async () => {
      if (mode === 'albums') await result.current.matchAlbums([{ ...source('Album'), tracks: [] }])
      else await result.current.matchArtists([source('Artist')])
    })
    expect(result.current.matchedData?.tracks.map(t => t.match?.uri)).toEqual(['spotify:track:One', 'spotify:track:Two'])
    act(() => result.current.removeTrack(0))
    expect(result.current.matchedData?.tracks.map(t => t.match?.uri)).toEqual(['spotify:track:Two'])
  })

  it('does not restore results when a cleared match request completes', async () => {
    const pending = deferred<Response>()
    vi.mocked(matchApi.matchTracksToSpotify).mockReturnValueOnce(pending.promise)
    const { result } = renderHook(useMatch, { wrapper: MatchProvider })
    let request!: Promise<void>
    act(() => { request = result.current.matchTracks([source('One')]) })
    act(() => result.current.clearMatches())
    await act(async () => { pending.resolve({ tracks: [{ sourceTrack: source('One'), match: match('One') }] }); await request })
    expect(result.current.matchedData).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('retries the original item after reordering without restoring removed tracks', async () => {
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValueOnce({ tracks: ['One', 'Two', 'Three'].map(name => ({ sourceTrack: source(name) })) })
    const { result } = renderHook(useMatch, { wrapper: MatchProvider })
    await act(async () => result.current.matchTracks(['One', 'Two', 'Three'].map(source)))
    const pending = deferred<Response>()
    vi.mocked(matchApi.matchTracksToSpotify).mockReturnValueOnce(pending.promise)
    let request!: Promise<void>
    act(() => { request = result.current.retryMatch(0) })
    act(() => { result.current.moveTrack(0, 2); result.current.removeTrack(0) })
    await act(async () => { pending.resolve({ tracks: [{ sourceTrack: source('One'), match: match('One') }] }); await request })
    expect(result.current.matchedData?.tracks.map(t => [t.sourceTrack.name, t.match?.uri])).toEqual([['Three', undefined], ['One', 'spotify:track:One']])
  })

  it('does not resurrect a retry after clearing the workflow', async () => {
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValueOnce({ tracks: [{ sourceTrack: source('One') }] })
    const { result } = renderHook(useMatch, { wrapper: MatchProvider })
    await act(async () => result.current.matchTracks([source('One')]))
    const pending = deferred<Response>()
    vi.mocked(matchApi.matchTracksToSpotify).mockReturnValueOnce(pending.promise)
    let request!: Promise<void>
    act(() => { request = result.current.retryMatch(0) })
    act(() => result.current.clearMatches())
    await act(async () => { pending.resolve({ tracks: [{ sourceTrack: source('One'), match: match('One') }] }); await request })
    expect(result.current.matchedData).toBeNull()
    expect(result.current.error).toBeNull()
  })
})
