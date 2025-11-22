
import { MathUtils, Matrix4, Vector3 } from 'three'

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, LayoutFunctionResult, Segment, TrackNode, TrackSpan, TrackSpanPartEnd, idGenerator, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

/** @param {Graph} graph */
export default async function sylvaInterchangeYard(graph: Graph, originalTracks: Graph) {
  const zone = `AN_Sylva_Interchange_Yard`

  const additionalTracks = 7

  const switchAngle = 8
  const trackLength = 500
  const node = graph.newNode(Id(`N${zone}_00`), new Vector3())
  let lastNode = node
  for (let i = 0; i < additionalTracks; i++) {
    const { nid, sid } = idGenerator(`${zone}_T${i}`)
    lastNode = lastNode
      .extend(nid(), sid(), 30)

    lastNode.rotation.y = -switchAngle

    const n1 = lastNode
      .extend(nid(), sid(), 20, switchAngle)

    n1.position.add(new Vector3(-1, 0, 0))
    n1.extend(nid(), sid(), trackLength - n1.position.z, 0)
  }

  const n1 = graph.getNode(Id(`N${zone}_T0_00`)) as TrackNode
  const n2 = graph.getNode(Id(`N${zone}_T0_01`)) as TrackNode

  n1.position.x = n2.position.x
  n1.rotation.y = 0

  // Integrate yard
  const ref = originalTracks.getNode(Id('Nlfg')) as TrackNode
  const in1 = originalTracks.getNode(Id('Nf8a')) as TrackNode
  const in2 = originalTracks.getNode(Id('Nmjn')) as TrackNode
  graph.nodes[in1.id] = in1
  graph.nodes[in2.id] = in2

  delete graph.nodes[node.id] // Remove starter
  const seg = graph.getSegment(Id(`S${zone}_T0_00`)) as Segment
  seg.startId = in1.id

  const matrix = new Matrix4()
  const positionOffset = new Vector3(-7, 0, 75)
  matrix.makeRotationAxis(UP, MathUtils.degToRad(ref.rotation.y))

  Object.entries(graph.nodes).forEach(([id, node]) => {
    if (id.startsWith(`N${zone}`)) {
      node.position
        .add(positionOffset)
        .applyMatrix4(matrix)
        .add(ref.position)
      node.rotation.y += ref.rotation.y
    }
  })

  Object.entries(graph.segments).forEach(([id, segment]) => {
    if (id.startsWith(`S${zone}`)) {
      segment.groupId = zone
    }
  })

  const span1 = graph.newSpan(Id(`P${zone}_00`), {
    segmentId: Id("S3ns"),
    end: TrackSpanPartEnd.Start,
    distance: 0
  }, {
    segmentId: Id("S3ns"),
    end: TrackSpanPartEnd.End,
    distance: 0
  })


  const area = graph.getArea(Id('sylva'))
  const indId = Id<Industry>(zone)
  const ind = area.industries[indId] ?? area.newIndustry(indId, 'Sylva Expansion')

  const makeProgIndComp = (id: IndustryComponentId, trackSpans: Id<TrackSpan>[]) => {
    ind.newComponent(id, `${ind.name} Site`, {
      type: IndustryComponentType.ProgressionIndustryComponent,
      carTypeFilter: '*',
      sharedStorage: true,
      trackSpans: trackSpans,
    })
    return `${zone}.${id}`
  }

  const mixin: AlinasMapModMixin = {
    items: {
      [zone]: {
        identifier: zone,
        name: 'Sylva Interchange Yard',
        groupIds: [zone],
        description: 'A yard that can be useful for organizing west bound trains and storing cars if the Interchange is filled to capacity.',
        deliveryPhases: [{
            cost: 2000,
            deliveries: [
              loadHelper('ballast', 4, 'GB*'),
              loadHelper('gravel', 12, 'GB*'),
            ]
          },
          {
            cost: 2000,
            deliveries: [
              loadHelper('gravel', 2, 'GB*'),
              loadHelper('ties', 8, 'GB*'),
              loadHelper('rails', 6, 'FM*'),
            ]
          }],
        prerequisiteSections: ['s1'],
        area: area.id,
        trackSpans: [span1.id],
        industryComponent: makeProgIndComp('interchange-yard-site', [span1.id]),
      }
    }
  }

  return {
    name: mixin.items[zone].name,
    desc: 'A yard that can be useful for organizing west bound trains and storing cars if the Interchange is filled to capacity.',
    version: '1.2.0',
    mixins: {
      alinasMapMod: mixin
    },
    conflicts: [
      { id: 'AlinaNova21.SylvaInterchangeYard' },
      { id: 'smecko.SylvaInterchange' }
    ],
    changelog: [
      { version: '1.2.0', desc: '- Add conflict for smeckos sylva interchange' }
    ]
  } as LayoutFunctionResult
  // graph.newSegment(`N${area}_L0`, in1, inNodes[0])
  // graph.newSegment(`N${area}_L1`, in2, outNodes[0])
}