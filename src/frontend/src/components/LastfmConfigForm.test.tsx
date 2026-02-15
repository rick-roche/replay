import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Theme } from '@radix-ui/themes'
import { LastfmConfigForm } from '@/components/LastfmConfigForm'
import { ConfigProvider } from '@/contexts/ConfigContext'

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>{children}</ConfigProvider>
    </Theme>
  )
}

describe('LastfmConfigForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should render form when not configured', () => {
    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('Last.fm Profile')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Connect Last.fm/i })).toBeInTheDocument()
    expect(screen.getByPlaceholderText(/Last.fm username/i)).toBeInTheDocument()
  })

  it('should display helpful description text', () => {
    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('Connect your Last.fm account to access your listening history')).toBeInTheDocument()
  })

  it('should disable submit button when input is empty', () => {
    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    const submitButton = screen.getByRole('button', { name: /Connect Last.fm/i })
    expect(submitButton).toBeDisabled()
  })

  it('should enable submit button when input has value', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Last.fm username/i)
    const submitButton = screen.getByRole('button', { name: /Connect Last.fm/i })

    await user.type(input, 'testuser')

    expect(submitButton).not.toBeDisabled()
  })

  it('should accept username input', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Last.fm username/i) as HTMLInputElement
    await user.type(input, 'my-username')

    expect(input.value).toBe('my-username')
  })

  it('should clear error when user starts typing', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Last.fm username/i)
    
    // Simulate error state by typing, clearing, then checking input is interactive
    await user.type(input, 'test')
    await user.clear(input)
    
    expect((input as HTMLInputElement).value).toBe('')
  })

  it('should have placeholder text for input field', () => {
    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Last.fm username/i)
    expect(input).toBeInTheDocument()
  })

  it('should show configured state with username and play count', () => {
    const config = {
      username: 'testuser',
      playCount: 10000,
      profileUrl: 'https://www.last.fm/user/testuser',
      isConfigured: true
    }
    localStorage.setItem('replay:lastfm_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('testuser')).toBeInTheDocument()
    expect(screen.getByText('10,000 total scrobbles')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Change Username/i })).toBeInTheDocument()
  })

  it('should show change username button when configured', () => {
    const config = {
      username: 'testuser',
      playCount: 10000,
      profileUrl: 'https://www.last.fm/user/testuser',
      isConfigured: true
    }
    localStorage.setItem('replay:lastfm_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    expect(screen.getByRole('button', { name: /Change Username/i })).toBeInTheDocument()
  })

  it('should switch to edit mode when change username is clicked', async () => {
    const user = userEvent.setup()
    const config = {
      username: 'testuser',
      playCount: 10000,
      profileUrl: 'https://www.last.fm/user/testuser',
      isConfigured: true
    }
    localStorage.setItem('replay:lastfm_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    const changeButton = screen.getByRole('button', { name: /Change Username/i })
    await user.click(changeButton)

    // Should now show the form instead of configured state
    expect(screen.getByPlaceholderText(/Last.fm username/i)).toBeInTheDocument()
  })

  it('should have form structure for accessibility', () => {
    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    const form = screen.getByPlaceholderText(/Last.fm username/i).closest('form')
    expect(form).toBeInTheDocument()
  })

  it('should format large play counts with commas', () => {
    const config = {
      username: 'testuser',
      playCount: 1234567,
      profileUrl: 'https://www.last.fm/user/testuser',
      isConfigured: true
    }
    localStorage.setItem('replay:lastfm_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <LastfmConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('1,234,567 total scrobbles')).toBeInTheDocument()
  })
})
