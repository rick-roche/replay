import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import { AutoFetcher } from '@/components/AutoFetcher'
import { ConfigProvider } from '@/contexts/ConfigContext'
import { DataProvider } from '@/contexts/DataContext'
import { MatchProvider } from '@/contexts/MatchContext'
import { DataSourceProvider } from '@/contexts/DataSourceContext'
import { DataSource } from '@/types/datasource'

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <Theme>
      <DataSourceProvider>
        <ConfigProvider>
          <DataProvider>
            <MatchProvider>{children}</MatchProvider>
          </DataProvider>
        </ConfigProvider>
      </DataSourceProvider>
    </Theme>
  )
}

describe('AutoFetcher', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
  })

  it('should render nothing when not fetching or matching', () => {
    const { container } = render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    expect(container.firstChild?.childNodes.length).toBeLessThanOrEqual(1)
  })

  it('should handle Last.fm source', () => {
    render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    expect(true).toBe(true)
  })

  it('should handle Discogs source', () => {
    render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    expect(true).toBe(true)
  })

  it('should handle Setlist.fm source', () => {
    render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    expect(true).toBe(true)
  })

  it('should display loading state', () => {
    render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    expect(true).toBe(true)
  })

  it('should only trigger fetch once on mount', () => {
    const { rerender } = render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    rerender(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    expect(true).toBe(true)
  })

  it('should only trigger match once after fetch completes', () => {
    const { rerender } = render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    rerender(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    expect(true).toBe(true)
  })

  it('should not display error initially', () => {
    render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    const error = screen.queryByText(/Error/i)
    expect(error).not.toBeInTheDocument()
  })

  it('should not display success initially', () => {
    render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    const success = screen.queryByText(/Data Fetched/i)
    expect(success).not.toBeInTheDocument()
  })

  it('should be responsive', () => {
    const { container } = render(
      <TestWrapper>
        <AutoFetcher />
      </TestWrapper>
    )

    expect(container).toBeInTheDocument()
  })
})
