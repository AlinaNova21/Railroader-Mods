

import { Euler, Vector3 } from 'three'
import { DEG2RAD } from 'three/src/math/MathUtils.js'
import { AlinasMapModMixin } from '../lib/AlinasMapMod.js'
import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, TrackSpan, TrackSpanPartEnd, createNode, createSegment, createSwitch, getNode, getSegment, loadHelper } from '../lib/index.js'


export default async function alarkaLoop(graph: Graph) {
  const zone = `AN_Alarka_Loop`
  const { nid, sid, pid } = Graph.Shared.pushIdGenerator('Alarka_Loop')

  const start = getNode(Id('N9mx'))
  // start.rotation.y = 159
  // start.flipSwitchStand = false

  const groupId = zone

  const begin1 = getNode(Id('Nt99'))
  begin1.rotation.x = 0

  const segLength = 60
  const segAng = -20

  const n1 = getNode(Id('Ne2z'))
  const n2 = getNode(Id('Nhph'))
  const mp1 = n2.position.clone()
    .sub(n1.position)
    .multiplyScalar(0.9)
    .add(n1.position)
  const mp2 = n2.position.clone()
    .sub(n1.position)
    .multiplyScalar(0.5)
    .add(n1.position)
  const mls1 = getSegment(Id('Sk7x'))
  const n3 = createNode(mp1, n1.rotation.clone())
  const n4 = createNode(mp2, n1.rotation.clone())
  // n3.offset(10)
  mls1.endId = n4.id
  const mls2 = createSegment(n2.id, n3.id)
  const mls3 = createSegment(n3.id, n4.id)

  const n5 = createNode()
  const dir = new Vector3(0, 0, 1)
  dir.applyAxisAngle(new Vector3(0, 1, 0), (n4.rotation.y + 50) * DEG2RAD)
  n5.position = dir.multiplyScalar(4).add(n4.position)
  n5.rotation = n4.rotation.clone()
  n5.rotation.y += 10

  const mls4 = createSegment(n3.id, n5.id)
  mls4.groupId = groupId

  const loopAng = 30
  const n6 = n5.extend(nid(), sid(), 100, loopAng, loopAng, groupId)
    .extend(nid(), sid(), 100, loopAng, loopAng, groupId)
    //.extend(nid(), sid(), 100, loopAng, loopAng, groupId)
    //.extend(nid(), sid(), 100, 5, 5, groupId)
  
  const n8 = getNode(Id('Nt99'))
  //n6.position.y = n8.position.y
  //n6.position.x = 4124
  //n6.position.z = -3340
  //n6.position = new Vector3(4134,n8.position.y,-3340)
 
  const sw1 = createSwitch(n8, 40, -1, 8, groupId, false)
  sw1.node.position = new Vector3(4098.56, 645, -3319.40)
  sw1.node.rotation = new Euler(0,321.81,0)
    
  const seg1 = createSegment(n6.id, sw1.node.id)
  seg1.groupId = groupId
  

  const n9 = getNode(Id('Np1s'))
  console.log(n9.position)
  n9.offset(-20)
  console.log(n9.position)

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

  const span1 = graph.createSpan({
    segmentId: Id("S6np"),
    end: TrackSpanPartEnd.Start,
    distance: 0
  },{
    segmentId: Id("Sok6"),
    end: TrackSpanPartEnd.End,
    distance: 1
  })


  const mixinName = 'Alarka Balloon Loop'
  const mixin: AlinasMapModMixin = {
    items: {
      [zone]: {
        identifier: zone,
        name: mixinName,
        groupIds: [zone],
        description: 'Balloon loop in Alarka, no more fighting with a Wye.',
        prerequisiteSections: ['alarka-branch'],
        trackSpans: [span1.id],
        industryComponent: makeProgIndComp('alarka-bypass-site', [span1.id]),
        area: area.id,
        deliveryPhases: [
          {
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
    name: 'Alarka Loop',
    desc: 'Balloon loop in Alarka, no more fighting with a Wye.',
    mixins: {
      alinasMapMod: mixin
      // whistles: [
      //   {
      //     "name": "Thomas Whistle 2",
      //     "clip": "file(thomas-whistle.ogg)",
      //     "model": {
      //       "assetPackIdentifier": "ttte",
      //       "assetIdentifier": "mdl-thomas-whistle"
      //     }
      //   }
      // ]
    }
  }
}
