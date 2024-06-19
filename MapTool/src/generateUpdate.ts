import { glob } from 'glob'
import { readFile, writeFile } from 'node:fs/promises'
import YAML from 'yaml'
import { Mod } from './lib/Mods.js'


export async function generateUpdate(layouts: Mod[]) {
  const mods = await glob('../*/mod.yaml').then(files => Promise.all(files.map(async f => YAML.parse(await readFile(f, 'utf8')) as Mod)))
  for (const mod of mods) {
    console.log(``)
    console.log(`# ${mod.name} v${mod.version}`)
    console.log(`https://railroader.alinanova.dev`)
    console.log(``)
    console.log(mod.changelog[0]?.desc || '')
    console.log(``)
    mod.assemblyName = mod.assemblyName || mod.modId.split('.')[1]
    mod.downloadUrl = mod.downloadUrl || `https://github.com/AlinaNova21/Railroader-Mods/releases/download/v${mod.version}/${mod.assemblyName}_${mod.version}.zip`
  }
  const amm = mods.find(mod => mod.modId === 'AlinaNova21.AlinasMapMod')
  if (!amm) throw new Error('AMM Not Found')
  for (const mod of layouts) {
    console.log(`# ${mod.name} v${mod.version}`)
    console.log(`https://railroader.alinanova.dev`)
    console.log(``)
    console.log(mod.changelog[0]?.desc || '')
    console.log(``)
    mod.assemblyName = mod.assemblyName || mod.modId.split('.')[1]
    mod.downloadUrl = `https://github.com/AlinaNova21/Railroader-Mods/releases/download/v${amm.version}/${mod.assemblyName}_${mod.version}.zip`
    mods.push(mod)
  }
  await writeFile('../Site/static/mods.json', JSON.stringify(mods, null, 2))
}