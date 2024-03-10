import { addHours, format } from 'date-fns'
import JSZip from 'jszip'
import { mkdir, readFile, readdir, rm, writeFile } from "node:fs/promises"
import alarkaJctAdditional from "./layouts/alarkaJctAdditional.js"
import andrewsInterchangeYard from "./layouts/andrewsInterchangeYard.js"
import sylvaInterchangeYard from "./layouts/sylvaInterchangeYard.js"
import sylvaWye from "./layouts/sylvaWye.js"
import whittierYard from './layouts/whittierYard.js'
import { AlinasMapModMixin } from './lib/AlinasMapMod.js'
import { Graph, LayoutFunction, Mixins } from './lib/index.js'


const build = format(addHours(new Date(), 6), 'yyDDD.Hmm', { useAdditionalDayOfYearTokens: true })

const VERSION = `1.0.${build}`

const replacer = function(key: string, value: any) {
  if (value instanceof Graph) {
    return value.toJSON()
  }
  return value
} 

async function run() {
  const empty = async(graph: Graph, ograph: Graph) => ({})
  console.log(`Loading tracks.json...`)
  const originalTracks = Graph.fromJSON(JSON.parse(await readFile("tracks.json", "utf8")))
  const layouts = {
    SylvaInterchangeYard: sylvaInterchangeYard,
    SylvaWye: sylvaWye,
    WhittierYard: whittierYard,
    AndrewsInterchangeYard: andrewsInterchangeYard,
    // AlinaSandbox: empty,
    AlarkaJctAdditional: alarkaJctAdditional,
  } as Record<string, LayoutFunction>
  const allGraphs = []
  const allMixins = []
  for(const [id, fn] of Object.entries(layouts)) {
    console.log(`Processing ${id}...`)
    const graph = new Graph()
    allGraphs.push(graph)
    const { name = "", mixins = {} } = await fn(graph, originalTracks)
    console.log(`Writing ${name}`)
    mixins.gameGraph = graph.toJSON();
    for (const [id, seg] of Object.entries(mixins.gameGraph.segments)) {
      seg.groupId = ''
    }
    allMixins.push(mixins)
    await generateMod(id, name, mixins)
    mixins.gameGraph = graph.toJSON();
    // const save = (file: string, value: any) => writeFile(`../${name}/${file}.json`, JSON.stringify(value, null, 2))
    // if (mixins.alinasMapMod) await save('AlinasMapMod', {}) //{ items: { [name]: mixins.alinasMapMod }})
    // if (mixins.gameGraph) await save('game-graph', {}) //mixins.gameGraph)
  }
  const all = Graph.mergeAll(allGraphs).toJSON()
  await writeFile(`../AlinasMapMod/game-graph.json`, JSON.stringify(all, null, 2))
  await writeFile(`../../AlinasMapMod/game-graph.json`, JSON.stringify(all, null, 2))
  
  const amm: AlinasMapModMixin = {
    items: {}
  }
  for (const mixin of allMixins) {
    if(!mixin.alinasMapMod) continue
    amm.items = Object.fromEntries([...Object.entries(amm.items), ...Object.entries(mixin.alinasMapMod.items)])
  }
  await writeFile(`../AlinasMapMod/AlinasMapMod.json`, JSON.stringify(amm, null, 2))
  await writeFile(`../../AlinasMapMod/AlinasMapMod.json`, JSON.stringify(amm, null, 2))

  Object.values(amm.items).forEach(item => {
    item.deliveryPhases.forEach(dp => dp.deliveries = [])
    item.trackSpans = []
  });
  await writeFile(`../AlinasMapMod/AlinasMapMod-NoDeliveries.json`, JSON.stringify(amm, null, 2))
}

async function generateMod(id: string, name: string, mixintos: Mixins) {
  const dir = `./dist/Release/${id}`
  await mkdir(dir, { recursive: true })

  const zip = new JSZip()

  const { version } = JSON.parse(await readFile("package.json", "utf8"))
  const definition = {
    manifestVersion: 5,
    id: `AlinaNova21.${id}`,
    name,
    version: VERSION,
    requires: [
      "Zamu.StrangeCustoms"
    ],
    mixintos: {} as Record<string, string>
  }
  for (const [type,contents] of Object.entries(mixintos)) {
    const key = type === 'gameGraph' ? 'game-graph' : type
    const filename = `${type}.json`
    definition.mixintos[key] = `file(${filename})`
    const file = `${dir}/${filename}`
    const rawJson = JSON.stringify(contents, replacer, 2)
    await writeFile(file, rawJson)
    zip.file(`Mods/${id}/${filename}`, rawJson, { createFolders: false })
  }
  const rawJson = JSON.stringify(definition, null, 2)
  await writeFile(`${dir}/Definition.json`, rawJson)
  zip.file(`Mods/${id}/Definition.json`, rawJson, { createFolders: true })

  const oldZip = (await readdir(`../bin/`))
    .find(f => f.startsWith(id) || f.startsWith(name))
  if (oldZip) await rm(`../bin/${oldZip}`)
  await writeFile(`../bin/${id}_${VERSION}.zip`, await zip.generateAsync({ type: 'nodebuffer' }))
}

run().catch(console.error)