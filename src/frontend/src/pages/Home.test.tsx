import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { Theme } from '@radix-ui/themes'
import { Home } from './Home'

let mockAuthState = {
  user: null,
  isLoading: false,
  isAuthenticated: false,
  login: vi.fn(),
  logout: vi.fn()
}

let mockDataSourceState = {
  selectedSource: null,
  selectSource: vi.fn(),
  clearSource: vi.fn()
}

let mockWorkflowState = {
  currentStep: 'select-source',
  completedSteps: new Set(),
  goToStep: vi.fn(),
  canGoToStep: () => false,
  markStepComplete: vi.fn(),
  nextStep: vi.fn(),
  previousStep: vi.fn(),
  resetWorkflow: vi.fn(),
  lockWorkflow: vi.fn()
}

let mockMatchState = {
  matchedData: null,
  matchedAlbums: null,
  matchedArtists: null,
  clearMatches: vi.fn()
}

let mockConfigState = {
  autoFetch: false,
  setAutoFetch: vi.fn()
}

let mockDataState = {
  normalizedData: null,
  isLoading: false,
  error: null,
  clearData: vi.fn()
}

beforeEach(() => {
  vi.clearAllMocks()
  localStorage.clear()
  
  // Reset to default unauthenticated state
  mockAuthState = {
    user: null,
    isLoading: false,
    isAuthenticated: false,
    login: vi.fn(),
    logout: vi.fn()
  }
  
  mockDataSourceState = {
    selectedSource: null,
    selectSource: vi.fn(),
    clearSource: vi.fn()
  }
  
  mockWorkflowState = {
    currentStep: 'select-source',
    completedSteps: new Set(),
    goToStep: vi.fn(),
    canGoToStep: () => false,
    markStepComplete: vi.fn(),
    nextStep: vi.fn(),
    previousStep: vi.fn(),
    resetWorkflow: vi.fn(),
    lockWorkflow: vi.fn()
  }
  
  mockMatchState = {
    matchedData: null,
    matchedAlbums: null,
    matchedArtists: null,
    clearMatches: vi.fn()
  }
  
  mockConfigState = {
    autoFetch: false,
    setAutoFetch: vi.fn()
  }
  
  mockDataState = {
    normalizedData: null,
    isLoading: false,
    error: null,
    clearData: vi.fn()
  }
})

// Mock all the contexts and components used by Home
vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => mockAuthState
}))

vi.mock('../contexts/DataSourceContext', () => ({
  useDataSource: () => mockDataSourceState
}))

vi.mock('../contexts/ConfigContext', () => ({
  useConfig: () => mockConfigState
}))

vi.mock('../contexts/DataContext', () => ({
  useData: () => mockDataState
}))

vi.mock('../contexts/MatchContext', () => ({
  useMatch: () => mockMatchState
}))

vi.mock('../contexts/WorkflowContext', () => ({
  WorkflowStep: {
    SELECT_SOURCE: 'select-source',
    CONFIGURE: 'configure',
    FETCH_AND_MATCH: 'fetch-and-match',
    CURATE: 'curate',
    CREATE: 'create'
  },
  useWorkflow: () => mockWorkflowState,
  STEP_ORDER: ['select-source', 'configure', 'fetch-and-match', 'curate', 'create']
}))

// Mock all the component imports
vi.mock('../components/WorkflowStepper', () => ({
  WorkflowStepper: () => <div>WorkflowStepper</div>
}))

vi.mock('../components/WorkflowStepContainer', () => ({
  WorkflowStepContainer: ({ title }: { title: string }) => <div>{title}</div>
}))

vi.mock('../components/DataSourceSelector', () => ({
  DataSourceSelector: () => <div>DataSourceSelector</div>
}))

vi.mock('../components/LastfmConfigForm', () => ({
  LastfmConfigForm: () => <div>LastfmConfigForm</div>
}))

vi.mock('../components/DiscogsConfigForm', () => ({
  DiscogsConfigForm: () => <div>DiscogsConfigForm</div>
}))

vi.mock('../components/SetlistConfigForm', () => ({
  SetlistConfigForm: () => <div>SetlistConfigForm</div>
}))

vi.mock('../components/LastfmFilterForm', () => ({
  LastfmFilterForm: () => <div>LastfmFilterForm</div>
}))

vi.mock('../components/DiscogsFilterForm', () => ({
  DiscogsFilterForm: () => <div>DiscogsFilterForm</div>
}))

vi.mock('../components/SetlistFmFilterForm', () => ({
  SetlistFmFilterForm: () => <div>SetlistFmFilterForm</div>
}))

vi.mock('../components/DataFetchForm', () => ({
  FetchDataButton: () => <div>FetchDataButton</div>,
  DataResults: () => <div>DataResults</div>
}))

