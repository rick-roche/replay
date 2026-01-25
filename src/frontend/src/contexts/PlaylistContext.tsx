import {
  createContext,
  useContext,
  useState,
  useCallback,
  useEffect,
  type ReactNode,
} from 'react';

interface PlaylistConfig {
  name: string;
  description: string;
  isPublic: boolean;
}

interface PlaylistContextType {
  config: PlaylistConfig;
  updateName: (name: string) => void;
  updateDescription: (description: string) => void;
  updateIsPublic: (isPublic: boolean) => void;
  updateConfig: (config: PlaylistConfig) => void;
  clearConfig: () => void;
}

const defaultConfig: PlaylistConfig = {
  name: 'My Re:Play Playlist',
  description: 'Created with Re:Play',
  isPublic: false,
};

const PlaylistContext = createContext<PlaylistContextType | undefined>(undefined);

export function PlaylistProvider({ children }: { children: ReactNode }) {
  const [config, setConfig] = useState<PlaylistConfig>(() => {
    // Load from localStorage on mount
    const stored = localStorage.getItem('replay:playlist_config');
    return stored ? JSON.parse(stored) : defaultConfig;
  });

  // Persist to localStorage whenever config changes (except when at default)
  useEffect(() => {
    // If config is at default, remove from localStorage
    if (
      config.name === defaultConfig.name &&
      config.description === defaultConfig.description &&
      config.isPublic === defaultConfig.isPublic
    ) {
      localStorage.removeItem('replay:playlist_config');
    } else {
      localStorage.setItem('replay:playlist_config', JSON.stringify(config));
    }
  }, [config]);

  const updateName = useCallback((name: string) => {
    setConfig((prev) => ({ ...prev, name }));
  }, []);

  const updateDescription = useCallback((description: string) => {
    setConfig((prev) => ({ ...prev, description }));
  }, []);

  const updateIsPublic = useCallback((isPublic: boolean) => {
    setConfig((prev) => ({ ...prev, isPublic }));
  }, []);

  const updateConfig = useCallback((newConfig: PlaylistConfig) => {
    setConfig(newConfig);
  }, []);

  const clearConfig = useCallback(() => {
    setConfig(defaultConfig);
  }, []);

  return (
    <PlaylistContext.Provider
      value={{
        config,
        updateName,
        updateDescription,
        updateIsPublic,
        updateConfig,
        clearConfig,
      }}
    >
      {children}
    </PlaylistContext.Provider>
  );
}

export function usePlaylist() {
  const context = useContext(PlaylistContext);
  if (!context) {
    throw new Error('usePlaylist must be used within a PlaylistProvider');
  }
  return context;
}
