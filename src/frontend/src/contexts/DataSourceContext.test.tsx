import { describe, it, expect, beforeEach, vi } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { DataSourceProvider, useDataSource } from '@/contexts/DataSourceContext'
import { DataSource } from '@/types/datasource'

describe('DataSourceContext extended', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('should initialize with null source in localStorage', () => {
    const { result } = renderHook(() => useDataSource(), {
      wrapper: DataSourceProvider
    })

    expect(result.current.selectedSource).toBeNull()
  })

  it('should select Last.fm source', () => {
    const { result } = renderHook(() => useDataSource(), {
      wrapper: DataSourceProvider
    })

    act(() => {
      result.current.selectSource(DataSource.LASTFM)
    })

    expect(result.current.selectedSource).toBe(DataSource.LASTFM)
  })

  it('should select Setlist.fm source', () => {
    const { result } = renderHook(() => useDataSource(), {
      wrapper: DataSourceProvider
    })

    act(() => {
      result.current.selectSource(DataSource.SETLISTFM)
    })

    expect(result.current.selectedSource).toBe(DataSource.SETLISTFM)
  })

  it('should select Discogs source', () => {
    const { result } = renderHook(() => useDataSource(), {
      wrapper: DataSourceProvider
    })

    act(() => {
      result.current.selectSource(DataSource.DISCOGS)
    })

    expect(result.current.selectedSource).toBe(DataSource.DISCOGS)
  })

  it('should persist source to localStorage on select', () => {
    const { result } = renderHook(() => useDataSource(), {
      wrapper: DataSourceProvider
    })

    act(() => {
      result.current.selectSource(DataSource.LASTFM)
    })

    expect(localStorage.getItem('replay:selected_source')).toBe(DataSource.LASTFM)
  })

  it('should clear source', () => {
    const { result } = renderHook(() => useDataSource(), {
      wrapper: DataSourceProvider
    })

    act(() => {
      result.current.selectSource(DataSource.LASTFM)
    })

    expect(result.current.selectedSource).toBe(DataSource.LASTFM)

    act(() => {
      result.current.clearSource()
    })

    expect(result.current.selectedSource).toBeNull()
    expect(localStorage.getItem('replay:selected_source')).toBeNull()
  })

  it('should load source from localStorage on init', () => {
    localStorage.setItem('replay:selected_source', DataSource.SETLISTFM)

    const { result } = renderHook(() => useDataSource(), {
      wrapper: DataSourceProvider
    })

    // After mount, should have loaded from localStorage
    expect(result.current.selectedSource).toBe(DataSource.SETLISTFM)
  })

  it('should handle invalid localStorage values', () => {
    localStorage.setItem('replay:selected_source', 'invalid-source')

    const { result } = renderHook(() => useDataSource(), {
      wrapper: DataSourceProvider
    })

    // Should ignore invalid value
    expect(result.current.selectedSource).toBeNull()
  })

  it('should switch between sources', () => {
    const { result } = renderHook(() => useDataSource(), {
      wrapper: DataSourceProvider
    })

    act(() => {
      result.current.selectSource(DataSource.LASTFM)
    })

    expect(result.current.selectedSource).toBe(DataSource.LASTFM)

    act(() => {
      result.current.selectSource(DataSource.SETLISTFM)
    })

    expect(result.current.selectedSource).toBe(DataSource.SETLISTFM)
    expect(localStorage.getItem('replay:selected_source')).toBe(DataSource.SETLISTFM)
  })
})
