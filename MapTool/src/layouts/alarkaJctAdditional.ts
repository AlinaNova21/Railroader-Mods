

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, LayoutFunctionResult, TrackSpan, getNode, idGenerator, loadHelper } from '../lib/index.js'

export default async function alarkaJctAdditional(graph: Graph, originalTracks: Graph): Promise<LayoutFunctionResult> {
  const zone = `AN_Alarka_Jct_Additional`
 
  const { nid, sid } = idGenerator(zone)

  const node1 = getNode(Id('N3cj'))
  const node2 = getNode(Id('N3wo'))

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

  Object.entries(graph.segments).forEach(([id, segment]) => {
    if (id.startsWith(`S${zone}`)) {
      segment.groupId = zone
    }
  })

  const area = graph.getArea(Id('alarka-jct'))
  const indId = Id<Industry>(zone)
  const ind = area.industries[indId] ?? area.newIndustry(indId, 'Alarka Jct Expansion')

  const makeProgIndComp = (id: IndustryComponentId, trackSpans: Id<TrackSpan>[]) => {
    ind.newComponent(id, `${ind.name} Site`, {
      type: IndustryComponentType.ProgressionIndustryComponent,
      carTypeFilter: '*',
      sharedStorage: true,
      trackSpans: trackSpans,
    })
    return `${zone}.${id}`
  } 

  const mixinName = 'Alarka Jct Additional Tracks'
  const mixin: AlinasMapModMixin = {
    items: {
      [zone]: { 
        identifier: zone,
        name: mixinName,
        groupIds: [zone],
        description: 'Additional tracks in Alarka Jct, currently just a bypass around the interchange.',
        prerequisiteSections: ['alarka-jct'],
        trackSpans: [Id('Pevc')],
        industryComponent: makeProgIndComp('alarka-bypass-site', [Id('Pevc')]),
        area: area.id,
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
    desc: 'Additional tracks in Alarka Jct, currently just a bypass around the interchange.',
    mixins: {
      alinasMapMod: mixin
    },
    conflicts: [
      { id: 'AlinaNova21.AlarkaJctAdditional' }
    ]
  } as LayoutFunctionResult
}