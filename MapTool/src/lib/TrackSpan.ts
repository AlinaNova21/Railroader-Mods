import { Graph, GraphPart } from "./Graph.js"
import { Segment } from "./Segment.js"
import { Id, dirtyLogSym, isDirtySym } from "./utils.js"

export interface TrackSpanJson {
  upper: TrackSpanPart
  lower: TrackSpanPart
}

export enum TrackSpanPartEnd {
  Start = "Start",
  End = "End",
}

export interface TrackSpanPart {
  segmentId: Id<Segment>,
  distance: Number,
  end: TrackSpanPartEnd
}

export const createTrackSpan = (upper: TrackSpanPart, lower: TrackSpanPart) => Graph.Shared.createSpan(upper, lower)

export class TrackSpan implements GraphPart<TrackSpanJson, TrackSpan> {
  public [isDirtySym] = false
  public [dirtyLogSym] = new Set<string>()
  
  constructor(public id: Id<TrackSpan>, public upper: TrackSpanPart, public lower: TrackSpanPart) {}
  toJson(): TrackSpanJson {
    const { upper, lower } = this
    return {
      upper,
      lower,
    }
  }
  static fromJson(id: Id<TrackSpan>, data: TrackSpanJson): TrackSpan {
    const { upper, lower, ...props } = data
    return Object.assign(new TrackSpan(id, upper, lower), props, { [isDirtySym]: false })
  }
}