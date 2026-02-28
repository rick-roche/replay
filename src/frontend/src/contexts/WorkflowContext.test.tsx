import { renderHook, act } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { WorkflowProvider, useWorkflow, WorkflowStep } from './WorkflowContext'

describe('WorkflowContext', () => {
  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <WorkflowProvider>{children}</WorkflowProvider>
  )

  it('should start at SELECT_SOURCE step', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    expect(result.current.currentStep).toBe(WorkflowStep.SELECT_SOURCE)
  })

  it('should advance to next step', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    // Mark current step complete before advancing
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)
  })

  it('should go to previous step', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)

    act(() => {
      result.current.previousStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.SELECT_SOURCE)
  })

  it('should mark step as complete', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
    })

    expect(result.current.completedSteps.has(WorkflowStep.SELECT_SOURCE)).toBe(true)
  })

  it('should allow going to completed steps', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)
    expect(result.current.canGoToStep(WorkflowStep.SELECT_SOURCE)).toBe(true)

    act(() => {
      result.current.goToStep(WorkflowStep.SELECT_SOURCE)
    })

    expect(result.current.currentStep).toBe(WorkflowStep.SELECT_SOURCE)
  })

  it('should not allow skipping steps', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    expect(result.current.canGoToStep(WorkflowStep.CURATE)).toBe(false)
    
    act(() => {
      result.current.goToStep(WorkflowStep.CURATE)
    })

    expect(result.current.currentStep).toBe(WorkflowStep.SELECT_SOURCE)
  })

  it('should allow going to next step when previous steps are complete', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
    })

    expect(result.current.canGoToStep(WorkflowStep.CONFIGURE)).toBe(true)
  })

  it('should reset workflow', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    // Move to CONFIGURE
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
      result.current.nextStep()
    })

    // Move to FETCH_AND_MATCH
    act(() => {
      result.current.markStepComplete(WorkflowStep.CONFIGURE)
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.FETCH_AND_MATCH)
    expect(result.current.completedSteps.size).toBe(2)

    act(() => {
      result.current.resetWorkflow()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.SELECT_SOURCE)
    expect(result.current.completedSteps.size).toBe(0)
  })

  it('should not go to previous step from first step', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    act(() => {
      result.current.previousStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.SELECT_SOURCE)
  })

  it('should not go to next step from last step', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    // Navigate to CONFIGURE
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
      result.current.nextStep()
    })

    // Navigate to FETCH_AND_MATCH
    act(() => {
      result.current.markStepComplete(WorkflowStep.CONFIGURE)
      result.current.nextStep()
    })

    // Navigate to CURATE
    act(() => {
      result.current.markStepComplete(WorkflowStep.FETCH_AND_MATCH)
      result.current.nextStep()
    })

    // Navigate to CREATE
    act(() => {
      result.current.markStepComplete(WorkflowStep.CURATE)
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CREATE)

    // Try to go to next step (should stay at CREATE)
    act(() => {
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CREATE)
  })

  it('should prevent navigation when locked', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    // Mark some steps as complete
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
      result.current.markStepComplete(WorkflowStep.CONFIGURE)
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)
    expect(result.current.canGoToStep(WorkflowStep.SELECT_SOURCE)).toBe(true)

    // Lock the workflow
    act(() => {
      result.current.lockWorkflow()
    })

    // Should not be able to navigate to any step
    expect(result.current.canGoToStep(WorkflowStep.SELECT_SOURCE)).toBe(false)
    expect(result.current.canGoToStep(WorkflowStep.CONFIGURE)).toBe(false)
    expect(result.current.canGoToStep(WorkflowStep.FETCH_AND_MATCH)).toBe(false)

    // Try to navigate - should stay at current step
    act(() => {
      result.current.goToStep(WorkflowStep.SELECT_SOURCE)
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)
  })

  it('should unlock workflow when reset', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    // Navigate forward and lock
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
      result.current.nextStep()
      result.current.lockWorkflow()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)
    expect(result.current.canGoToStep(WorkflowStep.SELECT_SOURCE)).toBe(false)

    // Reset workflow should unlock
    act(() => {
      result.current.resetWorkflow()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.SELECT_SOURCE)
    expect(result.current.canGoToStep(WorkflowStep.CONFIGURE)).toBe(false) // Not completed
    expect(result.current.completedSteps.size).toBe(0)
  })

  it('should prevent nextStep when locked', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    // Navigate forward
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)

    // Lock workflow
    act(() => {
      result.current.lockWorkflow()
    })

    // Try to advance - should be no-op
    act(() => {
      result.current.markStepComplete(WorkflowStep.CONFIGURE)
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)
  })

  it('should prevent previousStep when locked', () => {
    const { result } = renderHook(() => useWorkflow(), { wrapper })
    
    // Navigate forward
    act(() => {
      result.current.markStepComplete(WorkflowStep.SELECT_SOURCE)
      result.current.nextStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)

    // Lock workflow
    act(() => {
      result.current.lockWorkflow()
    })

    // Try to go back - should be no-op
    act(() => {
      result.current.previousStep()
    })

    expect(result.current.currentStep).toBe(WorkflowStep.CONFIGURE)
  })
})
