import { describe, it, expect, beforeEach, vi } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { ReactNode } from 'react';
import { PlaylistProvider, usePlaylist } from '@/contexts/PlaylistContext';

function Wrapper({ children }: { children: ReactNode }) {
  return <PlaylistProvider>{children}</PlaylistProvider>;
}

describe('PlaylistContext', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  it('should provide default config on initial mount', () => {
    const { result } = renderHook(() => usePlaylist(), { wrapper: Wrapper });

    expect(result.current.config.name).toBe('My Re:Play Playlist');
    expect(result.current.config.description).toBe('Created with Re:Play');
    expect(result.current.config.isPublic).toBe(false);
  });

  it('should load persisted config from localStorage', () => {
    const persistedConfig = {
      name: 'Custom Playlist',
      description: 'Custom Description',
      isPublic: true,
    };
    localStorage.setItem('replay:playlist_config', JSON.stringify(persistedConfig));

    const { result } = renderHook(() => usePlaylist(), { wrapper: Wrapper });

    expect(result.current.config).toEqual(persistedConfig);
  });

  it('should update name and persist to localStorage', () => {
    const { result } = renderHook(() => usePlaylist(), { wrapper: Wrapper });

    act(() => {
      result.current.updateName('New Playlist Name');
    });

    expect(result.current.config.name).toBe('New Playlist Name');
    expect(JSON.parse(localStorage.getItem('replay:playlist_config') || '{}')).toEqual(
      expect.objectContaining({ name: 'New Playlist Name' })
    );
  });

  it('should update description and persist to localStorage', () => {
    const { result } = renderHook(() => usePlaylist(), { wrapper: Wrapper });

    act(() => {
      result.current.updateDescription('New Description');
    });

    expect(result.current.config.description).toBe('New Description');
    expect(JSON.parse(localStorage.getItem('replay:playlist_config') || '{}')).toEqual(
      expect.objectContaining({ description: 'New Description' })
    );
  });

  it('should toggle isPublic and persist to localStorage', () => {
    const { result } = renderHook(() => usePlaylist(), { wrapper: Wrapper });

    act(() => {
      result.current.updateIsPublic(true);
    });

    expect(result.current.config.isPublic).toBe(true);
    expect(JSON.parse(localStorage.getItem('replay:playlist_config') || '{}')).toEqual(
      expect.objectContaining({ isPublic: true })
    );
  });

  it('should clear config and localStorage', () => {
    const { result } = renderHook(() => usePlaylist(), { wrapper: Wrapper });

    act(() => {
      result.current.updateName('Custom Name');
    });

    expect(localStorage.getItem('replay:playlist_config')).toBeTruthy();

    act(() => {
      result.current.clearConfig();
    });

    expect(result.current.config.name).toBe('My Re:Play Playlist');
    expect(localStorage.getItem('replay:playlist_config')).toBeNull();
  });

  it('should update entire config at once', () => {
    const { result } = renderHook(() => usePlaylist(), { wrapper: Wrapper });

    const newConfig = {
      name: 'Batch Updated',
      description: 'Batch Desc',
      isPublic: true,
    };

    act(() => {
      result.current.updateConfig(newConfig);
    });

    expect(result.current.config).toEqual(newConfig);
    expect(JSON.parse(localStorage.getItem('replay:playlist_config') || '{}')).toEqual(
      newConfig
    );
  });
});
