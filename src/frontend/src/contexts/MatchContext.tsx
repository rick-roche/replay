import {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from 'react';
import type { components } from '@/api/generated-client';
import { matchApi } from '@/api/match';

type NormalizedTrack = components['schemas']['NormalizedTrack']
type MatchedDataResponse = components['schemas']['MatchedDataResponse']

interface MatchContextType {
  matchedData: MatchedDataResponse | null;
  isLoading: boolean;
  error: string | null;
  matchTracks: (tracks: NormalizedTrack[]) => Promise<void>;
  clearMatches: () => void;
  clearError: () => void;
}

const MatchContext = createContext<MatchContextType | undefined>(undefined);

export function MatchProvider({ children }: { children: ReactNode }) {
  const [matchedData, setMatchedData] = useState<MatchedDataResponse | null>(
    null
  );
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const matchTracks = useCallback(
    async (tracks: NormalizedTrack[]) => {
      setIsLoading(true);
      setError(null);

      try {
        const result = await matchApi.matchTracksToSpotify({ tracks });
        setMatchedData(result);
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : 'Failed to match tracks';
        setError(errorMessage);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  const clearMatches = useCallback(() => {
    setMatchedData(null);
    setError(null);
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return (
    <MatchContext.Provider
      value={{
        matchedData,
        isLoading,
        error,
        matchTracks,
        clearMatches,
        clearError,
      }}
    >
      {children}
    </MatchContext.Provider>
  );
}

export function useMatch() {
  const context = useContext(MatchContext);
  if (!context) {
    throw new Error('useMatch must be used within a MatchProvider');
  }
  return context;
}
