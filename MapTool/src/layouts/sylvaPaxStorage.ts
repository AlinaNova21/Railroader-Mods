
import { Vector3 } from 'three'

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, TrackSpan, TrackSpanPartEnd, getNode, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

export default async function sylvaPaxStorage(graph: Graph, originalTracks: Graph) {
    const zone = `AN_Sylva_Pax_Storage`
    const { nid, sid, pid } = Graph.Shared.pushIdGenerator('Sylva_Pax_Storage')
    
    const start = getNode(Id('No9a'))
    const s = getNode(Id('Ngcv'))
    s.flipSwitchStand = true
    start.flipSwitchStand = true
    
    const switchpoint = start.extend(nid(),sid(),40,0,0)
    switchpoint.flipSwitchStand = true
    switchpoint.extend(nid(),sid(),45,0,0).extend(nid(),sid(),95,0,0).extend(nid(),sid(),100,-4,-2)
    switchpoint.extend(nid(),sid(),45,6,-6).extend(nid(),sid(),95,0,0).extend(nid(),sid(),100,-4,-2)
    
    Object.entries(graph.segments).forEach(([id, segment]) => {
      if (id.startsWith(`S${zone}`)) {
        segment.groupId = zone
      }
    })
    
    const spans = [] as Id<TrackSpan>[]
    
      const span = graph.newSpan(pid(), {
        segmentId: Id(`S89g`),
        end: TrackSpanPartEnd.Start,
        distance: 0,
      }, {
        segmentId: Id(`S89g`),
        end: TrackSpanPartEnd.End,
        distance: 0,
      })
      spans.push(span.id)
      
      //not sure what is hapening after this point
    
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
          name: 'Sylva Pax Storage',
          groupIds: [zone],
          description: 'Adds two storage tracks to Sylva Station.',
          area: area.id,
          trackSpans: spans,
          deliveryPhases: [{
            cost: 2000,
            deliveries: [
              loadHelper('ballast', 2, 'GB*'),
              loadHelper('ties', 2, 'GB*'),
              loadHelper('rails', 2, 'FM*'),
            ]
          }],
          prerequisiteSections: ['s1'],
          industryComponent: makeProgIndComp('sylva-station-site', spans),
        }
      }
    } 
  
    return {
      name: mixin.items[zone].name,
      desc: 'Adds two storage tracks to Sylva Station.',
      mixins: {
        alinasMapMod: mixin
      }
    }
  }