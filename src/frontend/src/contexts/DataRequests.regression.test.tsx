import { act, renderHook } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { DataProvider, useData } from './DataContext'
import { sourcesApi } from '../api/sources'
import type { components } from '../api/generated-client'
vi.mock('../api/sources')
type Data = components['schemas']['NormalizedDataResponse']
const filter = { dataType: 'Tracks' as const, timePeriod: 'Last12Months' as const, maxResults: 50 }
const data: Data = { dataType: 'Tracks', tracks: [], albums: [], artists: [] }
describe('data request invalidation', () => {
  it('ignores a cleared data fetch', async () => {
    let resolve!: (value: Data) => void
    vi.mocked(sourcesApi.fetchLastfmData).mockReturnValueOnce(new Promise(done => { resolve = done }))
    const { result } = renderHook(useData, { wrapper: DataProvider })
    let pending!: Promise<void>
    act(() => { pending = result.current.fetchData('alice', filter) })
    act(() => result.current.clearData())
    await act(async () => { resolve(data); await pending })
    expect(result.current.normalizedData).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })
  it('invalidates an outstanding concerts fetch on clear', async () => {
    let resolve!: (value: Awaited<ReturnType<typeof sourcesApi.fetchSetlistFmConcerts>>) => void
    vi.mocked(sourcesApi.fetchSetlistFmConcerts).mockReturnValueOnce(new Promise(done => { resolve = done }))
    const { result } = renderHook(useData, { wrapper: DataProvider })
    let pending!: Promise<void>
    act(() => { pending = result.current.fetchSetlistFmConcerts('alice', {}, 1) })
    act(() => result.current.clearData())
    await act(async () => { resolve({ concerts: [], pageNumber: 1, totalConcerts: 0, pageSize: 20 }); await pending })
    expect(result.current.setlistConcertsPage).toBeNull()
    expect(result.current.isLoadingConcerts).toBe(false)
  })
  it('keeps the later data fetch when the earlier response arrives last', async () => {
    let resolve!: (value: Data) => void
    vi.mocked(sourcesApi.fetchLastfmData).mockReturnValueOnce(new Promise(done => { resolve = done })).mockResolvedValueOnce({ ...data, tracks: [{ name: 'New', artist: 'Artist', source: 'lastfm', sourceMetadata: {} }] })
    const { result } = renderHook(useData, { wrapper: DataProvider })
    let pending!: Promise<void>
    act(() => { pending = result.current.fetchData('alice', filter) })
    await act(async () => result.current.fetchData('bob', filter))
    await act(async () => { resolve(data); await pending })
    expect(result.current.normalizedData?.tracks[0]?.name).toBe('New')
  })
})
