
import { MathUtils, Matrix4, Vector3 } from 'three'

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Segment, TrackNode, TrackSpanPartEnd, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

/** @param {Graph} graph */
export default async function andrewsInterchangeYard(graph: Graph, originalTracks: Graph) {
  const area = `AN_Andrews_Interchange_Yard`

  const additionalTracks = 7

  const switchAngle = 8
  const trackLength = 400
  const node = graph.newNode(Id(`N${area}_00`), new Vector3())
  let lastNode = node
  for (let i = 0; i < additionalTracks; i++) {
    
    lastNode = lastNode
      .extend(Id(`N${area}_T${i}_00`), Id(`S${area}_L${i}_00`), 30)
    
    lastNode.rotation.y = -switchAngle

    const n1 = lastNode
      .extend(Id(`N${area}_T${i}_01`), Id(`S${area}_T${i}_00`), 20, switchAngle)
    
    n1.position.add(new Vector3(-1, 0, 0))
    const end = n1.extend(Id(`N${area}_T${i}_02`), Id(`S${area}_T${i}_01`), trackLength - n1.position.z, 0)
    end.position.y -= 3
  }

  const n1 = graph.nodes.get(Id(`N${area}_T0_00`)) as TrackNode
  const n2 = graph.nodes.get(Id(`N${area}_T0_01`)) as TrackNode

  n1.position.x = n2.position.x
  n1.rotation.y = 0

  // Integrate yard
  const ref = originalTracks.nodes.get(Id('Nrgu')) as TrackNode
  const in1 = originalTracks.nodes.get(Id('Nrgu')) as TrackNode
  const is1 = originalTracks.segments.get(Id('Sinb')) as Segment
  graph.importNode(in1)
  graph.importSegment(is1)

  graph.nodes.delete(node.id) // Remove starter
  const seg = graph.segments.get(Id(`S${area}_L0_00`)) as Segment
  seg.startId = in1.id

  const ext = in1.extend(Id(`N${area}_MainlineExtension_00`), Id(`S${area}_MainlineExtension_00`), 30, 0, 0)
  is1.startId = ext.id

  const matrix = new Matrix4()
  const angleOffset = 8
  const positionOffset = new Vector3(2.5, 0, 0)
  matrix.makeRotationAxis(UP, MathUtils.degToRad(ref.rotation.y - angleOffset))

  graph.nodes.forEach((node, id) => {
    if (id.startsWith(`N${area}`) && !id.includes('Mainline')) {
      node.position
        .add(positionOffset)
        .applyMatrix4(matrix)
        .add(in1.position)
      node.rotation.y += ref.rotation.y - angleOffset
    }
  })

  graph.segments.forEach((segment, id) => {
    if (id.startsWith(`S${area}`) && !id.includes('Mainline')) {
      segment.groupId = area
    }
  })

  const span = graph.newSpan(Id(`P${area}_00`), {
    segmentId: Id('S8gq9'),
    end: TrackSpanPartEnd.Start,
    distance: 0
  }, {
    segmentId: Id('S8gq9'),
    end: TrackSpanPartEnd.End,
    distance: 0
  })

  const mixin: AlinasMapModMixin = {
    items: {
      [area]: {
        identifier: area,
        name: 'Andrews Interchange Yard',
        groupIds: [area],
        description: 'A yard that can be useful for organizing east bound trains and storing cars if the Interchange is filled to capacity.',
        trackSpans: [span.id],
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
    name: mixin.items[area].name,
    mixins: {
      alinasMapMod: mixin
    }
  }
}