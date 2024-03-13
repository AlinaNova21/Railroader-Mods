import { readFile, writeFile } from "node:fs/promises"
import { Vector3 } from 'three'
import alarkaJctAdditional from './layouts/alarkaJctAdditional.js'
import andrewsInterchangeYard from './layouts/andrewsInterchangeYard.js'
import sylvaInterchangeYard from './layouts/sylvaInterchangeYard.js'
import sylvaWye from './layouts/sylvaWye.js'
import whittierYard from './layouts/whittierYard.js'
import { AlinasMapModMixin } from './lib/AlinasMapMod.js'
import { Graph, Id, LayoutFunction, Mixins } from './lib/index.js'

async function run() {
  console.log(`Loading tracks.json...`)
  const graph = Graph.fromJSON(JSON.parse(await readFile("tracks.json", "utf8")))
  const layouts = {
    SylvaInterchangeYard: sylvaInterchangeYard,
    SylvaWye: sylvaWye,
    WhittierYard: whittierYard,
    AndrewsInterchangeYard: andrewsInterchangeYard,
    AlarkaJctAdditional: alarkaJctAdditional,
    // WalkerUraniumMine: walkerUraniumMine,
    // TestMod: testMod,
  } as Record<string, LayoutFunction>
  const allMixins = [] as Mixins[]
  for (const [id, fn] of Object.entries(layouts)) {
    console.log(`Processing ${id}...`)
    const { mixins = {} } = await fn(graph, graph)
    allMixins.push(mixins)
  }
  
  graph.newNode(Id('AlinasMapMod'), new Vector3().random()) // tracking node to trigger updates
  const all = graph.toJSON()

  await writeFile(`../AlinasMapMod/game-graph.json`, JSON.stringify(all, null, 2))
  await writeFile(`../../AlinasMapMod/game-graph.json`, JSON.stringify(all, null, 2))

  const amm: AlinasMapModMixin = {
    items: {}
  }
  for (const mixin of allMixins) {
    if (!mixin.alinasMapMod) continue
    amm.items = Object.fromEntries([...Object.entries(amm.items), ...Object.entries(mixin.alinasMapMod.items)])
  }
  await writeFile(`../AlinasMapMod/AlinasMapMod.json`, JSON.stringify(amm, null, 2))
  await writeFile(`../../AlinasMapMod/AlinasMapMod.json`, JSON.stringify(amm, null, 2))
}

run().catch(console.error)