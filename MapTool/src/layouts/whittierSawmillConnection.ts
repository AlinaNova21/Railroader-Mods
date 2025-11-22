
import { Euler, Vector3 } from 'three'

import { Graph, Id, Industry, IndustryComponentId, IndustryComponentType, LayoutFunctionResult, ProgressionsJson, TrackSpan, TrackSpanPartEnd, createNode, createTrackSpan, getNode, loadHelper, mapToBool } from '../lib/index.js'

/** @param {Graph} graph */
export default async function whittierSawmillConnection(graph: Graph, originalTracks: Graph) {
  const zone = `AN_Whittier_Sawmill_Connection`
  const sawmillGroup = zone

  const { nid, sid, pid } = Graph.Shared.pushIdGenerator('Whittier_Sawmill_Connection')

  const int1 = getNode(Id('N1be'))
  const sawmill = getNode(Id('N72a'))

  const yard00 = createNode(new Vector3(12915, 561.25, 4564), new Euler(0, 312, 0))
  const yard01 = yard00.extend(nid(), sid(), 120, -1, 0, sawmillGroup)

  const y0seg = yard00.toNode(sid(), int1)
  const y1seg = yard01.toNode(sid(), sawmill)

  y0seg.priority = -1
  y0seg.groupId = sawmillGroup
  y1seg.groupId = sawmillGroup

  yard01.rotation.y += 1.6
  yard01.position.x += 4
  yard01.position.y -= 1.25
  yard01.position.z += 1

  int1.flipSwitchStand = true

  const site1 = createTrackSpan({
    segmentId: Id("Sj6n"),
    end: TrackSpanPartEnd.End,
    distance: 0,
  }, {
    segmentId: Id("S6if"),
    end: TrackSpanPartEnd.End,
    distance: 0,
  })

  const indId = Id<Industry>(zone)

  const area = graph.getArea(Id('whittier'))

  const ind = area.industries[indId] ?? area.newIndustry(indId, 'East Whittier Expansion')

  const makeProgIndComp = (id: IndustryComponentId, trackSpans: Id<TrackSpan>[]) => {
    ind.newComponent(id, `${ind.name} Site`, {
      type: IndustryComponentType.ProgressionIndustryComponent,
      carTypeFilter: '*',
      sharedStorage: true,
      trackSpans: trackSpans,
    })
    return `${zone}.${id}`
  }

  const industryComponent = makeProgIndComp('sawmill-site', [site1.id])

  const progressions: ProgressionsJson = {
    progressions: {
      ewh: {
        sections: {
          [`${zone}_Sawmill`]: {
            displayName: `Whittier Sawmill Connection`,
            deliveryPhases: [
              {
                industryComponent,
                cost: 2000,
                deliveries: [
                  loadHelper('ballast', 4, 'GB*'),
                  loadHelper('gravel', 2, 'GB*'),
                  loadHelper('ties', 2, 'GB*'),
                  loadHelper('rails', 1, 'FM*'),
                ],
              }
            ],
            enableFeaturesOnUnlock: mapToBool([`${zone}_Sawmill`]),
            description: 'Extend the sawmill track over to the interchange',
          }
        }
      }
    },
    mapFeatures: {
      [`${zone}_Sawmill`]: {
        displayName: `Whittier Sawmill Connection`,
        trackGroupsEnableOnUnlock: mapToBool([sawmillGroup]),
        trackGroupsAvailableOnUnlock: mapToBool([sawmillGroup])
      }
    }
  }

  graph.createSpliney(Id(`${zone}_Sawmill_Poles`), "AlinasMapMod.TelegraphPoleBuilder", {
    polesToRaise: [585, 587]
  })

  return {
    name: 'Whittier Sawmill Connection',
    desc: 'Extend the sawmill track over to the interchange',
    mixins: {
      progressions,
    },
    conflicts: [
      { id: Id('AlinaNova21.WhittierYard') }
    ],
    version: '1.1.2',
    changelog: [
      { version: '1.1.2', desc: 'Updated poles to remove extras' }
    ]
  } as LayoutFunctionResult
}
