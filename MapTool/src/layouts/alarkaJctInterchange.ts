

import { Graph, Id, Industry, IndustryComponentType, LayoutFunctionResult, Segment, TrackSpanPartEnd, createTrackSpan, getNode, getSegment } from '../lib/index.js'

export default async function alarkaJctInterchange(graph: Graph, originalTracks: Graph): Promise<LayoutFunctionResult> {
  const zone = `AN_Alarka_Jct_Interchange`

  const { nid, sid } = Graph.Shared.pushIdGenerator('Alarka_Jct_Interchange')

  const area = graph.getArea(Id('alarka-jct'))
  const indId = Id<Industry>(zone)
  const ind = area.industries[indId] ?? area.newIndustry(indId, 'Alarka Jct Interchange')
  ind.localPosition = getNode(getSegment(Id('Svpx')).startId).position
  ind.usesContract = false

  const quickSpan = (id: Id<Segment>) => createTrackSpan({
    segmentId: id,
    distance: 0,
    end: TrackSpanPartEnd.Start,
  }, {
    segmentId: id,
    distance: 0,
    end: TrackSpanPartEnd.End,
  })

  const indSpans = [
    quickSpan(Id('Sk9w')),
    quickSpan(Id('S52e')),
    quickSpan(Id('Svpx')),
  ].map(s => s.id)

  ind.newComponent('interchange', 'Alarka Jct Interchange', {
    type: IndustryComponentType.Interchange,
    carTypeFilter: '*',
    sharedStorage: true,
    trackSpans: indSpans,
  })

  return {
    name: 'Alarka Jct Interchange',
    desc: 'Add an interchange to Alarka Jct. JSON clone of Alarka Is Big Enough',
    mixins: {},
    conflicts: [
      { id: 'Zamu.AlarkaIsBigEnough' }
    ],
    version: '1.0.0',
    changelog: [
      { version: '1.0.0', desc: 'Initial Creation' }
    ]
  } as LayoutFunctionResult
}