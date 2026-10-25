import { renderHook } from '@testing-library/react'
import { afterEach, expect, it } from 'vitest'
import { PlaylistProvider, usePlaylist } from './PlaylistContext'
import { ConfigProvider, useConfig } from './ConfigContext'
afterEach(() => localStorage.clear())
it.each(['{broken', 'null', '{"name":42,"isPublic":"yes"}'])('recovers from malformed playlist preferences: %s', stored => {
  localStorage.setItem('replay:playlist_config', stored)
  const { result } = renderHook(usePlaylist, { wrapper: PlaylistProvider })
  expect(result.current.config).toEqual({ name: 'My Re:Play Playlist', description: 'Created with Re:Play', isPublic: false })
})
it.each(['null', '[]', '{"dataType":"bogus","timePeriod":42}'])('ignores invalid stored filters: %s', stored => {
  localStorage.setItem('replay:lastfm_filter', stored)
  const { result } = renderHook(useConfig, { wrapper: ConfigProvider })
  expect(result.current.lastfmFilter).toMatchObject({ dataType: 'Tracks', timePeriod: 'Last12Months', maxResults: 50 })
})
