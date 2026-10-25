/**
 * Helper to extract and throw typed API errors from openapi-fetch responses.
 * Use this across all API wrappers for consistent error handling.
 * 
 * @param error - The error object from openapi-fetch
 * @param fallbackMessage - Default message if no error details available
 * @throws Error with the API error message or fallback
 * 
 * @example
 * const { data, error } = await client.POST('/api/endpoint', { body })
 * if (error) handleApiError(error, 'Operation failed')
 * return data!
 */
export function handleApiError(error: unknown, fallbackMessage: string): never {
  if (typeof error === 'object' && error !== null) {
    if ('message' in error && typeof error.message === 'string' && error.message.trim()) throw new Error(error.message)
    if ('code' in error && typeof error.code === 'string' && error.code.trim()) throw new Error(error.code)
  }
  throw new Error(fallbackMessage)
}
