
import { MathUtils, Matrix4, Vector3 } from 'three'

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, TrackSpan, TrackSpanPartEnd, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

/** @param {Graph} graph */
export default async function andrewsInterchangeYard(graph: Graph, originalTracks: Graph) {
  const zone = `AN_Andrews_Interchange_Yard`

  const additionalTracks = 7

  const switchAngle = 8
  const trackLength = 400
  const node = graph.newNode(Id(`N${zone}_00`), new Vector3())
  let lastNode = node
  for (let i = 0; i < additionalTracks; i++) {
    
    lastNode = lastNode
      .extend(Id(`N${zone}_T${i}_00`), Id(`S${zone}_L${i}_00`), 30)
    
    lastNode.rotation.y = -switchAngle

    const n1 = lastNode
      .extend(Id(`N${zone}_T${i}_01`), Id(`S${zone}_T${i}_00`), 20, switchAngle)
    
    n1.position.add(new Vector3(-1, 0, 0))
    const end = n1.extend(Id(`N${zone}_T${i}_02`), Id(`S${zone}_T${i}_01`), trackLength - n1.position.z, 0)
    end.position.y -= 3
  }

  const n1 = graph.getNode(Id(`N${zone}_T0_00`))
  const n2 = graph.getNode(Id(`N${zone}_T0_01`))

  n1.position.x = n2.position.x
  n1.rotation.y = 0

  // Integrate yard
  const ref = originalTracks.getNode(Id('Nrgu'))
  const in1 = originalTracks.getNode(Id('Nrgu'))
  const is1 = originalTracks.getSegment(Id('Sinb'))
  graph.importNode(in1)
  graph.importSegment(is1)

  delete graph.nodes[node.id] // Remove starter
  const seg = graph.getSegment(Id(`S${zone}_L0_00`))
  seg.startId = in1.id

  const ext = in1.extend(Id(`N${zone}_MainlineExtension_00`), Id(`S${zone}_MainlineExtension_00`), 30, 0, 0)
  is1.startId = ext.id

  const matrix = new Matrix4()
  const angleOffset = 8
  const positionOffset = new Vector3(2.5, 0, 0)
  matrix.makeRotationAxis(UP, MathUtils.degToRad(ref.rotation.y - angleOffset))

  Object.entries(graph.nodes).forEach(([id, node]) => {
    if (id.startsWith(`N${zone}`) && !id.includes('Mainline')) {
      node.position
        .add(positionOffset)
        .applyMatrix4(matrix)
        .add(in1.position)
      node.rotation.y += ref.rotation.y - angleOffset
    }
  })

  Object.entries(graph.segments).forEach(([id, segment]) => {
    if (id.startsWith(`S${zone}`) && !id.includes('Mainline')) {
      segment.groupId = zone
    }
  })

  const span = graph.newSpan(Id(`P${zone}_00`), {
    segmentId: Id('S8gq9'),
    end: TrackSpanPartEnd.Start,
    distance: 0
  }, {
    segmentId: Id('S8gq9'),
    end: TrackSpanPartEnd.End,
    distance: 0
  })

  const area = graph.getArea(Id('andrews'))
  const indId = Id<Industry>(zone)
  const ind = area.industries[indId] ?? area.newIndustry(indId, 'Andrews Expansion')

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
        name: 'Andrews Interchange Yard',
        groupIds: [zone],
        description: 'A yard that can be useful for organizing east bound trains and storing cars if the Interchange is filled to capacity.',
        trackSpans: [span.id],
        industryComponent: makeProgIndComp('interchange-yard-site', [span.id]),
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
        prerequisiteSections: ['s6'],
        area: 'andrews',
      }
    }
  }

  return {
    name: mixin.items[zone].name,
    mixins: {
      alinasMapMod: mixin
    }
  }
}