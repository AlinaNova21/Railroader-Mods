
import { Euler, MathUtils, Vector3 } from 'three'

import { AlinasMapModMixin, AlinasMapModMixinItem } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, LayoutFunctionResult, Segment, TrackNode, TrackSpan, TrackSpanPartEnd, createNode, getNode, getSegment, loadHelper } from '../lib/index.js'

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


  const { nid, sid, pid } = Graph.Shared.pushIdGenerator('Whittier_Yard')

  const interchange1 = getNode(Id('N5es'))
  const sawmill2 = getNode(Id('Nijv'))

  const start = createNode(new Vector3(13220, 561.25, 4395))
  
  const inSeg = getSegment(Id("Sj4c"));

  const inext1 = interchange1.extend(nid(), sid(), -22, 0, 0, extGroup)
  const inext3 = interchange1.extend(nid(), sid(), 20, 180+8, 5, leadGroup)

  inext1.flipSwitchStand = true

  inSeg.startId = inext1.id

  const inseg2 = start.toNode(sid(), inext3)
  inseg2.priority = -1
  inseg2.groupId = leadGroup

  start.rotation.y += 300 + -3

  interchange1.flipSwitchStand = true

  const entryNodes: TrackNode[] = []
  const exitNodes: TrackNode[] = []

  const switchAngle = 12
  const leadLength = 40
  const yardLength = 260

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

  start.position.add(new Vector3(20, 0, -22))
  start.rotation.y += 8

  const tn0 =  graph.getNode(Id(`N${zone}_T0_00`))
  tn0.position.add(new Vector3(0, 0, 0))
  tn0.offset(10)
  tn0.rotation.y += switchAngle - 0
  tn0.offset(-20)

  start.position.y = interchange1.position.y
  start.extend(nid(), sid(), -30, -1, -2, leadGroup)
       .extend(nid(), sid(), -140, -11, -11, leadGroup)


  const ss1 = graph.getSegment(Id('Sj6n'))
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

  const items = [] as AlinasMapModMixinItem[]

  const indId = Id<Industry>(zone)
  
  const area = graph.getArea(Id('whittier'))

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
      trackSpans: [site1.id],
      industryComponent: makeProgIndComp(`yard-site-${i}`, [site1.id]),
      area: area.id,
      description: i == 1 ? 'A yard that can be useful for organizing trains and storing cars.' : 'An additional 3 tracks for the Whittier yard',
      prerequisiteSections: [],
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

  const mixin: AlinasMapModMixin = {
    items: Object.fromEntries(items.map(i => [i.identifier, i]))
  }

  graph.createSpliney(Id(`${zone}_Poles`), "AlinasMapMod.TelegraphPoleBuilder", {
    polesToRaise: [591, 593, 595, 603, 605]
  })

  return {
    name: 'Whittier Yard',
    desc: 'A yard that can be useful for organizing trains and storing cars.',
    mixins: {
      alinasMapMod: mixin
    },
    conflicts: [
      { id: 'AlinaNova21.WhittierYard' }
    ],
    version: '1.1.2',
    changelog: [
      { version: '1.1.2', desc: 'Updated poles to remove extras' }
    ]
  } as LayoutFunctionResult
}