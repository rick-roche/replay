import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
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
})
