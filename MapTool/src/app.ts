import { readFile, writeFile } from "node:fs/promises"
import { Vector3 } from 'three'
import './generateUpdate.js'
import testMod from "./layouts/TestMod.js"
import alarkaJctAdditional from './layouts/alarkaJctAdditional.js'
import alarkaLoop from "./layouts/alarkaLoop.js"
import alarkaPaxStorage from './layouts/alarkaPaxStorage.js'
import andrewsInterchangeYard from './layouts/andrewsInterchangeYard.js'
import sylvaInterchangeYard from './layouts/sylvaInterchangeYard.js'
import sylvaPaperCrossover from './layouts/sylvaPaperCrossovers.js'
import sylvaPaxStorage from './layouts/sylvaPaxStorage.js'
import sylvaWye from './layouts/sylvaWye.js'
import whittierYard from './layouts/whittierYard.js'
import { AlinasMapModMixin } from './lib/AlinasMapMod.js'
import { Graph, Id, LayoutFunction, Mixins } from './lib/index.js'

async function run() {
  console.log(`Loading graph-json-dump.json...`)
  const graph = Graph.fromJSON(JSON.parse(await readFile("game-graph-dump.json", "utf8")))
  const layouts = {
    SylvaInterchangeYard: sylvaInterchangeYard,
    SylvaWye: sylvaWye,
    SylvaPaperCrossover: sylvaPaperCrossover,
    SylvaPaxStorage: sylvaPaxStorage,
    WhittierYard: whittierYard,
    AndrewsInterchangeYard: andrewsInterchangeYard,
    AlarkaJctAdditional: alarkaJctAdditional,
    AlarkaPaxStorage: alarkaPaxStorage,
    AlarkaLoop: alarkaLoop,
    // WalkerUraniumMine: walkerUraniumMine,
    TestMod: testMod,
  } as Record<string, LayoutFunction>
  const allMixins = [] as Mixins[]
  for (const [id, fn] of Object.entries(layouts)) {
    console.log(`Processing ${id}...`)
    graph.resetIdGenerator()
    const { mixins = {} } = await fn(graph, graph)
    allMixins.push(mixins)
  }
  
  graph.newNode(Id('AlinasMapMod'), new Vector3().random()) // tracking node to trigger updates
  const all = graph.toJSON()

  await writeFile(`../AlinasMapMod/game-graph.json`, JSON.stringify(all, null, 2))
  await writeFile(`../../AlinasMapMod/game-graph.json`, JSON.stringify(all, null, 2))

  const amm: AlinasMapModMixin = {
    items: {},
  }
  for (const mixin of allMixins) {
    if (!mixin.alinasMapMod) continue
    amm.items = Object.fromEntries([...Object.entries(amm.items), ...Object.entries(mixin.alinasMapMod.items)])
  }
  await writeFile(`../AlinasMapMod/AlinasMapMod.json`, JSON.stringify(amm, null, 2))
  await writeFile(`../../AlinasMapMod/AlinasMapMod.json`, JSON.stringify(amm, null, 2))

  const state = {
    progressions: {
      ewh: {
        sections: {} as any,
      },
    },
    mapFeatures: {} as any,
  }
  const mapToBool = (list: string[] = []) => Object.fromEntries(list.map(x => [x, true]))
  for(const [id, item] of Object.entries(amm.items)) {
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
  await writeFile(`../AlinasMapMod/progressions.json`, JSON.stringify(state, null, 2))
  await writeFile(`../../AlinasMapMod/progressions.json`, JSON.stringify(state, null, 2))

  const migrations = { waybillDestinations: {} as Record<string, string> }
  for(const [id, item] of Object.entries(amm.items)) {
    if (!item.industryComponent) continue
    migrations.waybillDestinations[`${item.identifier}.site`] = item.industryComponent
  }
  await writeFile(`../AlinasMapMod/game-migrations.json`, JSON.stringify(migrations, null, 2))
  await writeFile(`../../AlinasMapMod/game-migrations.json`, JSON.stringify(migrations, null, 2))
}

run().catch(console.error)