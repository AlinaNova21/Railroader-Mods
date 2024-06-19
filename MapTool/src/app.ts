import JSZip from "jszip"
import { createWriteStream } from "node:fs"
import { mkdir, readFile, readdir, unlink, writeFile } from "node:fs/promises"
import { generateUpdate } from "./generateUpdate.js"
import alarkaJctAdditional from './layouts/alarkaJctAdditional.js'
import alarkaLoop from "./layouts/alarkaLoop.js"
import alarkaPaxStorage from './layouts/alarkaPaxStorage.js'
import andrewsInterchangeYard from './layouts/andrewsInterchangeYard.js'
import sylvaInterchangeYard from './layouts/sylvaInterchangeYard.js'
import sylvaPaperCrossover from './layouts/sylvaPaperCrossovers.js'
import sylvaPaxStorage from './layouts/sylvaPaxStorage.js'
import sylvaWye from './layouts/sylvaWye.js'
import whittierSawmillConnection from "./layouts/whittierSawmillConnection.js"
import whittierYard from './layouts/whittierYard.js'
import { AlinasMapModMixin } from './lib/AlinasMapMod.js'
import { Mod } from "./lib/Mods.js"
import { Graph, LayoutFunction, Mixins, ModReference } from './lib/index.js'

async function run() {
  const baseRequires: ModReference[] = [
    {
      id: 'railloader',
      notBefore: '1.8.0'
    },
    {
      id: 'Zamu.StrangeCustoms',
      notBefore: '1.6.24125.1439'
    },
    {
      id: 'AlinaNova21.AlinasMapMod',
      notBefore: '1.4.0'
    }
  ]
  const baseConflicts: ModReference[] = [
    {
      id: 'AlinaNova21.AlinasMapMod',
      notAfter: '1.3.24149.1337',
    }
  ]
  console.log(`Loading game-graph-dump.json...`)
  const dumpJson = JSON.parse(await readFile("game-graph-dump.json", "utf8"))
  const graph = Graph.fromJSON(dumpJson)
  const layouts = {
    AlarkaJctAdditional: alarkaJctAdditional,
    AlarkaLoop: alarkaLoop,
    AlarkaPaxStorage: alarkaPaxStorage,
    AndrewsInterchangeYard: andrewsInterchangeYard,
    SylvaInterchangeYard: sylvaInterchangeYard,
    SylvaPaperCrossover: sylvaPaperCrossover,
    SylvaPaxStorage: sylvaPaxStorage,
    SylvaWye: sylvaWye,
    WhittierSawmillConnection: whittierSawmillConnection,
    WhittierYard: whittierYard,
    // WalkerUraniumMine: walkerUraniumMine,
    // TestMod: testMod,
  } as Record<string, LayoutFunction>
  const allMixins = [] as Mixins[]
  const { version: toolVersion } = JSON.parse(await readFile('package.json', 'utf8'))
  const binDir = await readdir(`../bin`)
  for(const file of binDir) {
    if (file.startsWith('AMM_')) {
      await unlink(`../bin/${file}`)
    }
  }
  const mods: Mod[] = []
  for (const [id, fn] of Object.entries(layouts)) {
    const modPath1 = `dist/AMM_${id}`
    const modPath2 = `../../AMM_${id}`
    // const modPath = `../../AMM_${id}`
    console.log(`Processing ${id}...`)
    const graph = Graph.fromJSON(dumpJson)
    graph.activate()
    const { 
      name,
      desc = '',
      version = toolVersion,
      changelog = [],
      mixins = {},
      conflicts = [],
      requires = []
    } = await fn(graph, graph)
    // requires.push(...ammDefinition.requires)
    requires.unshift(...baseRequires)
    conflicts.unshift(...baseConflicts)
    mixins['game-graph'] = graph.toJSON()
    if (mixins.alinasMapMod) {
      console.warn(`WARNING: Layout ${id} is using old mixin. Update to using progressions.`)
      const newMixins = convertLegacyAMMMixin(mixins.alinasMapMod)
      delete mixins.alinasMapMod
      Object.assign(mixins, newMixins)
    }
    const zipPath = `../bin/AMM_${id}_${version}.zip`
    const zip = new JSZip()
    await mkdir(`${modPath1}`, { recursive: true })
    await mkdir(`${modPath2}`, { recursive: true })
    const write = async (file: string, data: string) => {
      zip.file(`mods/AMM_${id}/${file}`, data)
      await writeFile(`${modPath1}/${file}`, data)
      await writeFile(`${modPath2}/${file}`, data)
    }

    const mixintos: { [key: string]: string } = {}
    for(const [id, mixin] of Object.entries(mixins)) {
      write(`${id}.json`, JSON.stringify(mixin, null, 2))
      mixintos[id] = `file(${id}.json)`
    }
    const definition = {
      manifestVersion: 5,
      id: `AlinaNova21.AMM_${id}`,
      name,
      version,
      requires,
      mixintos,
      conflictsWith: conflicts,
      updateUrl: 'https://railroader.alinanova.dev/update.json',
    }
    write('Definition.json', JSON.stringify(definition, null, 2))
    zip.generateNodeStream().pipe(createWriteStream(zipPath))
    const snakeId = `amm${id.replace(/[A-Z]/g, l => `-${l.toLowerCase()}`)}`
    mods.push({
      id: snakeId,
      modId: `AlinaNova21.AMM_${id}`,
      name,
      desc,
      version,
      changelog,
    })
  }
  generateUpdate(mods)
}  

function convertLegacyAMMMixin(amm: AlinasMapModMixin) {
  const state = {
    progressions: {
      ewh: {
        sections: {} as any,
      },
    },
    mapFeatures: {} as any,
  }
  const mapToBool = (list: string[] = []) => Object.fromEntries(list.map(x => [x, true]))
  for (const [id, item] of Object.entries(amm.items)) {
    item.deliveryPhases.forEach(dp => {
      dp.industryComponent = item.industryComponent
    })
    state.mapFeatures[item.identifier] = {
      displayName: item.name,
      name: item.name,
      trackGroupsEnableOnUnlock: mapToBool(item.groupIds),
      trackGroupsAvailableOnUnlock: mapToBool(item.groupIds),
    }
    state.progressions.ewh.sections[item.identifier] = {
      displayName: item.name,
      deliveryPhases: item.deliveryPhases,
      enableFeaturesOnUnlock: mapToBool([item.identifier]),
      prerequisiteSections: mapToBool(item.prerequisiteSections),
      description: item.description,
    }
  }
  return {
    progressions: state
  }
}

run().catch(console.error)