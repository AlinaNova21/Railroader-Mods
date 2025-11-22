
import { Vector3 } from 'three'

import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, LayoutFunctionResult, TrackSpan, TrackSpanPartEnd, getNode, loadHelper } from '../lib/index.js'

const UP = new Vector3(0, 1, 0)

export default async function sylvaPaperCrossover(graph: Graph, originalTracks: Graph) {
  const zone = `AN_Sylva_Paper_Crossover`
  const { nid, sid, pid } = Graph.Shared.pushIdGenerator('Sylva_Paper_Crossover')
  
  const b1 = getNode(Id('N7w9'))
  const b2 = getNode(Id('N8f9'))
  const b3 = getNode(Id('Nntj'))
  
  const posB = b3.position.clone()
  posB.sub(b2.position)
  posB.multiplyScalar(0.28)
  posB.add(b2.position)
  const b4 = graph.createNode(posB,b2.rotation)//create Node a4 on a line between a2 and a3
  

  graph.getSegment(Id('Skmy')).endId = b4.id
  
  graph.createSegment(b4.id,b3.id)
  
  const bs = graph.createSegment(b1.id,b4.id)

  bs.groupId = zone
  
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
        name: 'Sylva Paper Crossover',
        groupIds: [zone],
        description: 'Adds a Crossover at Sylva Paperboard.',
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
    desc: 'Adds a Crossover at Sylva Paperboard.',
    mixins: {
      alinasMapMod: mixin
    },
    version: '1.1.2',
    changelog: [
      { version: '1.1.2', desc: 'Removed crossover that conflicts with base game.' }
    ]
  } as LayoutFunctionResult
}
