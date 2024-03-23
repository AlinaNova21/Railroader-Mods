import type { ModsJson } from '$lib/jsons'
import { redirect } from '@sveltejs/kit'
import type { PageLoad } from './$types'

export const load: PageLoad = async ({ params, fetch }) => {
  const mods = await fetch('/mods.json').then(r => r.json()) as ModsJson
  if (params.modId) {
    return { mod: mods.find(m => m.modId === params.modId) }
  }
  return redirect(307, '/')
}