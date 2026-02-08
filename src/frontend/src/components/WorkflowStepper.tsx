import { Check } from 'lucide-react'
import { Flex, Box, Text } from '@radix-ui/themes'
import { useWorkflow, WorkflowStep, STEP_ORDER } from '../contexts/WorkflowContext'

interface StepInfo {
  step: WorkflowStep
  label: string
  shortLabel: string
}

const STEP_INFO: StepInfo[] = [
  { step: WorkflowStep.SELECT_SOURCE, label: 'Select Source', shortLabel: 'Source' },
  { step: WorkflowStep.CONFIGURE, label: 'Configure', shortLabel: 'Config' },
  { step: WorkflowStep.FETCH_AND_MATCH, label: 'Fetch & Match', shortLabel: 'Match' },
  { step: WorkflowStep.CURATE, label: 'Curate', shortLabel: 'Curate' },
  { step: WorkflowStep.CREATE, label: 'Create', shortLabel: 'Create' }
]

export function WorkflowStepper() {
  const { currentStep, completedSteps, goToStep, canGoToStep } = useWorkflow()

  const currentIndex = STEP_ORDER.indexOf(currentStep)

  return (
    <Box className="w-full" mb="6">
      <Flex 
        direction={{ initial: 'column', sm: 'row' }} 
        gap="2" 
        align="center" 
        className="relative"
      >
        {STEP_INFO.map((info, index) => {
          const isCompleted = completedSteps.has(info.step)
          const isCurrent = currentStep === info.step
          const isClickable = canGoToStep(info.step) && !isCurrent
          const isPast = index < currentIndex

          return (
            <Flex 
              key={info.step} 
              direction="column" 
              align="center" 
              gap="2" 
              className="flex-1 relative"
            >
              {/* Connecting line (hidden on mobile, shown on desktop) */}
              {index > 0 && (
                <Box 
                  className="absolute top-4 right-1/2 w-full h-0.5 hidden sm:block"
                  style={{
                    backgroundColor: isPast || isCompleted 
                      ? 'var(--green-9)' 
                      : 'var(--gray-6)',
                    transform: 'translateY(-50%)',
                    zIndex: 0
                  }}
                />
              )}

              {/* Step circle */}
              <Flex
                align="center"
                justify="center"
                className={`
                  relative z-10 h-8 w-8 rounded-full border-2 transition-all cursor-pointer
                  ${isCurrent 
                    ? 'border-green-500 bg-green-500 shadow-lg shadow-green-500/50' 
                    : isCompleted 
                      ? 'border-green-500 bg-green-500' 
                      : 'border-gray-600 bg-gray-800'
                  }
                  ${isClickable ? 'hover:scale-110' : ''}
                `}
                onClick={() => isClickable && goToStep(info.step)}
                style={{ 
                  cursor: isClickable ? 'pointer' : 'default'
                }}
              >
                {isCompleted && !isCurrent ? (
                  <Check className="h-4 w-4 text-white" />
                ) : (
                  <Text 
                    size="1" 
                    weight="bold" 
                    className={isCurrent || isCompleted ? 'text-white' : 'text-gray-400'}
                  >
                    {index + 1}
                  </Text>
                )}
              </Flex>

              {/* Step label */}
              <Text 
                size={{ initial: '1', sm: '2' }}
                weight={isCurrent ? 'bold' : 'regular'}
                className={`
                  text-center transition-colors
                  ${isCurrent ? 'text-green-500' : isCompleted ? 'text-gray-300' : 'text-gray-500'}
                `}
              >
                <span className="hidden sm:inline">{info.label}</span>
                <span className="sm:hidden">{info.shortLabel}</span>
              </Text>
            </Flex>
          )
        })}
      </Flex>
    </Box>
  )
}
