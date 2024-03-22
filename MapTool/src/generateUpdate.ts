import { glob } from 'glob'
import { readFile, writeFile } from 'node:fs/promises'
import YAML from 'yaml'

interface ChangeLogEntry {
  version: string
  desc: string
}

interface ModEntry {
  version: string
  changelog: ChangeLogEntry[]
} 

type Update = Record<string, ModEntry>

interface Definition {
  id: string
}

interface Mod {
  id: string
  name: string
  desc: string
  version: string
  downloadUrl: string
  changelog: ChangeLogEntry[]
}

async function run() {
  const output = {} as Record<string,Mod>
  const modList = await glob('../*/mod.yaml').then(files => Promise.all(files.map(async f => YAML.parse(await readFile(f, 'utf8')) as Mod)))
  const mods = Object.fromEntries(modList.map(mod => [mod.id, mod]))
  for(const mod of Object.values(mods)) {
    const name = mod.id.split('.')[1]
    mod.downloadUrl = `https://github.com/AlinaNova21/Railroader-Mods/releases/download/v${mod.version}/${name}_${mod.version}.zip`
  } 
  await writeFile('../Site/static/mods.json', JSON.stringify(mods, null, 2))
  const update = {} as Update
  for(const { id, version, changelog } of modList) {
    update[id] = { version, changelog }
  }
  await writeFile('../Site/static/update.json', JSON.stringify(update, null, 2))
}

run().catch(console.error)