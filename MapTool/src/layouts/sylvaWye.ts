
import { Vector3 } from 'three'

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { DeliveryDirection, Graph, Id, Industry, IndustryComponentId, IndustryComponentType, LayoutFunctionResult, TrackSpan, TrackSpanPartEnd, getNode, idGenerator, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

export default async function sylvaWye(graph: Graph, originalTracks: Graph) {
  const zone = `AN_Sylva_Wye`
  const { nid, sid, pid } = idGenerator(zone)

  const start1 = getNode(Id('N86n'))

  const lead = start1
    .extend(nid(), sid(), 100, -14, -3)
    .extend(nid(), sid(), -40, 170, -10)

  const branch1 = lead.extend(nid(), sid(), 300, -130, 20)
  const branch2 = lead.extend(nid(), sid(), 300, 130, -20)

  branch1.extend(nid(), sid(), 100).position.y += 4
  branch2.extend(nid(), sid(), 100).position.y += 4

  graph.newSegment(sid(), branch1, branch2)

  Object.entries(graph.segments).forEach(([id, segment]) => {
    if (id.startsWith(`S${zone}`)) {
      segment.groupId = zone
    }
  })

  const spans = [] as Id<TrackSpan>[]
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
        name: 'Sylva Wye',
        groupIds: [zone],
        description: 'Adds a Wye at the Sylva Interchange, great for turning around those massive Berks.',
        area: area.id,
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
        industryComponent: makeProgIndComp('wye-site', spans),
      }
    }
  }

  return {
    name: mixin.items[zone].name,
    desc: 'Adds a Wye at the Sylva Interchange, great for turning around those massive Berks.',
    version: '1.2.0',
    mixins: {
      alinasMapMod: mixin
    },
    conflicts: [
      { id: 'AlinaNova21.SylvaWye' },
    ],
    requires: [
      { id: 'AlinaNova21.AMM_SylvaInterchangeYard' },
    ],
    changelog: [
      {
        version: '1.2.0',
        desc: '- Added dependency for the Sylva Interchange Yard.',
      }
    ]
  } as LayoutFunctionResult
}
