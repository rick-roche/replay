export enum DataSource {
  LASTFM = 'lastfm',
  DISCOGS = 'discogs',
  SETLISTFM = 'setlistfm'
}

export interface DataSourceInfo {
  id: DataSource
  name: string
  description: string
  icon: string
  enabled: boolean
}
