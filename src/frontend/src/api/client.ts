import createClient from 'openapi-fetch'
import type { paths } from './generated-client'

// Typed OpenAPI client using openapi-fetch
export const client = createClient<paths>({
  baseUrl: typeof window !== 'undefined' ? window.location.origin : 'http://localhost',
  fetch: (input: RequestInfo | URL, init?: RequestInit) => fetch(input, init)
})
