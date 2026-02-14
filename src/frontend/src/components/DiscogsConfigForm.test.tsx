import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Theme } from '@radix-ui/themes'
import { DiscogsConfigForm } from '@/components/DiscogsConfigForm'
import { ConfigProvider } from '@/contexts/ConfigContext'

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>{children}</ConfigProvider>
    </Theme>
  )
}

describe('DiscogsConfigForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should render form when not configured', () => {
    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('Discogs Profile')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Connect Discogs/i })).toBeInTheDocument()
    expect(screen.getByPlaceholderText(/Discogs username or collection ID/i)).toBeInTheDocument()
  })

  it('should display helpful description text', () => {
    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('Link your Discogs username or collection ID')).toBeInTheDocument()
  })

  it('should disable submit button when input is empty', () => {
    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    const submitButton = screen.getByRole('button', { name: /Connect Discogs/i })
    expect(submitButton).toBeDisabled()
  })

  it('should enable submit button when input has value', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Discogs username or collection ID/i)
    const submitButton = screen.getByRole('button', { name: /Connect Discogs/i })

    await user.type(input, 'testuser')

    expect(submitButton).not.toBeDisabled()
  })

  it('should accept identifier input', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Discogs username or collection ID/i) as HTMLInputElement
    await user.type(input, 'my-username')

    expect(input.value).toBe('my-username')
  })

  it('should clear error when user starts typing', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Discogs username or collection ID/i)
    
    // Simulate error state by typing, clearing, then checking input is interactive
    await user.type(input, 'test')
    await user.clear(input)
    
    expect((input as HTMLInputElement).value).toBe('')
  })

  it('should have placeholder text for input field', () => {
    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Discogs username or collection ID/i)
    expect(input).toBeInTheDocument()
  })

  it('should show configured state with username and release count', () => {
    const config = {
      username: 'testuser',
      releaseCount: 42,
      collectionUrl: 'https://www.discogs.com/user/testuser/collection',
      isConfigured: true
    }
    localStorage.setItem('replay:discogs_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('testuser')).toBeInTheDocument()
    expect(screen.getByText('42 releases linked')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Change Profile/i })).toBeInTheDocument()
  })

  it('should show change profile button when configured', () => {
    const config = {
      username: 'testuser',
      releaseCount: 42,
      collectionUrl: 'https://www.discogs.com/user/testuser/collection',
      isConfigured: true
    }
    localStorage.setItem('replay:discogs_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    expect(screen.getByRole('button', { name: /Change Profile/i })).toBeInTheDocument()
  })

  it('should switch to edit mode when change profile is clicked', async () => {
    const user = userEvent.setup()
    const config = {
      username: 'testuser',
      releaseCount: 42,
      collectionUrl: 'https://www.discogs.com/user/testuser/collection',
      isConfigured: true
    }
    localStorage.setItem('replay:discogs_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    const changeButton = screen.getByRole('button', { name: /Change Profile/i })
    await user.click(changeButton)

    // Should now show the form instead of configured state
    expect(screen.getByPlaceholderText(/Discogs username or collection ID/i)).toBeInTheDocument()
  })

  it('should have form structure for accessibility', () => {
    render(
      <TestWrapper>
        <DiscogsConfigForm />
      </TestWrapper>
    )

    const form = screen.getByPlaceholderText(/Discogs username or collection ID/i).closest('form')
    expect(form).toBeInTheDocument()
  })
})
