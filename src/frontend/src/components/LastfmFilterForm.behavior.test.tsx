import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { LastfmFilterForm } from './LastfmFilterForm'
import { ConfigProvider } from '@/contexts/ConfigContext'

function Wrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>{children}</ConfigProvider>
    </Theme>
  )
}

describe('LastfmFilterForm behavior', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('toggles expand/collapse', () => {
    render(
      <Wrapper>
        <LastfmFilterForm />
      </Wrapper>
    )

    // Panel starts expanded, so content should be visible
    expect(screen.getByText('What to fetch')).toBeInTheDocument()

    // Click Hide to collapse
    const hide = screen.getByRole('button', { name: 'Hide' })
    fireEvent.click(hide)
    expect(screen.queryByText('What to fetch')).not.toBeInTheDocument()

    // Click Show to expand again
    const show = screen.getByRole('button', { name: 'Show' })
    fireEvent.click(show)
    expect(screen.getByText('What to fetch')).toBeInTheDocument()
  })

  it('renders custom date inputs and validation when time period is Custom', () => {
    // Pre-load filter as custom with invalid range
    localStorage.setItem('replay:lastfm_filter', JSON.stringify({
      dataType: 'Tracks',
      timePeriod: 'Custom',
      customStartDate: '2025-12-31',
      customEndDate: '2025-01-01',
      maxResults: 50
    }))

    render(
      <Wrapper>
        <LastfmFilterForm />
      </Wrapper>
    )

    // Panel is expanded by default, controls should be visible
    expect(screen.getByDisplayValue('2025-12-31')).toBeInTheDocument()
    expect(screen.getByDisplayValue('2025-01-01')).toBeInTheDocument()

    // Invalid range message should appear
    expect(screen.getByText('Start date must be before end date')).toBeInTheDocument()
  })

  it('accepts valid max results and rejects invalid values', () => {
    render(
      <Wrapper>
        <LastfmFilterForm />
      </Wrapper>
    )

    // Panel is expanded by default, controls should be visible
    const input = screen.getByRole('spinbutton') as HTMLInputElement

    // Initial default value
    expect(input.value).toBe('50')

    // Valid: change to 200
    fireEvent.change(input, { target: { value: '200' } })
    expect(input.value).toBe('200')
    fireEvent.blur(input) // Validate on blur
    expect(input.value).toBe('200')

    // Invalid: over 500 - allows typing but resets on blur
    fireEvent.change(input, { target: { value: '600' } })
    expect(input.value).toBe('600') // Allows typing
    fireEvent.blur(input) // Should reset to last valid value
    expect(input.value).toBe('200')

    // Invalid: zero - allows typing but resets on blur
    fireEvent.change(input, { target: { value: '0' } })
    expect(input.value).toBe('0') // Allows typing
    fireEvent.blur(input) // Should reset to last valid value
    expect(input.value).toBe('200')

    // Invalid: empty string - allows typing but resets on blur
    fireEvent.change(input, { target: { value: '' } })
    expect(input.value).toBe('') // Allows clearing
    fireEvent.blur(input) // Should reset to last valid value
    expect(input.value).toBe('200')
  })
})
