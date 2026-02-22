import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Theme } from '@radix-ui/themes';
import { PlaylistConfigForm } from '@/components/PlaylistConfigForm';
import { PlaylistProvider } from '@/contexts/PlaylistContext';

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <PlaylistProvider>{children}</PlaylistProvider>
    </Theme>
  );
}

describe('PlaylistConfigForm', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
  });

  it('should render the form with expanded state by default', () => {
    render(
      <TestWrapper>
        <PlaylistConfigForm />
      </TestWrapper>
    );

    expect(screen.getByText('Configure Playlist Details')).toBeInTheDocument();
    // Form should be expanded by default, so description should be visible
    expect(screen.getByText('Give your playlist a memorable name')).toBeInTheDocument();
  });

  it('should show form fields when expanded', () => {
    render(
      <TestWrapper>
        <PlaylistConfigForm />
      </TestWrapper>
    );

    // Form is expanded by default
    expect(screen.getByText('Give your playlist a memorable name')).toBeInTheDocument();
    expect(screen.getByText('Describe what makes this playlist special (optional)')).toBeInTheDocument();
  });

  it('should display default playlist name', () => {
    render(
      <TestWrapper>
        <PlaylistConfigForm />
      </TestWrapper>
    );

    const nameInput = screen.getByPlaceholderText('My Re:Play Playlist') as HTMLInputElement;
    expect(nameInput.value).toBe('My Re:Play Playlist');
  });

  it('should display default description', () => {
    render(
      <TestWrapper>
        <PlaylistConfigForm />
      </TestWrapper>
    );

    const descInput = screen.getByPlaceholderText('Created with Re:Play') as HTMLTextAreaElement;
    expect(descInput.value).toBe('Created with Re:Play');
  });

  it('should allow editing playlist name', async () => {
    const user = userEvent.setup();
    render(
      <TestWrapper>
        <PlaylistConfigForm />
      </TestWrapper>
    );

    const nameInput = screen.getByPlaceholderText('My Re:Play Playlist') as HTMLInputElement;
    await user.clear(nameInput);
    await user.type(nameInput, 'Top Tracks 2024');

    expect(nameInput.value).toBe('Top Tracks 2024');
  });

  it('should allow editing description', async () => {
    const user = userEvent.setup();
    render(
      <TestWrapper>
        <PlaylistConfigForm />
      </TestWrapper>
    );

    const descInput = screen.getByPlaceholderText('Created with Re:Play') as HTMLTextAreaElement;
    await user.clear(descInput);
    await user.type(descInput, 'My favorite tracks from Last.fm');

    expect(descInput.value).toBe('My favorite tracks from Last.fm');
  });

  it('should display public/private toggle with default private', () => {
    render(
      <TestWrapper>
        <PlaylistConfigForm />
      </TestWrapper>
    );

    expect(screen.getByText('Private')).toBeInTheDocument();
    expect(screen.getByText(/Only you can see this playlist/)).toBeInTheDocument();
  });

  it('should toggle public/private visibility', async () => {
    const user = userEvent.setup();
    render(
      <TestWrapper>
        <PlaylistConfigForm />
      </TestWrapper>
    );

    const toggle = screen.getByRole('switch');
    await user.click(toggle);

    expect(screen.getByText('Public')).toBeInTheDocument();
    expect(screen.getByText(/Anyone can see and follow this playlist/)).toBeInTheDocument();
  });

  it('should collapse form when toggle button is clicked', async () => {
    const user = userEvent.setup();
    render(
      <TestWrapper>
        <PlaylistConfigForm />
      </TestWrapper>
    );

    // Form is expanded by default
    expect(screen.getByText('Give your playlist a memorable name')).toBeInTheDocument();

    const button = screen.getByText('Configure Playlist Details').closest('button');
    await user.click(button!);

    expect(screen.queryByText('Give your playlist a memorable name')).not.toBeInTheDocument();
  });
});
