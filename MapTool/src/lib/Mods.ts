export interface ChangeLogEntry {
  version: string
  desc: string
}

export interface ModEntry {
  version: string
  changelog: ChangeLogEntry[]
}

type Update = Record<string, ModEntry>

export interface Mod {
  id: string
  modId: string
  name: string
  assemblyName?: string
  desc: string
  version: string
  downloadUrl?: string
  changelog: ChangeLogEntry[]
}
