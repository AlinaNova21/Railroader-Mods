

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, TrackSpan, getArea, getNode } from '../lib/index.js'

export default async function walkerUraniumMine(graph: Graph, originalTracks: Graph) {
  const zone = `AN_Walker_Uranium_Mine`
  const { nid, sid } = Graph.Shared.pushIdGenerator('Walker_Uranium_Mine')

  const start = getNode(Id('Ncyr'))
  const segs = Object.values(Graph.Shared.segments)
    .filter(s => s.startId == start.id || s.endId == start.id)
  const seg = segs[0]
  const n = getNode(Id(seg.startId == start.id ? seg.endId : seg.startId))
  n.flipSwitchStand = true
  console.log(n.id)
  start.flipSwitchStand = true

  const entryGroup = `${zone}_Entry`

  start.offset(-20);
  // start.position.y += -0.5
  start.rotation.x += -0.5

  const entry1 = start.switch(nid(), sid(), 30, 180-6, 180-3, entryGroup, true, originalTracks)
  
  let tn = entry1.extend(nid(), sid(), 150, 0, 0, entryGroup)
  tn.position.y += 2
  tn.rotation.y += 1
  tn = tn.extend(nid(), sid(), 150, 12, 12, entryGroup)
  tn.position.y += 2
  tn = tn.extend(nid(), sid(), 150, 5, 5, entryGroup)
  tn.position.y += 2
  

  Object.entries(Graph.Shared.segments).forEach(([id, segment]) => {
    if (id.startsWith(`S${zone}`)) {
      segment.groupId = ''
    }
  })


  const area = getArea(Id('bryson-above'))
  const indId = Id<Industry>(zone)
  const ind = area.industries[indId] ?? area.newIndustry(indId, 'Walker Expansion')

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
        name: 'Walker Uranium Mine',
        groupIds: [zone],
        description: 'Adds a uranium mine at Walker',
        area: area.id,
        trackSpans: [],
        deliveryPhases: [{
          cost: 0,
          // deliveries: [
          //   loadHelper('mow-machinery', 1, 'FM'),
          //   loadHelper('debris', 100, 'GB'),
          // ]
        }],
        prerequisiteSections: ['s2'],
        industryComponent: makeProgIndComp('walker-uranium-mine-site', []),
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