vi.mock('../components/MatchTracksButton', () => ({
  MatchTracksButton: () => <div>MatchTracksButton</div>
}))

vi.mock('../components/MatchAlbumsButton', () => ({
  MatchAlbumsButton: () => <div>MatchAlbumsButton</div>
}))

vi.mock('../components/MatchArtistsButton', () => ({
  MatchArtistsButton: () => <div>MatchArtistsButton</div>
}))

vi.mock('../components/MatchResults', () => ({
  MatchResults: () => <div>MatchResults</div>
}))

vi.mock('../components/PlaylistConfigForm', () => ({
  PlaylistConfigForm: () => <div>PlaylistConfigForm</div>
}))

vi.mock('../components/CreatePlaylistButton', () => ({
  CreatePlaylistButton: () => <div>CreatePlaylistButton</div>
}))

vi.mock('../components/PlaylistConfirmation', () => ({
  PlaylistConfirmation: () => <div>PlaylistConfirmation</div>
}))

vi.mock('../components/AutoFetcher', () => ({
  AutoFetcher: () => <div>AutoFetcher</div>
}))

vi.mock('../components/AdvancedOptions', () => ({
  AdvancedOptions: () => <div>AdvancedOptions</div>
}))

function TestWrapper({ children }: { children: React.ReactNode }) {
  return (
    <BrowserRouter>
      <Theme>{children}</Theme>
    </BrowserRouter>
  )
}

describe('Home', () => {
  it('should render unauthenticated hero section', () => {
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByText(/Replay Your Music History/i)).toBeInTheDocument()
  })

  it('should display feature cards', () => {
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByText('Last.fm')).toBeInTheDocument()
    expect(screen.getByText('Discogs')).toBeInTheDocument()
    expect(screen.getByText('Setlist.fm')).toBeInTheDocument()
  })

  it('should display how it works section', () => {
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByText('How It Works')).toBeInTheDocument()
    expect(screen.getByText('Connect')).toBeInTheDocument()
    expect(screen.getByText('Select')).toBeInTheDocument()
    expect(screen.getByText('Curate')).toBeInTheDocument()
    expect(screen.getByText('Create')).toBeInTheDocument()
  })

  it('should have get started button', () => {
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByRole('button', { name: /get started/i })).toBeInTheDocument()
  })

  it('should have learn more link to about page', () => {
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    const learnMoreLink = screen.getByRole('link', { name: /learn more/i })
    expect(learnMoreLink).toBeInTheDocument()
    expect(learnMoreLink).toHaveAttribute('href', '/about')
  })

  it('should render authenticated welcome message', () => {
    mockAuthState.isAuthenticated = true
    mockAuthState.user = { displayName: 'TestUser', imageUrl: null }
    
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByText(/Welcome back, TestUser/i)).toBeInTheDocument()
  })

  it('should render workflow stepper when authenticated', () => {
    mockAuthState.isAuthenticated = true
    mockAuthState.user = { displayName: 'TestUser', imageUrl: null }
    
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByText('WorkflowStepper')).toBeInTheDocument()
  })

  it('should render configure step when data source is selected', () => {
    mockAuthState.isAuthenticated = true
    mockAuthState.user = { displayName: 'TestUser', imageUrl: null }
    mockDataSourceState.selectedSource = 'lastfm'
    
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByText('Configure')).toBeInTheDocument()
  })

  it('should render fetch and match step when data source is selected', () => {
    mockAuthState.isAuthenticated = true
    mockAuthState.user = { displayName: 'TestUser', imageUrl: null }
    mockDataSourceState.selectedSource = 'discogs'
    mockWorkflowState.currentStep = 'fetch-and-match'
    
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByText('Fetch & Match')).toBeInTheDocument()
  })

  it('should render curate step when matched data exists', () => {
    mockAuthState.isAuthenticated = true
    mockAuthState.user = { displayName: 'TestUser', imageUrl: null }
    mockMatchState.matchedData = { tracks: [{ id: '1', name: 'Test Track', artist: { name: 'Test Artist' } }] }
    mockWorkflowState.currentStep = 'curate'
    
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByText('Curate Playlist')).toBeInTheDocument()
  })

  it('should render create step when matched data exists', () => {
    mockAuthState.isAuthenticated = true
    mockAuthState.user = { displayName: 'TestUser', imageUrl: null }
    mockMatchState.matchedData = { tracks: [{ id: '1', name: 'Test Track', artist: { name: 'Test Artist' } }] }
    mockWorkflowState.currentStep = 'create'
    
    render(
      <TestWrapper>
        <Home />
      </TestWrapper>
    )

    expect(screen.getByText('Create Playlist')).toBeInTheDocument()
  })
})
