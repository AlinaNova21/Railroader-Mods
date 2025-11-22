import { glob } from 'glob'
import { readFile, writeFile } from 'node:fs/promises'
import YAML from 'yaml'
import { Mod } from './lib/Mods.js'


export async function generateUpdate(layouts: Mod[]) {
  const mods = await glob('../*/mod.yaml').then(files => Promise.all(files.map(async f => YAML.parse(await readFile(f, 'utf8')) as Mod)))
  const amm = mods.find(mod => mod.modId === 'AlinaNova21.AlinasMapMod')
  if (!amm) throw new Error('AMM Not Found')
  console.log(`Found AMM v${amm.version}`)
  console.log(`${mods.length} mods found`)
  for (const mod of mods) {
    console.log(``)
    console.log(`# ${mod.name} v${mod.version}`)
    console.log(`https://railroader.alinanova.dev`)
    console.log(``)
    console.log(mod.changelog[0]?.desc || '')
    console.log(``)
    mod.assemblyName = mod.assemblyName || mod.modId.split('.')[1]
    mod.downloadUrl = mod.downloadUrl || `https://whoverse.nyc3.digitaloceanspaces.com/railroader-mods/${mod.assemblyName}_${mod.version}.zip`
  }
  for (const mod of layouts) {
    if (mod.id == 'AlinaNova21.AMM_TestMod') continue
    console.log(`# ${mod.name} v${mod.version}`)
    console.log(`https://railroader.alinanova.dev`)
    console.log(``)
    console.log(mod.changelog[0]?.desc || '')
    console.log(``)
    mod.assemblyName = mod.assemblyName || mod.modId.split('.')[1]
    mod.downloadUrl = `https://whoverse.nyc3.digitaloceanspaces.com/railroader-mods/${mod.assemblyName}_${mod.version}.zip`
    mods.push(mod)
  }
  mods.forEach(mod => console.log(mod.modId))
  await writeFile('../Site/static/mods.json', JSON.stringify(mods, null, 2))
}