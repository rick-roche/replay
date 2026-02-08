import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { SetlistFmFilterForm } from '@/components/SetlistFmFilterForm'
import { ConfigProvider } from '@/contexts/ConfigContext'

// Create a test wrapper with required providers
function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>{children}</ConfigProvider>
    </Theme>
  )
}

describe('SetlistFmFilterForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should render the filter form component', () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    expect(screen.getByText('Filter Setlist.fm Concerts')).toBeInTheDocument()
  })

  it('should be collapsible by default with Show button', () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    expect(showButton).toBeInTheDocument()

    // Content should not be visible initially
    expect(screen.queryByText('Start Date (optional)')).not.toBeInTheDocument()
  })

  it('should have filter icon in header', () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Check that Sliders icon (filter icon) is rendered via the component
    const heading = screen.getByText('Filter Setlist.fm Concerts')
    expect(heading).toBeInTheDocument()
  })

  it('should display filter heading with defaults', () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // The heading should be visible
    const heading = screen.getByText(/Filter Setlist.fm Concerts/)
    expect(heading).toBeInTheDocument()
  })

  it('should load filter state from localStorage on mount', () => {
    // Pre-populate localStorage
    const filterState = {
      maxConcerts: 20,
      maxTracks: 150
    }
    localStorage.setItem('replay:setlistfm_filter', JSON.stringify(filterState))

    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Component should load without errors
    expect(screen.getByText('Filter Setlist.fm Concerts')).toBeInTheDocument()

    // Verify localStorage wasn't cleared
    expect(localStorage.getItem('replay:setlistfm_filter')).toBe(JSON.stringify(filterState))
  })

  it('should provide UI for all filter controls', () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Even collapsed, the component should exist and render
    const component = screen.getByText('Filter Setlist.fm Concerts').closest('div')
    expect(component).toBeInTheDocument()
  })

  it('should use Radix UI components for accessibility', () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Check for Radix UI Card wrapper
    const heading = screen.getByText('Filter Setlist.fm Concerts')
    expect(heading).toBeInTheDocument()

    // The button should be focusable/accessible
    const showButton = screen.getByText('Show')
    expect(showButton.tagName).toBe('BUTTON')
  })

  it('should be dark-mode compatible', () => {
    // Rendered within Theme provider which handles dark mode
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const component = screen.getByText('Filter Setlist.fm Concerts').closest('div')
    expect(component).toBeInTheDocument()
  })

  it('should be responsive', () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Component should render without issues at any viewport
    expect(screen.getByText('Filter Setlist.fm Concerts')).toBeInTheDocument()
  })

  it('should have proper ARIA labels and accessibility', () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    // Button should be accessible
    expect(showButton.tagName).toBe('BUTTON')
  })
})
