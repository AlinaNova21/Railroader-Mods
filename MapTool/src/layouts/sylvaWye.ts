
import { Vector3 } from 'three'

import { AlinasMapModMixin, DeliveryDirection } from '../lib/AlinasMapMod.js'
import { Graph, Id, TrackNode, TrackSpanPartEnd, idGenerator, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

export default async function sylvaWye(graph: Graph, originalTracks: Graph) {
  const area = `AN_Sylva_Wye`
  const { nid, sid, pid } = idGenerator(area)

  const start1 = graph.importNode(originalTracks.nodes.get(Id('N86n')) as TrackNode)

  const lead = start1
    .extend(nid(), sid(), 100, -14, -3)
    .extend(nid(), sid(), -40, 170, -10)

  const branch1 = lead.extend(nid(), sid(), 300, -130, 20)
  const branch2 = lead.extend(nid(), sid(), 300, 130, -20)

  branch1.extend(nid(), sid(), 100).position.y += 4
  branch2.extend(nid(), sid(), 100).position.y += 4

  graph.newSegment(sid(), branch1, branch2)

  graph.segments.forEach((segment, id) => {
    if (id.startsWith(`S${area}`)) {
      segment.groupId = area
    }
  })

  const spans = []
  for(let i = 0; i < 1; i++) {
    const span = graph.newSpan(pid(), {
      segmentId: Id(`SAN_Sylva_Interchange_Yard_T${i}_02`),
      end: TrackSpanPartEnd.Start,
      distance: 0,
    }, {
      segmentId: Id(`SAN_Sylva_Interchange_Yard_T${i}_02`),
      end: TrackSpanPartEnd.End,
      distance: 0,
    })
    spans.push(span.id)
  }
  // const span1 = graph.newSpan(pid(), {
  //   segmentId: Id("Sc01"),
  //   end: TrackSpanPartEnd.Start,
  //   distance: 0
  // }, {
  //   segmentId: Id("Sc01"),
  //   end: TrackSpanPartEnd.End,
  //   distance: 0
  // })

  const mixin: AlinasMapModMixin = {
    items: {
      [area]: {
        identifier: area,
        name: 'Sylva Wye',
        groupIds: [area],
        description: 'Adds a Wye at the Sylva Interchange, great for turning around those massive Berks.',
        area: 'sylva',
        trackSpans: spans,
        deliveryPhases: [{
          cost: 2000,
          deliveries: [
            loadHelper('mow-machinery', 1, 'FM'),
            loadHelper('debris', 100, 'GB'),
          ]
        }, {
          cost: 2000,
          deliveries: [
            loadHelper('mow-machinery', 1, 'FM', DeliveryDirection.LoadFromIndustry),
            loadHelper('ballast', 6, 'GB*'),
            loadHelper('gravel', 5, 'GB*'),
            loadHelper('ties', 2, 'GB*'),
            loadHelper('rails', 2, 'FM*'),
          ]
        }],
        prerequisiteSections: ['s1', 'AN_Sylva_Interchange_Yard'],
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
