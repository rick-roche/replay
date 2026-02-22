import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { Theme } from '@radix-ui/themes'
import { About } from './About'

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <BrowserRouter>
      <Theme>{children}</Theme>
    </BrowserRouter>
  )
}

describe('About', () => {
  it('should render Re:Play heading', () => {
    render(
      <TestWrapper>
        <About />
      </TestWrapper>
    )

    expect(screen.getByText('Re:Play')).toBeInTheDocument()
  })

  it('should display project description', () => {
    render(
      <TestWrapper>
        <About />
      </TestWrapper>
    )

    expect(screen.getByText(/transforms your music history/i)).toBeInTheDocument()
  })

  it('should display version information', () => {
    render(
      <TestWrapper>
        <About />
      </TestWrapper>
    )

    expect(screen.getByText('Version')).toBeInTheDocument()
  })

  it('should have GitHub link', () => {
    render(
      <TestWrapper>
        <About />
      </TestWrapper>
    )

    const githubLink = screen.getByRole('link', { name: /view on github/i })
    expect(githubLink).toHaveAttribute('href', 'https://github.com/rick-roche/replay')
    expect(githubLink).toHaveAttribute('target', '_blank')
  })

  it('should display features list', () => {
    render(
      <TestWrapper>
        <About />
      </TestWrapper>
    )

    expect(screen.getByText('Features')).toBeInTheDocument()
    expect(screen.getByText(/Connect Last.fm/i)).toBeInTheDocument()
    expect(screen.getByText(/Import your Discogs collection/i)).toBeInTheDocument()
    expect(screen.getByText(/Relive concerts/i)).toBeInTheDocument()
  })

  it('should display tagline', () => {
    render(
      <TestWrapper>
        <About />
      </TestWrapper>
    )

    expect(screen.getByText('Your music, replayed your way')).toBeInTheDocument()
  })
})
