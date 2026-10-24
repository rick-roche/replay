import { describe, expect, it } from 'vitest'
import { getBuildIdentifier } from './buildIdentifier'

describe('getBuildIdentifier', () => {
  it('truncates production commit SHAs to seven characters', () => {
    expect(getBuildIdentifier('0123456789abcdef')).toBe('0123456')
  })

  it('uses development when no commit SHA is available', () => {
    expect(getBuildIdentifier()).toBe('development')
  })
})
