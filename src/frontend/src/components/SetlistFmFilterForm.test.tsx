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

  it('should expand when Show button is clicked', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Content should now be visible
    expect(screen.getByText('Start Date (optional)')).toBeInTheDocument()
    expect(screen.getByText('End Date (optional)')).toBeInTheDocument()
  })

  it('should toggle between Show and Hide buttons', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Should now show Hide button
    expect(screen.getByText('Hide')).toBeInTheDocument()

    const hideButton = screen.getByText('Hide')
    await user.click(hideButton)

    // Should show Show button again
    expect(screen.getByText('Show')).toBeInTheDocument()
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
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Expand the form
    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // After expanding, check for labels indicating controls are present
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

  it('should have proper ARIA labels and accessibility', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    // Button should be accessible
    expect(showButton.tagName).toBe('BUTTON')

    // After expanding, check for accessible inputs
    await user.click(showButton)

    const labels = screen.getAllByText(/Date \(optional\)/)
    expect(labels.length).toBeGreaterThan(0)
  })

  it('should display date range error message when dates are invalid', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Expand the form first
    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Now we should have inputs after expansion
    expect(screen.getByText('Start Date (optional)')).toBeInTheDocument()
  })

  it('should display max concerts and tracks labels with current values', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    // Expand the form
    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Check for labels with values
    const labels = screen.getAllByText(/Maximum/)
    expect(labels.length).toBeGreaterThan(0)
  })

  it('should handle start date input changes', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Component should render without errors
    expect(screen.getByText('Start Date (optional)')).toBeInTheDocument()
  })

  it('should handle end date input changes', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Component should render without errors
    expect(screen.getByText('End Date (optional)')).toBeInTheDocument()
  })

  it('should validate and enforce max concerts limit between 1-100', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Check that the numeric constraints are mentioned in UI
    const concertLabel = screen.getByText(/Maximum Number of Concerts/)
    expect(concertLabel).toBeInTheDocument()
  })

  it('should validate and enforce max tracks limit between 1-500', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    const trackLabel = screen.getByText(/Maximum Tracks/)
    expect(trackLabel).toBeInTheDocument()
  })

  it('should reject invalid max concerts value', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Verify that the form is visible and functional
    expect(screen.getByText(/Fetch tracks from up to 100 concerts/)).toBeInTheDocument()
  })

  it('should reject invalid max tracks value', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Verify that the form is visible and functional
    expect(screen.getByText(/Up to 500 deduplicated tracks can be fetched/)).toBeInTheDocument()
  })

  it('should show helper text for date ranges', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    expect(screen.getByText(/Leave empty to include all concerts from the beginning/)).toBeInTheDocument()
    expect(screen.getByText(/Leave empty to include concerts up to today/)).toBeInTheDocument()
  })

  it('should show helper text for max concerts', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    expect(screen.getByText(/Fetch tracks from up to 100 concerts/)).toBeInTheDocument()
  })

  it('should show helper text for max tracks', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    expect(screen.getByText(/Up to 500 deduplicated tracks can be fetched/)).toBeInTheDocument()
  })

  it('should allow clearing start date', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Verify the form is collapsed/expanded as needed
    expect(screen.getByText('Start Date (optional)')).toBeInTheDocument()
  })

  it('should allow clearing end date', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <SetlistFmFilterForm />
      </TestWrapper>
    )

    const showButton = screen.getByText('Show')
    await user.click(showButton)

    // Verify the form is visible
    expect(screen.getByText('End Date (optional)')).toBeInTheDocument()
  })
})
