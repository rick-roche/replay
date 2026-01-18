import '@testing-library/jest-dom/vitest'

// Polyfill ResizeObserver for Radix UI components in JSDOM
class ResizeObserverMock {
	observe() {}
	unobserve() {}
	disconnect() {}
}

// @ts-expect-error jsdom environment lacks ResizeObserver
global.ResizeObserver = ResizeObserverMock as unknown as ResizeObserver
