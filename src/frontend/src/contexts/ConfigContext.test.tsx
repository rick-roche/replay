import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { ConfigProvider, useConfig } from './ConfigContext'
import * as apiModule from '@/api/config'

vi.mock('@/api/config', () => ({
  configApi: {
    configureLastfm: vi.fn(),
    configureDiscogs: vi.fn()
  }
}))

const { configApi } = apiModule

function wrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>{children}</ConfigProvider>
    </Theme>
  )
}

describe('ConfigContext', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('loads configs and filter from localStorage', () => {
    const lastfmCfg = { username: 'alice', playCount: 100, isConfigured: true }
    const discogsCfg = { username: 'collector', collectionUrl: 'https://discogs.com/users/collector/collection', releaseCount: 12, isConfigured: true }
    const flt = { dataType: 'Albums', timePeriod: 'Last3Months', maxResults: 75 }
    localStorage.setItem('replay:lastfm_config', JSON.stringify(lastfmCfg))
    localStorage.setItem('replay:discogs_config', JSON.stringify(discogsCfg))
    localStorage.setItem('replay:lastfm_filter', JSON.stringify(flt))

    const { result } = renderHook(() => useConfig(), { wrapper })
    expect(result.current.lastfmConfig).toEqual(lastfmCfg)
    expect(result.current.discogsConfig).toEqual(discogsCfg)
    expect(result.current.lastfmFilter).toEqual(expect.objectContaining(flt))
  })

  it('configureLastfm sets state and persists to localStorage', async () => {
    vi.mocked(configApi.configureLastfm).mockResolvedValue({ username: 'bob', playCount: 10, isConfigured: true })

    const { result } = renderHook(() => useConfig(), { wrapper })
    await act(async () => {
      await result.current.configureLastfm('bob')
    })

    expect(result.current.lastfmConfig?.username).toBe('bob')
    expect(localStorage.getItem('replay:lastfm_config')).toContain('bob')
  })

  it('configureLastfm sets error on failure', async () => {
    vi.mocked(configApi.configureLastfm).mockRejectedValue(new Error('Bad'))

    const { result } = renderHook(() => useConfig(), { wrapper })
    await act(async () => {
      await result.current.configureLastfm('bad')
    })

    expect(result.current.error).toBe('Bad')
    act(() => result.current.clearError())
    expect(result.current.error).toBeNull()
  })

  it('updateFilter merges and persists changes', () => {
    const { result } = renderHook(() => useConfig(), { wrapper })
    act(() => {
      result.current.updateFilter({ maxResults: 123 })
    })

    expect(result.current.lastfmFilter.maxResults).toBe(123)
    expect(localStorage.getItem('replay:lastfm_filter')).toContain('123')
  })

  it('configureDiscogs stores configuration locally', async () => {
    const discogsResponse = {
      username: 'collector',
      collectionUrl: 'https://discogs.com/users/collector/collection',
      releaseCount: 42,
      isConfigured: true
    }
    vi.mocked(configApi.configureDiscogs).mockResolvedValue(discogsResponse)

    const { result } = renderHook(() => useConfig(), { wrapper })
    await act(async () => {
      await result.current.configureDiscogs('collector')
    })

    expect(result.current.discogsConfig).toEqual(discogsResponse)
    expect(localStorage.getItem('replay:discogs_config')).toContain('collector')
  })

  it('configureDiscogs sets error on failure', async () => {
    vi.mocked(configApi.configureDiscogs).mockRejectedValue(new Error('Discogs error'))

    const { result } = renderHook(() => useConfig(), { wrapper })
    await act(async () => {
      await result.current.configureDiscogs('bad')
    })

    expect(result.current.error).toBe('Discogs error')
  })
})
