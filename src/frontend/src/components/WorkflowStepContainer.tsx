import { type ReactNode } from 'react'
import { ChevronDown, ChevronRight } from 'lucide-react'
import { Box, Card, Flex, Heading, Button } from '@radix-ui/themes'
import { useWorkflow, WorkflowStep } from '../contexts/WorkflowContext'

interface WorkflowStepContainerProps {
  step: WorkflowStep
  title: string
  children: ReactNode
  onComplete?: () => void
  canComplete?: boolean
}

export function WorkflowStepContainer({
  step,
  title,
  children,
  onComplete,
  canComplete = true
}: WorkflowStepContainerProps) {
  const { currentStep, completedSteps, goToStep, canGoToStep } = useWorkflow()

  const isActive = currentStep === step
  const isCompleted = completedSteps.has(step)
  const canNavigateTo = canGoToStep(step) && !isActive

  if (!isActive && !isCompleted) {
    // Step not yet reached - don't render
    return null
  }

  if (isCompleted && !isActive) {
    // Collapsed state - show summary
    return (
      <Card>
        <Flex
          align="center"
          justify="between"
          onClick={() => canNavigateTo && goToStep(step)}
          className={canNavigateTo ? 'cursor-pointer hover:bg-gray-800/50' : ''}
        >
          <Flex align="center" gap="3" py="2">
            <ChevronRight className="h-4 w-4 text-gray-500" />
            <Heading size="4" weight="medium" className="text-gray-400">
              {title}
            </Heading>
          </Flex>
          {canNavigateTo && (
            <Button variant="ghost" size="1" className="text-gray-500">
              Edit
            </Button>
          )}
        </Flex>
      </Card>
    )
  }

  // Active state - show full content
  return (
    <Card>
      <Flex direction="column" gap="4">
        <Flex align="center" gap="3">
          <ChevronDown className="h-4 w-4 text-green-500" />
          <Heading size="5" weight="bold" className="text-green-500">
            {title}
          </Heading>
        </Flex>
        
        <Box>{children}</Box>

        {onComplete && (
          <Flex justify="end" pt="2">
            <Button 
              onClick={onComplete} 
              disabled={!canComplete}
              size="3"
            >
              Continue
            </Button>
          </Flex>
        )}
      </Flex>
    </Card>
  )
}
