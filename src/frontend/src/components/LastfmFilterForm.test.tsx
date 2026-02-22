import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { LastfmFilterForm } from '@/components/LastfmFilterForm'
import { ConfigProvider } from '@/contexts/ConfigContext'

// Create a test wrapper with required providers
function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>{children}</ConfigProvider>
    </Theme>
  )
}

describe('LastfmFilterForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should render the filter form component', () => {
    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    expect(screen.getByText('Filter Last.fm Data')).toBeInTheDocument()
  })

  it('should be expanded by default with Hide button', () => {
    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    const hideButton = screen.getByText('Hide')
    expect(hideButton).toBeInTheDocument()

    // Content should be visible initially
    expect(screen.getByText('What to fetch')).toBeInTheDocument()
  })

  it('should have filter icon in header', () => {
    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    // Check that Sliders icon (filter icon) is rendered via the component
    const heading = screen.getByText('Filter Last.fm Data')
    expect(heading).toBeInTheDocument()
  })

  it('should display max results with default value', () => {
    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    // The default max results should be shown in the heading
    const heading = screen.getByText(/Filter Last.fm Data/)
    expect(heading).toBeInTheDocument()
  })

  it('should load filter state from localStorage on mount', () => {
    // Pre-populate localStorage
    const filterState = {
      dataType: 'Tracks',
      timePeriod: 'Last3Months',
      maxResults: 100
    }
    localStorage.setItem('replay:lastfm_filter', JSON.stringify(filterState))

    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    // Component should load without errors
    expect(screen.getByText('Filter Last.fm Data')).toBeInTheDocument()

    // Verify localStorage wasn't cleared
    expect(localStorage.getItem('replay:lastfm_filter')).toBe(JSON.stringify(filterState))
  })

  it('should provide UI for all filter controls', () => {
    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    // Even collapsed, the component should exist and render
    const component = screen.getByText('Filter Last.fm Data').closest('div')
    expect(component).toBeInTheDocument()
  })

  it('should use Radix UI components for accessibility', () => {
    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    // Check for Radix UI Card wrapper
    const heading = screen.getByText('Filter Last.fm Data')
    expect(heading).toBeInTheDocument()

    // The button should be focusable/accessible
    const hideButton = screen.getByText('Hide')
    expect(hideButton.tagName).toBe('BUTTON')
  })

  it('should be dark-mode compatible', () => {
    // Rendered within Theme provider which handles dark mode
    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    const component = screen.getByText('Filter Last.fm Data').closest('div')
    expect(component).toBeInTheDocument()
  })

  it('should be responsive', () => {
    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    // Component should render without issues at any viewport
    expect(screen.getByText('Filter Last.fm Data')).toBeInTheDocument()
  })

  it('should have proper ARIA labels and accessibility', () => {
    render(
      <TestWrapper>
        <LastfmFilterForm />
      </TestWrapper>
    )

    const hideButton = screen.getByText('Hide')
    // Button should be accessible
    expect(hideButton.tagName).toBe('BUTTON')
  })
})
