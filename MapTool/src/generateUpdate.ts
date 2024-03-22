import { glob } from 'glob'
import { readFile, writeFile } from 'node:fs/promises'
import { dirname } from 'node:path'
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
  name: string
  desccription: string
  version: string
  downloadUrl: string
  changelog: ChangeLogEntry[]
}

async function run() {
  const update = {} as Update
  const mods = {} as Record<string,Mod>
  const changelogs = await glob('../**/changelog.yaml', { ignore: ['**/node_modules/**'] })
  for (const changelogFile of changelogs) {
    const dir = dirname(changelogFile)
    const changelog = YAML.parse(await readFile(changelogFile, 'utf8')) as ChangeLogEntry[]
    const definition = JSON.parse(await readFile(`${dir}/definition.json`, 'utf8')) as Definition
    const version = changelog[0].version
    const [,name] = definition.id.split('.')
    update[definition.id] = {
      version,
      changelog
    }
    mods[definition.id] = {
      name,
      desccription: '',
      changelog,
      version,
      downloadUrl: `https://github.com/AlinaNova21/Railroader-Mods/releases/download/v${version}/${name}_${version}.zip`
    }
  }
  await writeFile('../Site/static/update.json', JSON.stringify(update, null, 2))
  await writeFile('../Site/static/mods.json', JSON.stringify(mods, null, 2))
}

run().catch(console.error)