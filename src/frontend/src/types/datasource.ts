export const DataSource = {
  LASTFM: 'lastfm',
  DISCOGS: 'discogs',
  SETLISTFM: 'setlistfm'
} as const

export type DataSource = (typeof DataSource)[keyof typeof DataSource]

export interface DataSourceInfo {
  id: DataSource
  name: string
  description: string
  icon: string
  enabled: boolean
}
