import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { WorkflowProvider } from '../contexts/WorkflowContext'
import { WorkflowStepper } from './WorkflowStepper'

describe('WorkflowStepper', () => {
  it('should render all steps', () => {
    render(
      <WorkflowProvider>
        <WorkflowStepper />
      </WorkflowProvider>
    )

    // Use getAllByText to handle responsive duplicates
    expect(screen.getAllByText('Select Source').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Configure').length).toBeGreaterThan(0)
    expect(screen.getAllByText('Fetch & Match').length).toBeGreaterThan(0)
    expect(screen.getAllByText(/curate/i).length).toBeGreaterThan(0)
    expect(screen.getAllByText(/create/i).length).toBeGreaterThan(0)
  })

  it('should highlight the current step', () => {
    const { container } = render(
      <WorkflowProvider>
        <WorkflowStepper />
      </WorkflowProvider>
    )

    // The first step (1) should be highlighted (current)
    expect(screen.getByText('1')).toBeInTheDocument()
    
    // Check for green-500 styling on the first step's parent span
    const greenElements = container.querySelectorAll('.text-green-500')
    expect(greenElements.length).toBeGreaterThan(0)
  })
})
