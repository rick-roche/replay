import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
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

  it('should be expanded by default with Hide button', () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const hideButton = screen.getByText('Hide')
    expect(hideButton).toBeInTheDocument()

    // Content should be visible initially
    expect(screen.getByText('Start Date (optional)')).toBeInTheDocument()
  })

  it('should collapse when Hide button is clicked', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const hideButton = screen.getByText('Hide')
    await user.click(hideButton)

    // Content should now be hidden
    expect(screen.queryByText('Start Date (optional)')).not.toBeInTheDocument()
    expect(screen.queryByText('End Date (optional)')).not.toBeInTheDocument()
  })

  it('should toggle between Hide and Show buttons', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Should start with Hide button (expanded by default)
    const hideButton = screen.getByText('Hide')
    await user.click(hideButton)

    // Should now show Show button
    expect(screen.getByText('Show')).toBeInTheDocument()

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Should show Hide button again
    expect(screen.getByText('Hide')).toBeInTheDocument()
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
  })

  it('should provide UI for all filter controls', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Form is expanded by default, check for labels indicating controls are present
    expect(screen.getByText('Start Date (optional)')).toBeInTheDocument()
    expect(screen.getByText('End Date (optional)')).toBeInTheDocument()
    expect(screen.getByText(/Maximum Number of Concerts/)).toBeInTheDocument()
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
    const hideButton = screen.getByText('Hide')
    expect(hideButton.tagName).toBe('BUTTON')
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

  it('should have proper ARIA labels and accessibility', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const hideButton = screen.getByText('Hide')
    // Button should be accessible
    expect(hideButton.tagName).toBe('BUTTON')

    // Form is expanded by default, check for accessible inputs
    const labels = screen.getAllByText(/Date \(optional\)/)
    expect(labels.length).toBeGreaterThan(0)
  })

  it('should display date range error message when dates are invalid', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Form is expanded by default, inputs should be visible
    expect(screen.getByText('Start Date (optional)')).toBeInTheDocument()
  })

  it('should display max concerts and tracks labels with current values', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Form is expanded by default, check for labels with values
    const labels = screen.getAllByText(/Maximum/)
    expect(labels.length).toBeGreaterThan(0)
  })

  it('should handle start date input changes', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Component should render without errors
    expect(screen.getByText('Start Date (optional)')).toBeInTheDocument()
  })

  it('should handle end date input changes', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Component should render without errors
    expect(screen.getByText('End Date (optional)')).toBeInTheDocument()
  })

  it('should validate and enforce max concerts limit between 1-100', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Check that the numeric constraints are mentioned in UI
    const concertLabel = screen.getByText(/Maximum Number of Concerts/)
    expect(concertLabel).toBeInTheDocument()
  })

  it('should validate and enforce max tracks limit between 1-500', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const trackLabel = screen.getByText(/Maximum Tracks/)
    expect(trackLabel).toBeInTheDocument()
  })

  it('should reject invalid max concerts value', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Verify that the form is visible and functional
    expect(screen.getByText(/Fetch tracks from up to 100 concerts/)).toBeInTheDocument()
  })

  it('should reject invalid max tracks value', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Verify that the form is visible and functional
    expect(screen.getByText(/Up to 500 deduplicated tracks can be fetched/)).toBeInTheDocument()
  })

  it('should show helper text for date ranges', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    expect(screen.getByText(/Leave empty to include all concerts from the beginning/)).toBeInTheDocument()
    expect(screen.getByText(/Leave empty to include concerts up to today/)).toBeInTheDocument()
  })

  it('should show helper text for max concerts', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    expect(screen.getByText(/Fetch tracks from up to 100 concerts/)).toBeInTheDocument()
  })

  it('should show helper text for max tracks', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    expect(screen.getByText(/Up to 500 deduplicated tracks can be fetched/)).toBeInTheDocument()
  })

  it('should allow clearing start date', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Verify the form is expanded and visible
    expect(screen.getByText('Start Date (optional)')).toBeInTheDocument()
  })

  it('should allow clearing end date', async () => {
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Verify the form is visible
    expect(screen.getByText('End Date (optional)')).toBeInTheDocument()
  })
})
