export interface ChangeLogEntry {
  version: string
  desc: string
}

export interface UpdateEntry {
  version: string
  changelog: ChangeLogEntry[]
}

export interface ModEntry {
  id: string
  modId: string
  assemblyName: string
  name: string
  desc: string
  version: string
  downloadUrl: string
  changelog: ChangeLogEntry[]
}

export type UpdateJson = Record<string, UpdateEntry>
export type ModsJson = ModEntry[]