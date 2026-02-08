import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { WorkflowProvider, useWorkflow, WorkflowStep } from '../contexts/WorkflowContext'
import { WorkflowStepContainer } from './WorkflowStepContainer'

describe('WorkflowStepContainer', () => {
  it('should render active step content', () => {
    render(
      <WorkflowProvider>
        <WorkflowStepContainer
          step={WorkflowStep.SELECT_SOURCE}
          title="Test Step"
        >
          <div>Step content</div>
        </WorkflowStepContainer>
      </WorkflowProvider>
    )

    expect(screen.getByText('Test Step')).toBeInTheDocument()
    expect(screen.getByText('Step content')).toBeInTheDocument()
  })

  it('should not render if step is not active and not complete', () => {
    render(
      <WorkflowProvider>
        <WorkflowStepContainer
          step={WorkflowStep.CONFIGURE}
          title="Test Step"
        >
          <div>Step content</div>
        </WorkflowStepContainer>
      </WorkflowProvider>
    )

    expect(screen.queryByText('Test Step')).not.toBeInTheDocument()
  })

  it('should render collapsed view when step is completed but not active', async () => {
    const user = userEvent.setup()
    
    function TestComponent() {
      const { markStepComplete, nextStep } = useWorkflow()

      return (
        <>
          <button onClick={() => {
            markStepComplete(WorkflowStep.SELECT_SOURCE)
            nextStep()
          }}>
            Next
          </button>
          <WorkflowStepContainer
            step={WorkflowStep.SELECT_SOURCE}
            title="Test Step"
          >
            <div>Step content</div>
          </WorkflowStepContainer>
        </>
      )
    }

    render(
      <WorkflowProvider>
        <TestComponent />
      </WorkflowProvider>
    )

    const nextButton = screen.getByText('Next')
    await user.click(nextButton)

    // Wait for React state to update
    await waitFor(() => {
      // Step should now be collapsed
      expect(screen.getByText('Test Step')).toBeInTheDocument()
      expect(screen.queryByText('Step content')).not.toBeInTheDocument()
    })
  })

  it('should show Continue button when onComplete is provided', () => {
    const onComplete = vi.fn()

    render(
      <WorkflowProvider>
        <WorkflowStepContainer
          step={WorkflowStep.SELECT_SOURCE}
          title="Test Step"
          onComplete={onComplete}
        >
          <div>Step content</div>
        </WorkflowStepContainer>
      </WorkflowProvider>
    )

    expect(screen.getByText('Continue')).toBeInTheDocument()
  })

  it('should call onComplete when Continue button is clicked', async () => {
    const user = userEvent.setup()
    const onComplete = vi.fn()

    render(
      <WorkflowProvider>
        <WorkflowStepContainer
          step={WorkflowStep.SELECT_SOURCE}
          title="Test Step"
          onComplete={onComplete}
        >
          <div>Step content</div>
        </WorkflowStepContainer>
      </WorkflowProvider>
    )

    const continueButton = screen.getByText('Continue')
    await user.click(continueButton)

    expect(onComplete).toHaveBeenCalledTimes(1)
  })

  it('should disable Continue button when canComplete is false', () => {
    const onComplete = vi.fn()

    render(
      <WorkflowProvider>
        <WorkflowStepContainer
          step={WorkflowStep.SELECT_SOURCE}
          title="Test Step"
          onComplete={onComplete}
          canComplete={false}
        >
          <div>Step content</div>
        </WorkflowStepContainer>
      </WorkflowProvider>
    )

    const continueButton = screen.getByText('Continue')
    expect(continueButton).toBeDisabled()
  })
})
