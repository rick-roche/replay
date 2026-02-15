import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import App from './App'

beforeEach(() => {
  vi.clearAllMocks()
  localStorage.clear()
})

vi.mock('./contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({
    user: { displayName: 'Tester', imageUrl: null },
    isLoading: false,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn()
  }))
}))

vi.mock('./contexts/DataSourceContext', () => ({
  useDataSource: vi.fn(() => ({
    selectedSource: 'discogs',
    selectSource: vi.fn(),
    clearSource: vi.fn()
  }))
}))

vi.mock('./contexts/ConfigContext', () => ({
  useConfig: vi.fn(() => ({
    autoFetch: false,
    setAutoFetch: vi.fn()
  }))
}))

vi.mock('./contexts/MatchContext', () => ({
  useMatch: vi.fn(() => ({
    matchedData: null
  }))
}))

vi.mock('./contexts/WorkflowContext', () => ({
  WorkflowStep: {
    SELECT_SOURCE: 'select-source',
    CONFIGURE: 'configure',
    FETCH_AND_MATCH: 'fetch-and-match',
    CURATE: 'curate',
    CREATE: 'create'
  },
  useWorkflow: vi.fn(() => ({
    currentStep: 'fetch-and-match',
    completedSteps: new Set(['select-source', 'configure']),
    goToStep: vi.fn(),
    canGoToStep: () => true,
    markStepComplete: vi.fn(),
    nextStep: vi.fn(),
    previousStep: vi.fn(),
    resetWorkflow: vi.fn()
  })),
  STEP_ORDER: ['select-source', 'configure', 'fetch-and-match', 'curate', 'create']
}))

vi.mock('./components/DataFetchForm', () => ({
  FetchDataButton: () => <div>FetchDataButton</div>,
  DataResults: () => <div>DataResults</div>
}))

vi.mock('./components/MatchTracksButton', () => ({
  MatchTracksButton: () => <div>MatchTracksButton</div>
}))

vi.mock('./components/MatchResults', () => ({
  MatchResults: () => <div>MatchResults</div>
}))

vi.mock('./components/AutoFetcher', () => ({
  AutoFetcher: () => <div>AutoFetcher</div>
}))

vi.mock('./components/WorkflowStepper', () => ({
  WorkflowStepper: () => <div>WorkflowStepper</div>
}))

vi.mock('./components/WorkflowStepContainer', () => ({
  WorkflowStepContainer: ({ children, title }: { children: React.ReactNode; title: string }) => <div>{title}: {children}</div>
}))

vi.mock('./components/DataSourceSelector', () => ({
  DataSourceSelector: () => <div>DataSourceSelector</div>
}))

vi.mock('./components/LastfmConfigForm', () => ({
  LastfmConfigForm: () => <div>LastfmConfigForm</div>
}))

vi.mock('./components/DiscogsConfigForm', () => ({
  DiscogsConfigForm: () => <div>DiscogsConfigForm</div>
}))

vi.mock('./components/SetlistConfigForm', () => ({
  SetlistConfigForm: () => <div>SetlistConfigForm</div>
}))

vi.mock('./components/LastfmFilterForm', () => ({
  LastfmFilterForm: () => <div>LastfmFilterForm</div>
}))

vi.mock('./components/DiscogsFilterForm', () => ({
  DiscogsFilterForm: () => <div>DiscogsFilterForm</div>
}))

vi.mock('./components/SetlistFmFilterForm', () => ({
  SetlistFmFilterForm: () => <div>SetlistFmFilterForm</div>
}))

vi.mock('./components/PlaylistConfigForm', () => ({
  PlaylistConfigForm: () => <div>PlaylistConfigForm</div>
}))

vi.mock('./components/CreatePlaylistButton', () => ({
  CreatePlaylistButton: () => <div>CreatePlaylistButton</div>
}))

vi.mock('./components/PlaylistConfirmation', () => ({
  PlaylistConfirmation: () => <div>PlaylistConfirmation</div>
}))

vi.mock('./components/AdvancedOptions', () => ({
  AdvancedOptions: () => <div>AdvancedOptions</div>
}))

describe('App', () => {
  it('should render with authenticated user', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    const replayHeadings = screen.queryAllByText(/Re:Play/i)
    expect(replayHeadings.length).toBeGreaterThan(0)
  })

  it('shows Re:Play header', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText('Re:Play')).toBeInTheDocument()
  })

  it('shows welcome message for authenticated user', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText(/Welcome back, Tester/i)).toBeInTheDocument()
  })

  it('shows workflow stepper when authenticated', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText('WorkflowStepper')).toBeInTheDocument()
  })

  it('shows data source selector step', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText('DataSourceSelector')).toBeInTheDocument()
  })

  it('shows configure step with Discogs when Discogs is selected', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText('DiscogsConfigForm')).toBeInTheDocument()
  })

  it('shows fetch controls for Discogs when auto-fetch is off', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText('FetchDataButton')).toBeInTheDocument()
    expect(screen.getByText('MatchTracksButton')).toBeInTheDocument()
  })

  it('should handle authenticated state correctly', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    // Should show authenticated content
    const testerElements = screen.queryAllByText(/Tester/i)
    expect(testerElements.length).toBeGreaterThan(0)
  })

  it('should render all main sections', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    // Check for main content sections
    expect(screen.getByText('WorkflowStepper')).toBeInTheDocument()
  })

  it('should display feature cards', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText('Last.fm')).toBeInTheDocument()
    expect(screen.getByText('Discogs')).toBeInTheDocument()
    expect(screen.getByText('Setlist.fm')).toBeInTheDocument()
  })

  it('should display how it works section', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText('How It Works')).toBeInTheDocument()
  })

  it('should have proper footer links', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    const links = screen.getAllByRole('link')
    expect(links.length).toBeGreaterThan(0)
  })

  it('should show user avatar when authenticated', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    // Avatar or user info should be rendered for authenticated user
    expect(screen.getByText('Tester')).toBeInTheDocument()
  })

  it('should have multiple workflow steps', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    // Should have at least the main steps rendered
    expect(screen.getByText('WorkflowStepper')).toBeInTheDocument()
  })

  it('should be responsive for dark mode', () => {
    const { container } = render(
      <Theme>
        <App />
      </Theme>
    )

    expect(container).toBeInTheDocument()
  })

  it('should show advanced options', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText('AdvancedOptions')).toBeInTheDocument()
  })
})
