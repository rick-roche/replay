export function getBuildIdentifier(gitSha?: string): string {
  return gitSha?.slice(0, 7) || 'development'
}
