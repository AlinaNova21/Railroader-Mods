
import { Euler, MathUtils, Vector3 } from 'three'

import { AlinasMapModMixin, AlinasMapModMixinItem } from '../lib/AlinasMapMod.js'
import { Area, Graph, Id, Industry, IndustryComponentId, IndustryComponentType, Segment, TrackNode, TrackSpan, TrackSpanPartEnd, idGenerator, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

/** @param {Graph} graph */
export default async function whittierYard(graph: Graph, originalTracks: Graph) {
  const tracks = 9

  const zone = `AN_Whittier_Yard`
  const sawmillGroup = `${zone}_Sawmill`
  const extGroup = `${zone}_Ext`
  const leadGroup = `${zone}_Yard_Lead`
  const trackGroupFn = (i: number) => `${zone}_Yard_Track_${i}`
  const noGroupSids = [] as Id<Segment>[]

  const groups = {} as Record<string, Id<Segment>[]>


  const { nid, sid, pid } = idGenerator(zone);

  // const interchange1 = graph.newNode(Id('N9m4'), new Vector3(0, 561.25, 0))
  const interchange1 = graph.importNode(originalTracks.getNode(Id('Njk9')))
  // const interchange1 = graph.importNode(originalTracks.getNode(Id('N5es')))
  // const interchange1 = graph.importNode(originalTracks.getNode(Id('N731')))
  const interchange2 = graph.importNode(originalTracks.getNode(Id('N1be')))
  // const sawmill = graph.importNode(originalTracks.getNode(Id('N72a')))
  const sawmill = graph.importNode(originalTracks.getNode(Id('N72a')))
  const sawmill2 = graph.importNode(originalTracks.getNode(Id('Nijv')))

  const start = graph.newNode(nid(), new Vector3(13155, 561.25, 4415))
  const yard00 = graph.newNode(nid(), new Vector3(12915, 561.25, 4564), new Euler(0, 312, 0))
  const yard01 = yard00.extend(nid(), sid(), 120, -1, 0, sawmillGroup)
  
  const inSeg = graph.importSegment(originalTracks.getSegment(Id("Sj4c")));

  const inext1 = interchange1.extend(nid(), sid(), 50, 0, 0, extGroup)
  // const inext2 = inext1.extend(nid(), sid(), 30, 0, 0, extGroup)
  const inext3 = inext1.extend(nid(), sid(), 30, 180+5, 5, leadGroup)

  inext1.flipSwitchStand = true

  // inext1.position.y += 1
  inSeg.endId = inext1.id
  // inext1
  //   .extend(nid(), sid(), 10, 10)
  //   .extend(nid(), sid(), 10, 10)
  //   .extend(nid(), sid(), 10, 10)
  const inseg2 = start.toNode(sid(), inext3)
  inseg2.priority = -1
  inseg2.groupId = leadGroup

  start.rotation.y += 300 //+ -0.5

  const y0seg = yard00.toNode(sid(), interchange2)
  const y1seg = yard01.toNode(sid(), sawmill)

  y0seg.priority = -1
  y0seg.groupId = sawmillGroup
  y1seg.groupId = sawmillGroup

  yard01.rotation.y += 1.6
  yard01.position.x += 4
  yard01.position.z += 1

  interchange1.flipSwitchStand = true
  interchange2.flipSwitchStand = true

  const entryNodes: TrackNode[] = []
  const exitNodes: TrackNode[] = []

  const switchAngle = 12
  const leadLength = 20
  const yardLength = 250

  for (let i = 0; i < tracks; i++) {
    const trackGroup = trackGroupFn(i)
    const entry = (entryNodes.slice(-1)[0] || start)
      .extend(Id(`N${zone}_T${i}_00`), Id(`S${zone}_L${i}_00`), 20, 0, 0, leadGroup)

    entryNodes.push(entry)

    const dir = new Vector3(0, 0, 1)
    dir.applyAxisAngle(UP, MathUtils.degToRad(entry.rotation.y + switchAngle))

    const offset = new Vector3(0, 0, 1)
      .applyAxisAngle(UP, MathUtils.degToRad(entry.rotation.y))
      .multiplyScalar(12)

    const forwardPos = offset.clone().add(entry.position)

    const trackPos1 = forwardPos.clone().add(dir.clone().multiplyScalar(leadLength))
    const trackPos2 = forwardPos.clone().add(dir.clone().multiplyScalar(leadLength + yardLength))
    const exitPos = forwardPos.clone().add(dir.clone().multiplyScalar(leadLength + yardLength + leadLength))
    exitPos.add(offset)
    // trackPos2.y -= 0.5
    // exitPos.y -= 0.5

    const trackNode1 = graph.newNode(Id(`N${zone}_T${i}_01`), trackPos1, new Euler(0, switchAngle + entry.rotation.y, 0))
    const trackNode2 = graph.newNode(Id(`N${zone}_T${i}_02`), trackPos2, new Euler(0, switchAngle + entry.rotation.y, 0))
    const exit = graph.newNode(Id(`N${zone}_T${i}_03`), exitPos, entry.rotation.clone())

    const entrySeg = entry.toNode(Id(`S${zone}_T${i}_00`), trackNode1)
    const trackSeg = trackNode1.toNode(Id(`S${zone}_T${i}_01`), trackNode2)
    const exitSeg = trackNode2.toNode(Id(`S${zone}_T${i}_02`), exit)

    entrySeg.groupId = trackGroup
    trackSeg.groupId = trackGroup
    exitSeg.groupId = trackGroup

    entrySeg.priority = -1
    exitSeg.priority = -1

    exitNodes.push(exit)
  }

  start.position.add(new Vector3(0, 0, -0.5))
  start.rotation.y += 3


  const ss1 = graph.importSegment(originalTracks.getSegment(Id('Sj6n')))
  const ext1 = sawmill2.extend(nid(), sid(), 2, 0, 0, extGroup)
  const ssId = sid.last()
  const ext2 = ext1.extend(nid(), sid(), 30, 0, 0, extGroup)

  ss1.startId = ext2.id
  console.log(sawmill2.rotation, ext1.rotation)
  const ext3 = ext1.extend(nid(), sid(), 30, -5, 0, leadGroup)
  // ext1.position.y += 0.5
  exitNodes.push(ext3)
  ext1.flipSwitchStand = true

  for (let i = 1; i < exitNodes.length; i++) {
    exitNodes[i - 1].toNode(Id(`S${zone}_L${i - 1}_01`), exitNodes[i]).groupId = leadGroup
  }

  Object.entries(graph.segments).forEach(([id, segment]) => {
    if(!id.startsWith(`S${zone}`) || noGroupSids.includes(Id(id))) return
    if(segment.groupId == "") console.log(`Warning: Segment ${id} has no groupId!`)
    if(segment.groupId === extGroup) segment.groupId = ""
    //   segment.groupId = area
  })

  const site1 = graph.newSpan(pid(), {
    segmentId: Id("Sj6n"),
    end: TrackSpanPartEnd.End,
    distance: 0,
  }, {
    segmentId: Id("S6if"),
    end: TrackSpanPartEnd.End,
    distance: 0,
  })

  const site2 = graph.newSpan(pid(), {
    segmentId: ssId,
    end: TrackSpanPartEnd.Start,
    distance: 0,
  }, {
    segmentId: y0seg.id,
    end: TrackSpanPartEnd.Start,
    distance: 0,
  })

  const items = [] as AlinasMapModMixinItem[]

  const indId = Id<Industry>(zone)
  
  const area = graph.areas[Id<Area>('whittier')] ?? graph.importArea(originalTracks.getArea(Id('whittier')))

  const ind = area.industries[indId] ?? area.newIndustry(indId, 'East Whittier Expansion')
  
  const makeProgIndComp = (id: IndustryComponentId, trackSpans: Id<TrackSpan>[]) => {
    ind.newComponent(id, `${ind.name} Site`, {
      type: IndustryComponentType.ProgressionIndustryComponent,
      carTypeFilter: '*',
      sharedStorage: true,
      trackSpans: trackSpans,
    })
    return `${zone}.${id}`
  }

  items.push({
    identifier: `${zone}_Sawmill`,
    name: `Whittier Sawmill Connection`,
    groupIds: [sawmillGroup],
    description: 'Extend the sawmill track over to the interchange',
    trackSpans: [site1.id],
    area: area.id,
    industryComponent: makeProgIndComp('sawmill-site', [site1.id]),
    deliveryPhases: [
      { 
        cost: 2000,
        deliveries: [
          loadHelper('ballast', 4, 'GB*'),
          loadHelper('gravel', 2, 'GB*'),
          loadHelper('ties', 2, 'GB*'),
          loadHelper('rails', 1, 'FM*'),
        ],
      }
    ],
  })

  for(let i = 1; i <= tracks / 3; i++) {
    const id = `${zone}_${i}`
    const item = {
      identifier: id,
      name: `Whittier Yard ${i>1?`Extension ${i}`:''}`,
      groupIds: [
        ...(i==1?[leadGroup]:[]),
        trackGroupFn(tracks - (i * 3) + 0),
        trackGroupFn(tracks - (i * 3) + 1),
        trackGroupFn(tracks - (i * 3) + 2),
      ],
      trackSpans: [site2.id],
      industryComponent: makeProgIndComp(`yard-site-${i}`, [site2.id]),
      area: area.id,
      description: i == 1 ? 'A yard that can be useful for organizing trains and storing cars.' : 'An additional 3 tracks for the Whittier yard',
      prerequisiteSections: [`${zone}_Sawmill`],
      deliveryPhases: [
        {
          cost: 2000,
          deliveries: [
            loadHelper('ballast', 8, 'GB*'),
            loadHelper('gravel', 12, 'GB*'),
          ]
        },
        {
          cost: 2000,
          deliveries: [
            loadHelper('gravel', 6, 'GB*'),
            loadHelper('ties', 8, 'GB*'),
            loadHelper('rails', 6, 'FM*'),
          ]
        }
      ],
    } as AlinasMapModMixinItem
    items.push(item)
    if(i > 1) {
      for(let j = 1; j < i; j++) {
        item.prerequisiteSections?.push(`${zone}_${j}`)
      }
    }
  }
  
  for (const item of items) {
    // makeProgIndComp(item.identifier, [site1.id])
    // item.industryComponent = item.identifier
  }

  const mixin: AlinasMapModMixin = {
    items: Object.fromEntries(items.map(i => [i.identifier, i]))
  }

  return {
    name: 'Whittier Yard',
    mixins: {
      alinasMapMod: mixin
    }
  }
}