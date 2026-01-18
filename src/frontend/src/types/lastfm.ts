export interface ConfigureLastfmRequest {
  username: string
}

export interface ConfigureLastfmResponse {
  username: string
  playCount: number
  isConfigured: boolean
}

export const LastfmDataType = {
  Tracks: 'Tracks',
  Albums: 'Albums',
  Artists: 'Artists'
} as const

export type LastfmDataType = (typeof LastfmDataType)[keyof typeof LastfmDataType]

export const LastfmTimePeriod = {
  Last7Days: 'Last7Days',
  Last1Month: 'Last1Month',
  Last3Months: 'Last3Months',
  Last6Months: 'Last6Months',
  Last12Months: 'Last12Months',
  Overall: 'Overall',
  Custom: 'Custom'
} as const

export type LastfmTimePeriod = (typeof LastfmTimePeriod)[keyof typeof LastfmTimePeriod]

export interface LastfmFilter {
  dataType: LastfmDataType
  timePeriod: LastfmTimePeriod
  customStartDate?: string
  customEndDate?: string
  maxResults: number
}

export interface LastfmTrack {
  name: string
  artist: string
  album?: string
  playCount: number
}

export interface LastfmAlbum {
  name: string
  artist: string
  playCount: number
}

export interface LastfmArtist {
  name: string
  playCount: number
}

export interface LastfmDataResponse {
  dataType: LastfmDataType
  tracks: LastfmTrack[]
  albums: LastfmAlbum[]
  artists: LastfmArtist[]
  totalResults: number
}
