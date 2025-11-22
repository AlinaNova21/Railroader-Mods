import type { ModsJson } from '$lib/jsons'
import { error, redirect } from '@sveltejs/kit'
import type { PageLoad } from './$types'

export const load: PageLoad = async ({ params, fetch }) => {
  const mods = await fetch('/mods.json').then(r => r.json()) as ModsJson
  if (params.modId) {
    const mod = mods.find(m => m.id === params.modId)
    if (!mod) return error(404, 'Mod not found')
    return { mod }
  }
  return redirect(307, '/')
}