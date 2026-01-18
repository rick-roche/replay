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

    const toggle = screen.getByRole('button', { name: 'Show' })
    fireEvent.click(toggle)
    expect(screen.getByText('What to fetch')).toBeInTheDocument()

    const hide = screen.getByRole('button', { name: 'Hide' })
    fireEvent.click(hide)
    expect(screen.queryByText('What to fetch')).not.toBeInTheDocument()
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

    // Expand to see controls
    fireEvent.click(screen.getByRole('button', { name: 'Show' }))

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

    // Expand controls
    fireEvent.click(screen.getByRole('button', { name: 'Show' }))

    const input = screen.getByRole('spinbutton') as HTMLInputElement

    // Valid: change to 200
    fireEvent.change(input, { target: { value: '200' } })
    expect(input.value).toBe('200')

    // Invalid: over 500
    fireEvent.change(input, { target: { value: '600' } })
    expect(input.value).toBe('200')

    // Invalid: zero
    fireEvent.change(input, { target: { value: '0' } })
    expect(input.value).toBe('200')

    // Invalid: non-number
    fireEvent.change(input, { target: { value: 'abc' } })
    expect(input.value).toBe('200')
  })
})
