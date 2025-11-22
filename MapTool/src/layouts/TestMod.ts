
import { Euler, Vector3 } from 'three'

import { Graph, Id, Industry, IndustryComponentType, PaxStationComponent, ProgressionsJson, TrackSpanPartEnd, createIndustry, createNode, createSegment, createTrackSpan, idGenerator } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

export default async function testMod(graph: Graph, originalTracks: Graph) {
  const zone = `AN_Test_Mod`
  
  const { nid, sid, pid } = graph.pushIdGenerator('Test_Mod')
  
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

  graph.createScenery('cranky', new Vector3(12930, 561, 4673), new Euler(0, 160, 0), new Vector3(1,1,1))

  const stalls = 5

  const tt1 = graph.createSpliney(Id('tt1'), 'AlinasMapMod.Turntable.TurntableBuilder', {
    position: new Vector3(12926, 561.25, 4648),
    rotation: new Vector3(0, 70, 0),
    roundhouseStalls: stalls
  });

  const center = new Vector3(12926, 561.25, 4648);

  const node1 = createNode(center.clone().add(new Vector3(0, 0, 30)), new Euler(0, 90, 0));
  const node2 = createNode(center.clone().add(new Vector3(70, 0, 30)), new Euler(0, 90, 0));
  const seg = createSegment(node1.id, node2.id);

  const points = []

  let lastPos = node1.position.clone().add(new Vector3(10, 0, 0))
  for(let i = 0; i < 5; i++) {
    points.push(lastPos.clone())
    lastPos = lastPos.add(new Vector3(10, 0, 0))
  }

  graph.createSpliney(Id('ammWaterColumn_z'), 'AlinasMapMod.LoaderBuilder', {
    prefab: 'vanilla://waterColumn',
    position: { x:0, y:680, z: 0 },
    rotation: { x: 0, y: 0, z: 0 },
  })

  const node_1 = createNode(new Vector3(0, 680, 0), new Euler(0, 0, 0))
  const node_2 = createNode(new Vector3(100, 680, 0), new Euler(0, 0, 0))
  createSegment(node_1.id, node_2.id)

  graph.createSpliney(Id('ammWaterColumn'), 'AlinasMapMod.LoaderBuilder', {
    prefab: 'vanilla://waterColumn',
    position: points.pop(),
    rotation: { x: 0, y: 0, z: 0 },
  })

  graph.createSpliney(Id('ammWaterTower'), 'AlinasMapMod.LoaderBuilder', {
    prefab: 'vanilla://waterTower',
    position: points.pop(),
    rotation: { x: 0, y: 0, z: 0 },
  })

  graph.createSpliney(Id('ammCoalConveyor'), 'AlinasMapMod.LoaderBuilder', {
    prefab: 'vanilla://coalConveyor',
    position: points.pop()?.add(new Vector3(0, 0, 15)),
    rotation: { x: 0, y: 0, z: 0 },
    Industry: 'wh-e-engine',
  })

  graph.createSpliney(Id('ammCoalTower'), 'AlinasMapMod.LoaderBuilder', {
    prefab: 'vanilla://coalTower',
    position: points.pop(),
    rotation: { x: 0, y: 0, z: 0 },
    Industry: 'wh-e-engine',
  })

  graph.createSpliney(Id('ammDieselFuelingStand'), 'AlinasMapMod.LoaderBuilder', {
    prefab: 'vanilla://dieselFuelingStand',
    position: points.pop()?.add(new Vector3(0, 0, 5)),
    rotation: { x: 0, y: -90, z: 0 },
    Industry: 'wh-e-engine',
  })

  var span1 = createTrackSpan({
    segmentId: Id('SREXSTEAMSHOP3.0xs3g'),
    distance: 20,
    end: TrackSpanPartEnd.Start,
  },
  {
    segmentId: Id('SREXSTEAMSHOP3.0xs3g'),
    distance: 60,
    end: TrackSpanPartEnd.Start,
  })

  var span2 = createTrackSpan({
    segmentId: Id('SREXSTEAMSHOP3.0xs3g'),
    distance: 20,
    end: TrackSpanPartEnd.Start,
  },
  {
    segmentId: Id('SREXSTEAMSHOP3.0xs3g'),
    distance: 60,
    end: TrackSpanPartEnd.Start,
  })

  var span3 = createTrackSpan({
    segmentId: Id('SREXSTEAMSHOP3.0xs3g'),
    distance: 20,
    end: TrackSpanPartEnd.Start,
  },
  {
    segmentId: Id('SREXSTEAMSHOP3.0xs3g'),
    distance: 60,
    end: TrackSpanPartEnd.Start,
  })

  var ind = createIndustry(Id('barkers'), Id('barkers-station'), 'Barkers Station')

  ind.newComponent<PaxStationComponent>('ammBarkersStation', "Barkers Creek", {
    type: IndustryComponentType.PaxStationComponent,
    timetableCode: 'BC',
    basePopulation: 10,
    loadId: Id('passengers'),
    trackSpans: [span1.id],
    branch: 'Main',
    neighborIds: [],
    carTypeFilter: '*',
    sharedStorage: true,
  })

  var ind = createIndustry(Id('connelly'), Id('connelly-station'), 'Connelly Station')

  ind.newComponent<PaxStationComponent>('ela', "Connelly", {
    type: IndustryComponentType.PaxStationComponent,
    timetableCode: 'CC',
    basePopulation: 10,
    loadId: Id('passengers'),
    trackSpans: [span1.id, span2.id, span3.id],
    branch: 'Main',
    neighborIds: [],
    carTypeFilter: '*',
    sharedStorage: true,
  })

  // graph.createSpliney(Id('ammBarkersStation'), 'AlinasMapMod.PaxBuilder', {
  //   industry: ind.id,
  //   spanIds: [span1.id],
  //   timetableCode: 'BARKERS',
  //   basePopulation: 10,
  // })

  
    // "REXSTEAMTT": {
    //   "handler": "AlinasMapMod.Turntable.TurntableBuilder",
    //   "position": { "x": 18229.0, "y": 580.0, "z": 810.0 },
    //   "rotation": { "x": 0, "y": -680, "z": 0 },
    //   "roundhouseStalls": 8
    // },

  for(let i = 1; i <= stalls; i++) {
    const seg = graph.newSegment(Id(`Stt1RoundhouseSegment${i}`), Id(`Ntt1TurntableNode${i}`), Id(`Ntt1RoundhouseNode${i}`))
    seg.groupId = "tt1_roundhouse"
  }

  const progressions: ProgressionsJson = {
    mapFeatures: {
      [`tt1_roundhouse`]: {
        displayName: `Whittier Test Roundhouse`,
        trackGroupsEnableOnUnlock: {
          tt1_roundhouse: true
        },
        trackGroupsAvailableOnUnlock: {
          tt1_roundhouse: true
        },
        gameObjectsEnableOnUnlock: {
          "path://scene/World/tt1": true
        }
      }
    },
    progressions: {}
  }

  // graph.createScenery("allen-dual-shed", new Vector3(4570, 528.83, 5451), new Euler(0, 70, 0), new Vector3(1, 1, 2));
  // graph.scenery[shed.id] = shed
  // graph.scenery[Id('cranky')] = cranky

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
      progressions,
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
