
import { Vector3 } from 'three'

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, TrackSpan, TrackSpanPartEnd, getNode, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

export default async function sylvaPaperCrossover(graph: Graph, originalTracks: Graph) {
  const zone = `AN_Sylva_Paper_Crossover`
  const { nid, sid, pid } = Graph.Shared.pushIdGenerator('Sylva_Paper_Crossover')
  
  const a1 = getNode(Id('Nje2'))
  const a2 = getNode(Id('N37u'))
  const a3 = getNode(Id('Nntj'))
  const b1 = getNode(Id('N7w9'))
  const b2 = getNode(Id('N8f9'))
  const b3 = getNode(Id('Nntj'))
  
  
  
  const posA = a3.position.clone()
  posA.sub(a2.position)
  posA.multiplyScalar(0.32)
  posA.add(a2.position)
  const a4 = graph.createNode(posA,a2.rotation)//create Node a4 on a line between a2 and a3

  const posB = b3.position.clone()
  posB.sub(b2.position)
  posB.multiplyScalar(0.28)
  posB.add(b2.position)
  const b4 = graph.createNode(posB,b2.rotation)//create Node a4 on a line between a2 and a3
  

  graph.getSegment(Id('Szya')).endId = a4.id//trim segment a2<>a3 to a4<>a3
  graph.getSegment(Id('Skmy')).endId = b4.id
  
  graph.createSegment(a4.id,a2.id)//fill gap a2<>a4
  graph.createSegment(b4.id,b3.id)
  
  const as = graph.createSegment(a1.id,a4.id)//create the actual crossover
  const bs = graph.createSegment(b1.id,b4.id)

  as.groupId = zone
  bs.groupId = zone
  
  a1.flipSwitchStand = false
  a4.flipSwitchStand = true
  b1.flipSwitchStand = true
  b4.flipSwitchStand = false
  
  
  const spans = [] as Id<TrackSpan>[]
  
    const span = graph.newSpan(pid(), {
      segmentId: Id(`Spiq`),
      end: TrackSpanPartEnd.Start,
      distance: 0,
    }, {
      segmentId: Id(`Spiq`),
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
        name: 'Sylva Paper Crossovers',
        groupIds: [zone],
        description: 'Adds two Crossovers at Sylva Paperboard.',
        area: area.id,
        trackSpans: spans,
        deliveryPhases: [{
          cost: 1000,
          deliveries: [
            loadHelper('ballast', 1, 'GB*'),
            loadHelper('ties', 1, 'GB*'),
            loadHelper('rails', 1, 'FM*'),
          ]
        }],
        prerequisiteSections: ['s1'],
        industryComponent: makeProgIndComp('sylva-paper-crossover-site', spans),
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
