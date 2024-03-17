
import { Vector3 } from 'three'

import { Graph, Id, idGenerator, isDirtySym } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

export default async function testMod(graph: Graph, originalTracks: Graph) {
  const area = `AN_Test_Mod`
  const { nid, sid, pid } = idGenerator('Test_Mod')
  
  const cranky = {
    id: Id('cranky'),
    position: { x: 4570, y: 528, z: 5433 },
    rotation: { x: 0, y: 160, z: 0 },
    scale: { x: 1, y: 1, z: 1 },
    modelIdentifier: "cranky",
    toJson() {
      return this
    }
  }
  // graph.scenery[Id('cranky')] = cranky
  graph[isDirtySym] = true

  // const orig = Graph.fromJSON(JSON.parse(await readFile("tracks.json", "utf8")))
  // const nodes = Object.keys(orig.nodes) as Id<TrackNode>[]

  // const seg = orig.newSegment(sid(), nodes[0], nodes[1])
  // console.log(orig.segments[seg.id] === seg)
  // // @ts-ignore
  // console.log(seg.__dirtyLog, seg[isDirtySym])
  // // orig.toJSON()
  // console.log(orig.toJSON(), orig[isDirtySym], )
  // console.log(orig.loads)

  return {
    name: 'Test Mod',
    mixins: {
      // whistles: [
      //   {
      //     "name": "Thomas Whistle 2",
      //     "clip": "file(thomas-whistle.ogg)",
      //     "model": {
      //       "assetPackIdentifier": "ttte",
      //       "assetIdentifier": "mdl-thomas-whistle"
      //     }
      //   }
      // ]
    }
  }
}
