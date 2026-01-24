import { useState, useCallback } from 'react';
import * as Dialog from '@radix-ui/react-dialog';
import { X, Search, Loader2 } from 'lucide-react';
import type { components } from '@/api/generated-client';

type SpotifyTrack = components['schemas']['SpotifyTrack'];

interface ManualSearchModalProps {
  open: boolean;
  onClose: () => void;
  onSelect: (track: SpotifyTrack) => void;
  onSearch: (query: string) => Promise<SpotifyTrack[]>;
  initialQuery?: string;
}

export function ManualSearchModal({
  open,
  onClose,
  onSelect,
  onSearch,
  initialQuery = '',
}: ManualSearchModalProps) {
  const [query, setQuery] = useState(initialQuery);
  const [results, setResults] = useState<SpotifyTrack[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSearch = useCallback(
    async (e: React.FormEvent<HTMLFormElement>) => {
      e.preventDefault();
      if (!query.trim()) {
        setError('Please enter a search query');
        return;
      }

      setIsSearching(true);
      setError(null);

      try {
        const searchResults = await onSearch(query);
        setResults(searchResults);
        if (searchResults.length === 0) {
          setError('No tracks found');
        }
      } catch (err) {
        const errorMessage =
          err instanceof Error ? err.message : 'Search failed';
        setError(errorMessage);
      } finally {
        setIsSearching(false);
      }
    },
    [query, onSearch]
  );

  const handleSelectTrack = (track: SpotifyTrack) => {
    onSelect(track);
    onClose();
  };

  return (
    <Dialog.Root open={open} onOpenChange={onClose}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-lg border border-zinc-700 bg-zinc-900 p-6 shadow-lg">
          <Dialog.Title className="text-lg font-semibold text-white">
            Search Spotify
          </Dialog.Title>

          <form onSubmit={handleSearch} className="mt-4 flex gap-2">
            <input
              type="text"
              placeholder="Track name, artist..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="flex-1 rounded border border-zinc-700 bg-zinc-800 px-3 py-2 text-white placeholder-zinc-500 focus:border-green-500 focus:outline-none"
              disabled={isSearching}
            />
            <button
              type="submit"
              disabled={isSearching}
              className="inline-flex items-center gap-2 rounded bg-green-600 px-4 py-2 font-medium text-white hover:bg-green-700 disabled:opacity-50"
            >
              {isSearching ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Search className="h-4 w-4" />
              )}
            </button>
          </form>

          {error && (
            <div className="mt-3 rounded bg-red-900/20 p-2 text-sm text-red-400">
              {error}
            </div>
          )}

          <div className="mt-4 max-h-80 space-y-2 overflow-y-auto">
            {results.map((track) => (
              <button
                key={track.id}
                onClick={() => handleSelectTrack(track)}
                className="w-full rounded border border-zinc-700 bg-zinc-800 p-3 text-left transition hover:bg-zinc-700"
              >
                <div className="font-medium text-white">{track.name}</div>
                <div className="text-sm text-zinc-400">
                  {track.artist}
                  {track.album && ` • ${track.album}`}
                </div>
              </button>
            ))}
          </div>

          <Dialog.Close asChild>
            <button
              aria-label="Close"
              className="absolute right-4 top-4 text-zinc-400 hover:text-white"
            >
              <X className="h-4 w-4" />
            </button>
          </Dialog.Close>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
