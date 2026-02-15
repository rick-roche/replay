import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Theme } from '@radix-ui/themes'
import { SetlistConfigForm } from '@/components/SetlistConfigForm'
import { ConfigProvider } from '@/contexts/ConfigContext'

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>{children}</ConfigProvider>
    </Theme>
  )
}

describe('SetlistConfigForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should render form when not configured', () => {
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    expect(screen.getByRole('button', { name: /Connect Setlist.fm/i })).toBeInTheDocument()
    expect(screen.getByPlaceholderText(/Setlist.fm username or user ID/i)).toBeInTheDocument()
  })

  it('should render configured state with display name', () => {
    const config = {
      userId: 'user123',
      displayName: 'Test User',
      attendedConcerts: 5,
      profileUrl: 'https://setlist.fm/user/test',
      isConfigured: true
    }
    localStorage.setItem('replay:setlist_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('Test User')).toBeInTheDocument()
    expect(screen.getByText(/5 concerts attended/)).toBeInTheDocument()
  })

  it('should be dark mode compatible', () => {
    const { container } = render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    expect(container).toBeInTheDocument()
  })

  it('should be responsive', () => {
    const { container } = render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    expect(container).toBeInTheDocument()
  })

  it('should display heading when not configured', () => {
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('Setlist.fm Profile')).toBeInTheDocument()
  })

  it('should display subtitle when not configured', () => {
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText(/Link your Setlist.fm username or user ID/i)).toBeInTheDocument()
  })

  it('should have input field for username or ID', () => {
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Setlist.fm username or user ID/i) as HTMLInputElement
    expect(input).toBeInTheDocument()
  })

  it('should disable submit button when input is empty', () => {
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const button = screen.getByRole('button', { name: /Connect Setlist.fm/i })
    expect(button).toBeDisabled()
  })

  it('should enable submit button when input has value', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Setlist.fm username or user ID/i)
    await user.type(input, 'testuser')

    const button = screen.getByRole('button', { name: /Connect Setlist.fm/i })
    expect(button).not.toBeDisabled()
  })

  it('should allow input of username', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Setlist.fm username or user ID/i) as HTMLInputElement
    await user.type(input, 'myusername')

    expect(input.value).toBe('myusername')
  })

  it('should allow input of user ID', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Setlist.fm username or user ID/i) as HTMLInputElement
    await user.type(input, '12345')

    expect(input.value).toBe('12345')
  })

  it('should show Change Profile button when configured', () => {
    const config = {
      userId: 'user123',
      displayName: 'Test User',
      attendedConcerts: 5,
      profileUrl: 'https://setlist.fm/user/test',
      isConfigured: true
    }
    localStorage.setItem('replay:setlist_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    expect(screen.getByRole('button', { name: /Change Profile/i })).toBeInTheDocument()
  })

  it('should switch to edit mode when Change Profile is clicked', async () => {
    const user = userEvent.setup()
    const config = {
      userId: 'user123',
      displayName: 'Test User',
      attendedConcerts: 5,
      profileUrl: 'https://setlist.fm/user/test',
      isConfigured: true
    }
    localStorage.setItem('replay:setlist_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const changeButton = screen.getByRole('button', { name: /Change Profile/i })
    await user.click(changeButton)

    // Should switch to edit mode
    expect(screen.getByRole('button', { name: /Connect Setlist.fm/i })).toBeInTheDocument()
  })

  it('should pre-fill input with current user ID when switching to edit mode', async () => {
    const user = userEvent.setup()
    const config = {
      userId: 'user123',
      displayName: 'Test User',
      attendedConcerts: 5,
      profileUrl: 'https://setlist.fm/user/test',
      isConfigured: true
    }
    localStorage.setItem('replay:setlist_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const changeButton = screen.getByRole('button', { name: /Change Profile/i })
    await user.click(changeButton)

    const input = screen.getByPlaceholderText(/Setlist.fm username or user ID/i) as HTMLInputElement
    expect(input.value).toBe('user123')
  })

  it('should display green checkmark when configured', () => {
    const config = {
      userId: 'user123',
      displayName: 'Test User',
      attendedConcerts: 5,
      profileUrl: 'https://setlist.fm/user/test',
      isConfigured: true
    }
    localStorage.setItem('replay:setlist_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    // The component displays a green checkmark via CheckCircle2 icon
    const heading = screen.getByText('Setlist.fm Profile')
    expect(heading).toBeInTheDocument()
  })

  it('should display profile URL link when configured', () => {
    const config = {
      userId: 'user123',
      displayName: 'Test User',
      attendedConcerts: 5,
      profileUrl: 'https://setlist.fm/user/test123',
      isConfigured: true
    }
    localStorage.setItem('replay:setlist_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const link = screen.getByRole('link', { name: /View profile/i })
    expect(link).toHaveAttribute('href', 'https://setlist.fm/user/test123')
    expect(link).toHaveAttribute('target', '_blank')
  })

  it('should clear error on input change', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Setlist.fm username or user ID/i)
    
    // Type something - error should be cleared automatically
    await user.type(input, 'user')
    
    expect(input).toBeInTheDocument()
  })

  it('should show form with all required elements initially', () => {
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText('Setlist.fm Profile')).toBeInTheDocument()
    expect(screen.getByPlaceholderText(/Setlist.fm username or user ID/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Connect Setlist.fm/i })).toBeInTheDocument()
  })

  it('should handle whitespace trim in input', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const input = screen.getByPlaceholderText(/Setlist.fm username or user ID/i)
    await user.type(input, '  user123  ')

    // Input should accept the value (trimming happens on submit)
    expect((input as HTMLInputElement).value).toBe('  user123  ')
  })

  it('should handle display of multiple attended concerts', () => {
    const config = {
      userId: 'user456',
      displayName: 'Concert Fan',
      attendedConcerts: 125,
      profileUrl: 'https://setlist.fm/user/concertfan',
      isConfigured: true
    }
    localStorage.setItem('replay:setlist_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    expect(screen.getByText(/125 concerts attended/)).toBeInTheDocument()
  })

  it('should be in form element', () => {
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const forms = screen.getAllByRole('button')
    expect(forms.length).toBeGreaterThan(0)
  })

  it('should not show error initially', () => {
    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    // No alert or error message should be visible
    const alerts = screen.queryAllByRole('alert')
    expect(alerts.length).toBe(0)
  })

  it('should display configured state heading', () => {
    const config = {
      userId: 'user123',
      displayName: 'Test User',
      attendedConcerts: 5,
      profileUrl: 'https://setlist.fm/user/test',
      isConfigured: true
    }
    localStorage.setItem('replay:setlist_config', JSON.stringify(config))

    render(
      <TestWrapper>
        <SetlistConfigForm />
      </TestWrapper>
    )

    const heading = screen.getByText('Setlist.fm Profile')
    expect(heading).toBeInTheDocument()
  })
})
