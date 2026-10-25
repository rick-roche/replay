import { act, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { Theme } from '@radix-ui/themes'
import { BrowserRouter } from 'react-router-dom'
import { Home } from './Home'
import { ConfigProvider, useConfig } from '../contexts/ConfigContext'
import { DataSourceProvider, useDataSource } from '../contexts/DataSourceContext'
import { DataProvider } from '../contexts/DataContext'
import { MatchProvider, useMatch } from '../contexts/MatchContext'
import { PlaylistProvider } from '../contexts/PlaylistContext'
import { CreatePlaylistProvider } from '../contexts/CreatePlaylistContext'
import { WorkflowProvider, useWorkflow, STEP_ORDER, WorkflowStep } from '../contexts/WorkflowContext'
import { DataSource } from '../types/datasource'
import { matchApi } from '../api/match'
import { configApi } from '../api/config'
vi.mock('../contexts/AuthContext', () => ({ useAuth: () => ({ user: { id: 'alice', displayName: 'Alice' }, isAuthenticated: true }) }))
vi.mock('../api/match')
vi.mock('../api/config')
function Controls() {
  const workflow = useWorkflow()
  const { selectSource } = useDataSource()
  const { configureLastfm, updateFilter, setAutoFetch } = useConfig()
  const { matchTracks, matchedData } = useMatch()
  return <>
    <button onClick={async () => {
      setAutoFetch(false)
      selectSource(DataSource.LASTFM)
      await configureLastfm('alice')
    }}>Prepare source</button>
    <button onClick={async () => {
      await matchTracks([{ name: 'Old track', artist: 'Artist', source: 'lastfm' }])
      STEP_ORDER.forEach(workflow.markStepComplete)
    }}>Seed workflow</button>
    <button onClick={() => workflow.goToStep(WorkflowStep.CREATE)}>Visit create</button>
    <button onClick={() => updateFilter({ maxResults: 10 })}>Change upstream filter</button>
    <button onClick={() => selectSource(DataSource.DISCOGS)}>Switch upstream source</button>
    <output data-testid="workflow-state">{workflow.currentStep}|{workflow.completedSteps.has(WorkflowStep.CURATE) ? 'curated' : 'not-curated'}|{matchedData ? 'matched' : 'empty'}</output>
  </>
}
describe('upstream workflow edits', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.mocked(configApi.configureLastfm).mockResolvedValue({ username: 'alice', profileUrl: '', playCount: 0, isConfigured: true })
    vi.mocked(matchApi.matchTracksToSpotify).mockResolvedValue({ tracks: [{ sourceTrack: { name: 'Old track', artist: 'Artist', source: 'lastfm' } }] })
  })
  it.each(['Change upstream filter', 'Switch upstream source'])('invalidates downstream matches and completed steps on %s', async action => {
    render(<BrowserRouter><Theme><DataSourceProvider><ConfigProvider><DataProvider><MatchProvider><PlaylistProvider><CreatePlaylistProvider><WorkflowProvider><Controls /><Home /></WorkflowProvider></CreatePlaylistProvider></PlaylistProvider></MatchProvider></DataProvider></ConfigProvider></DataSourceProvider></Theme></BrowserRouter>)
    // These test-only controls drive context transitions; pointer simulation adds unrelated work.
    const change = async (name: string) => {
      await act(async () => { fireEvent.click(screen.getByText(name, { selector: 'button' })) })
    }
    await change('Prepare source')
    await change('Seed workflow')
    await change('Visit create')
    await waitFor(() => expect(screen.getByTestId('workflow-state')).toHaveTextContent('create|curated|matched'))
    await change(action)
    await waitFor(() => expect(screen.getByTestId('workflow-state')).toHaveTextContent('not-curated|empty'))
    expect(screen.getByTestId('workflow-state')).not.toHaveTextContent('create|')
  })
})
