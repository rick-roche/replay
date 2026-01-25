import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'

export const WorkflowStep = {
  SELECT_SOURCE: 'select-source',
  CONFIGURE: 'configure',
  FETCH_AND_MATCH: 'fetch-and-match',
  CURATE: 'curate',
  CREATE: 'create'
} as const

export type WorkflowStep = typeof WorkflowStep[keyof typeof WorkflowStep]

interface WorkflowContextValue {
  currentStep: WorkflowStep
  goToStep: (step: WorkflowStep) => void
  nextStep: () => void
  previousStep: () => void
  resetWorkflow: () => void
  canGoToStep: (step: WorkflowStep) => boolean
  completedSteps: Set<WorkflowStep>
  markStepComplete: (step: WorkflowStep) => void
}

const WorkflowContext = createContext<WorkflowContextValue | null>(null)

const STEP_ORDER: WorkflowStep[] = [
  WorkflowStep.SELECT_SOURCE,
  WorkflowStep.CONFIGURE,
  WorkflowStep.FETCH_AND_MATCH,
  WorkflowStep.CURATE,
  WorkflowStep.CREATE
]

export function WorkflowProvider({ children }: { children: ReactNode }) {
  const [currentStep, setCurrentStep] = useState<WorkflowStep>(WorkflowStep.SELECT_SOURCE)
  const [completedSteps, setCompletedSteps] = useState<Set<WorkflowStep>>(new Set())

  const getCurrentStepIndex = useCallback(() => {
    return STEP_ORDER.indexOf(currentStep)
  }, [currentStep])

  const canGoToStep = useCallback((step: WorkflowStep) => {
    // Can always go to a completed step or the current step
    if (completedSteps.has(step) || step === currentStep) {
      return true
    }
    
    // Check if all previous steps are completed
    const stepIndex = STEP_ORDER.indexOf(step)
    for (let i = 0; i < stepIndex; i++) {
      if (!completedSteps.has(STEP_ORDER[i]!)) {
        return false
      }
    }
    
    // If all previous steps are completed, can go to this step
    return true
  }, [completedSteps, currentStep])

  const goToStep = useCallback((step: WorkflowStep) => {
    if (canGoToStep(step)) {
      setCurrentStep(step)
    }
  }, [canGoToStep])

  const nextStep = useCallback(() => {
    const currentIndex = getCurrentStepIndex()
    if (currentIndex < STEP_ORDER.length - 1) {
      const nextStep = STEP_ORDER[currentIndex + 1]!
      setCurrentStep(nextStep)
    }
  }, [getCurrentStepIndex])

  const previousStep = useCallback(() => {
    const currentIndex = getCurrentStepIndex()
    if (currentIndex > 0) {
      const prevStep = STEP_ORDER[currentIndex - 1]!
      setCurrentStep(prevStep)
    }
  }, [getCurrentStepIndex])

  const resetWorkflow = useCallback(() => {
    setCurrentStep(WorkflowStep.SELECT_SOURCE)
    setCompletedSteps(new Set())
  }, [])

  const markStepComplete = useCallback((step: WorkflowStep) => {
    setCompletedSteps(prev => new Set([...prev, step]))
  }, [])

  const value: WorkflowContextValue = {
    currentStep,
    goToStep,
    nextStep,
    previousStep,
    resetWorkflow,
    canGoToStep,
    completedSteps,
    markStepComplete
  }

  return <WorkflowContext.Provider value={value}>{children}</WorkflowContext.Provider>
}

export function useWorkflow() {
  const context = useContext(WorkflowContext)
  if (!context) {
    throw new Error('useWorkflow must be used within WorkflowProvider')
  }
  return context
}

export { STEP_ORDER }
