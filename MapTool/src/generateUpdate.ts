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

interface Mod {
  id: string
  modId: string
  name: string
  assemblyName: string
  desc: string
  version: string
  downloadUrl: string
  changelog: ChangeLogEntry[]
}

async function run() {
  const mods = await glob('../*/mod.yaml').then(files => Promise.all(files.map(async f => YAML.parse(await readFile(f, 'utf8')) as Mod)))
  for (const mod of mods) {
    console.log(``)
    console.log(`# ${mod.name} v${mod.version}`)
    console.log(`https://railroader.alinanova.dev`)
    console.log(``)
    console.log(mod.changelog[0].desc)
    console.log(``)
    mod.assemblyName = mod.assemblyName || mod.modId.split('.')[1]
    mod.downloadUrl = `https://github.com/AlinaNova21/Railroader-Mods/releases/download/v${mod.version}/${mod.assemblyName}_${mod.version}.zip`
  } 
  await writeFile('../Site/static/mods.json', JSON.stringify(mods, null, 2))
}

run().catch(console.error)