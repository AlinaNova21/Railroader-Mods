import type { ModEntry, ModsJson, UpdateEntry, UpdateJson } from '$lib/jsons'
import { json } from '@sveltejs/kit'
import type { RequestHandler } from './$types'

const updateMap = ([id, mod]: [string, ModEntry]) => [id, { name: mod.name, version: mod.version, changelog: mod.changelog } as UpdateEntry]

export const GET: RequestHandler = async ({ fetch }) => {
  const mods = await fetch('/mods.json').then(r => r.json()) as ModsJson
  const update: UpdateJson = {}
  for(const k in mods) {
    const { modId, version, changelog } = mods[k]
    update[modId] = { version, changelog }
  }
  return json(update)
}