
import { Euler, Vector3 } from 'three'

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, TrackSpan, TrackSpanPartEnd, getNode, getSegment, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

export default async function alarkaPaxStorage(graph: Graph, originalTracks: Graph) {
    const zone = `AN_Alarka_Pax_Storage`
    const { nid, sid, pid } = Graph.Shared.pushIdGenerator('Alarka_Pax_Storage')
    
    const start1 = getNode(Id('Ne2z'))
    const start2 = getNode(Id('Nmye'))
    const seg = getSegment(Id('Sv2b'))
    
    const pos1 = new Vector3(4306.98,646.65,-3235.17)
    const rot1 = new Euler(359.43,131.81,0.0)
    const turnout = graph.createNode(pos1,rot1)
    turnout.flipSwitchStand=true
    seg.endId = turnout.id
    const pos2 = new Vector3(4326.56,646.94,-3256.56)
    const rot2 = new Euler(359.43,140.81,0.0)
    const n1 = graph.createNode(pos2,rot2)
    
    graph.createSegment(turnout.id,n1.id)
    const gap = graph.createSegment(turnout.id,start1.id)

    
    const switchpoint = n1.extend(nid(),sid(),30,-3,-5)
    switchpoint.flipSwitchStand=true
    const siding1 = switchpoint.extend(nid(),sid(),45,0,0)
    const siding2 = switchpoint.extend(nid(),sid(),45,6,-6)



    siding1.extend(nid(),sid(),75,0,0).extend(nid(),sid(),120,14,10)
    siding2.extend(nid(),sid(),75,0,0).extend(nid(),sid(),120,14,10)
    

    Object.entries(graph.segments).forEach(([id, segment]) => {
      if (id.startsWith(`S${zone}`)) {
        segment.groupId = zone
      }
    })
    gap.groupId = ""
    
    const spans = [] as Id<TrackSpan>[]
    
    const span1 = graph.createSpan({
      segmentId: Id("S6np"),
      end: TrackSpanPartEnd.Start,
      distance: 0
    },{
      segmentId: Id("Sok6"),
      end: TrackSpanPartEnd.End,
      distance: 1
    })
      spans.push(span1.id)
      
      //not sure what is hapening after this point
    
    const area = graph.getArea(Id('alarka'))
    const indId = Id<Industry>(zone)
    const ind = area.industries[indId] ?? area.newIndustry(indId, 'Alarka Expansion')
    
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
          name: 'Alarka Pax Storage',
          groupIds: [zone],
          description: 'Adds two storage tracks to Alarka Station.',
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
          prerequisiteSections: ['alarka-branch'],
          industryComponent: makeProgIndComp('alarka-station-site', spans),
        }
      }
    } 
  
    return {
      name: mixin.items[zone].name,
      desc: 'Adds two storage tracks to Alarka Station.',
      mixins: {
        alinasMapMod: mixin
      }
    }
  }
