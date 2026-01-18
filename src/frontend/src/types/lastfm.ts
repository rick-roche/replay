export interface ConfigureLastfmRequest {
  username: string
}

export interface ConfigureLastfmResponse {
  username: string
  playCount: number
  isConfigured: boolean
}
