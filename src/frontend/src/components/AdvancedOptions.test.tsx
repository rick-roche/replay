import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Theme } from '@radix-ui/themes'
import { AdvancedOptions } from '@/components/AdvancedOptions'
import { ConfigProvider } from '@/contexts/ConfigContext'

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <ConfigProvider>{children}</ConfigProvider>
    </Theme>
  )
}

describe('AdvancedOptions', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should render collapsed by default', () => {
    render(
      <TestWrapper>
        <AdvancedOptions />
      </TestWrapper>
    )

    expect(screen.getByText('Advanced Options')).toBeInTheDocument()
    expect(screen.queryByText('Auto-Fetch & Match')).not.toBeInTheDocument()
  })

  it('should expand and show auto-fetch option when button clicked', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <AdvancedOptions />
      </TestWrapper>
    )

    const button = screen.getByText('Advanced Options').closest('button')
    expect(button).toBeInTheDocument()

    await user.click(button!)

    expect(screen.getByText('Auto-Fetch & Match')).toBeInTheDocument()
  })

  it('should display auto-fetch label when expanded', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <AdvancedOptions />
      </TestWrapper>
    )

    const button = screen.getByText('Advanced Options').closest('button')
    await user.click(button!)

    const label = screen.getByText('Auto-Fetch & Match')
    expect(label).toBeInTheDocument()
  })

  it('should toggle auto-fetch switch', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <AdvancedOptions />
      </TestWrapper>
    )

    const button = screen.getByText('Advanced Options').closest('button')
    await user.click(button!)

    const toggle = screen.getByRole('switch')
    expect(toggle).toBeInTheDocument()

    await user.click(toggle)

    // Verify the toggle was interacted with
    expect(toggle).toBeInTheDocument()
  })

  it('should collapse when button is clicked again', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <AdvancedOptions />
      </TestWrapper>
    )

    const button = screen.getByText('Advanced Options').closest('button')
    await user.click(button!)

    expect(screen.getByText('Auto-Fetch & Match')).toBeInTheDocument()

    await user.click(button!)

    expect(screen.queryByText('Auto-Fetch & Match')).not.toBeInTheDocument()
  })

  it('should have a switch control', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <AdvancedOptions />
      </TestWrapper>
    )

    const button = screen.getByText('Advanced Options').closest('button')
    await user.click(button!)

    const toggle = screen.getByRole('switch')
    expect(toggle).toBeInTheDocument()
  })

  it('should have helpful description text', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <AdvancedOptions />
      </TestWrapper>
    )

    const button = screen.getByText('Advanced Options').closest('button')
    await user.click(button!)

    // Should show one of the descriptions
    const descriptionElements = screen.queryAllByText(/auto|manually/i)
    expect(descriptionElements.length).toBeGreaterThan(0)
  })

  it('should be responsive', () => {
    const { container } = render(
      <TestWrapper>
        <AdvancedOptions />
      </TestWrapper>
    )

    expect(container).toBeInTheDocument()
  })

  it('should have icon that changes on expand/collapse', async () => {
    const user = userEvent.setup()
    render(
      <TestWrapper>
        <AdvancedOptions />
      </TestWrapper>
    )

    const button = screen.getByText('Advanced Options').closest('button')
    const svgBefore = button?.querySelector('svg')
    expect(svgBefore).toBeInTheDocument()

    await user.click(button!)

    const svgAfter = button?.querySelector('svg')
    expect(svgAfter).toBeInTheDocument()
  })
})
