import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { DiscogsFilterForm } from '@/components/DiscogsFilterForm'
import { ConfigProvider } from '@/contexts/ConfigContext'

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>{children}</ConfigProvider>
    </Theme>
  )
}

describe('DiscogsFilterForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('allows partial year input without blocking typing', () => {
    render(
      <TestWrapper>
        <DiscogsFilterForm />
      </TestWrapper>
    )

    fireEvent.click(screen.getByRole('button', { name: 'Show' }))
    const yearInputs = screen.getAllByRole('spinbutton')

    fireEvent.change(yearInputs[0], { target: { value: '2' } })

    expect(yearInputs[0]).toHaveValue(2)
  })

  it('persists valid year inputs to localStorage', () => {
    render(
      <TestWrapper>
        <DiscogsFilterForm />
      </TestWrapper>
    )

    fireEvent.click(screen.getByRole('button', { name: 'Show' }))
    const yearInputs = screen.getAllByRole('spinbutton')

    fireEvent.change(yearInputs[0], { target: { value: '1999' } })

    const stored = localStorage.getItem('replay:discogs_filter')
    expect(stored).not.toBeNull()

    const parsed = JSON.parse(stored ?? '{}') as { minReleaseYear?: number }
    expect(parsed.minReleaseYear).toBe(1999)
  })
})
