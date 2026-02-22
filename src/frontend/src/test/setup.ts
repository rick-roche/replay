import '@testing-library/jest-dom/vitest'

// Polyfill ResizeObserver for Radix UI components in JSDOM
class ResizeObserverMock {
	observe() {}
	unobserve() {}
	disconnect() {}
}

// @ts-expect-error jsdom environment lacks ResizeObserver
global.ResizeObserver = ResizeObserverMock as unknown as ResizeObserver

// Polyfill localStorage for JSDOM
const localStorageMock = (() => {
	let store: Record<string, string> = {}

	return {
		getItem: (key: string) => store[key] || null,
		setItem: (key: string, value: string) => {
			store[key] = value.toString()
		},
		removeItem: (key: string) => {
			delete store[key]
		},
		clear: () => {
			store = {}
		},
		get length() {
			return Object.keys(store).length
		},
		key: (index: number) => {
			const keys = Object.keys(store)
			return keys[index] || null
		},
	}
})()

Object.defineProperty(window, 'localStorage', {
	value: localStorageMock,
})
