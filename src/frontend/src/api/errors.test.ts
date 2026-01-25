import { describe, it, expect } from 'vitest'
import { handleApiError } from './errors'

describe('handleApiError', () => {
  it('throws error with API error message', () => {
    const error = {
      message: 'User not found',
      code: 'NOT_FOUND'
    }
    
    expect(() => {
      handleApiError(error, 'Fallback message')
    }).toThrow('User not found')
  })

  it('throws error with code when message is undefined', () => {
    const error = {
      code: 'INVALID_REQUEST'
    }
    
    expect(() => {
      handleApiError(error, 'Fallback message')
    }).toThrow('INVALID_REQUEST')
  })

  it('throws error with fallback message when error is undefined', () => {
    expect(() => {
      handleApiError(undefined, 'Fallback message')
    }).toThrow('Fallback message')
  })

  it('throws error with fallback when error object has no message or code', () => {
    const error = {}
    
    expect(() => {
      handleApiError(error, 'Operation failed')
    }).toThrow('Operation failed')
  })

  it('prioritizes message over code', () => {
    const error = {
      message: 'Specific error',
      code: 'GENERIC_CODE'
    }
    
    expect(() => {
      handleApiError(error, 'Fallback')
    }).toThrow('Specific error')
  })

  it('handles null error object', () => {
    expect(() => {
      handleApiError(null, 'Network error')
    }).toThrow('Network error')
  })
})
