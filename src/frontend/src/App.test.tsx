import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { Theme } from '@radix-ui/themes'
import App from './App'

vi.mock('./contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { displayName: 'Tester', imageUrl: null },
    isLoading: false,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn()
  })
}))

vi.mock('./contexts/DataSourceContext', () => ({
  useDataSource: () => ({
    selectedSource: 'discogs',
    selectSource: vi.fn(),
    clearSource: vi.fn()
  })
}))

vi.mock('./contexts/ConfigContext', () => ({
  useConfig: () => ({
    autoFetch: false
  })
}))

vi.mock('./contexts/MatchContext', () => ({
  useMatch: () => ({
    matchedData: null
  })
}))

vi.mock('./contexts/WorkflowContext', () => ({
  WorkflowStep: {
    SELECT_SOURCE: 'select-source',
    CONFIGURE: 'configure',
    FETCH_AND_MATCH: 'fetch-and-match',
    CURATE: 'curate',
    CREATE: 'create'
  },
  useWorkflow: () => ({
    currentStep: 'fetch-and-match',
    completedSteps: new Set(['select-source', 'configure']),
    goToStep: vi.fn(),
    canGoToStep: () => true,
    markStepComplete: vi.fn(),
    nextStep: vi.fn(),
    previousStep: vi.fn(),
    resetWorkflow: vi.fn()
  }),
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

describe('App Discogs flow', () => {
  it('shows fetch controls for Discogs when auto-fetch is off', () => {
    render(
      <Theme>
        <App />
      </Theme>
    )

    expect(screen.getByText('FetchDataButton')).toBeInTheDocument()
    expect(screen.getByText('MatchTracksButton')).toBeInTheDocument()
  })
})
