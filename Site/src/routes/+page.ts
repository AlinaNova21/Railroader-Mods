import type { ModsJson } from '$lib/jsons'
import type { PageLoad } from './$types'

export const load: PageLoad = async ({ params, fetch }) => {
  return {
    mods: await fetch('/mods.json').then(r => r.json()) as ModsJson
  }
}