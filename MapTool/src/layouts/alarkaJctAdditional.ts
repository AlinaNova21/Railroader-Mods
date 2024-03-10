

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, LayoutFunctionResult, TrackNode, idGenerator, loadHelper } from '../lib/index.js'

export default async function alarkaJctAdditional(graph: Graph, originalTracks: Graph): Promise<LayoutFunctionResult> {
  const area = `AN_Alarka_Jct_Additional`
 
  const { nid, sid } = idGenerator(area)

  const node1 = graph.importNode(originalTracks.nodes.get(Id('N3cj')) as TrackNode)
  const node2 = graph.importNode(originalTracks.nodes.get(Id('N3wo')) as TrackNode)

  // Shift node back to move switch back from sign
  node2.position.x += 5
  node2.position.z += -15

  const n1 = node1
    .extend(nid(), sid(), 60, -18, -6)
    .extend(nid(), sid(), 110, 0, 5)
  
  n1.position.x += -3
  n1.position.y -= 2
    
  const n2 = node2
    .extend(nid(), sid(), 50, 185, 3)
    .extend(nid(), sid(), 80, -1, -2)

  const n3 = n2
    .extend(nid(), sid(), 40, 0, 0)
  
  n3.position.y += 1
  
  n3.toNode(sid(), n1)

  graph.segments.forEach((segment, id) => {
    if (id.startsWith(`S${area}`)) {
      segment.groupId = area
    }
  })


  const mixinName = 'Alarka Jct Additional Tracks'
  const mixin: AlinasMapModMixin = {
    items: {
      [area]: { 
        identifier: area,
        name: mixinName,
        groupIds: [area],
        description: 'Additional tracks in Alarka Jct, currently just a bypass around the interchange.',
        prerequisiteSections: ['alarka-jct'],
        trackSpans: [Id('Pevc')],
        area: 'alarka-jct',
        deliveryPhases: [
          {
            // TODO: Debug why this doesn't work
            // industryComponent: 'alarka-track.site2',
            cost: 2000,
            deliveries: [
              loadHelper('ballast', 4, 'GB*'),
              loadHelper('gravel', 2, 'GB*'),
              loadHelper('ties', 2, 'GB*'),
              loadHelper('rails', 1, 'FM*'),
            ],
          }
        ],
      }
    }
  }

  return {
    name: mixinName,
    mixins: {
      alinasMapMod: mixin
    }
  }
}