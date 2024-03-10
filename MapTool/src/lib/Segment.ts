import { GraphPart, } from "./Graph.js"
import { TrackNode } from "./TrackNode.js"
import { Id } from "./utils.js"

export enum SegmentStyle {
  Standard = "Standard",
  Tunnel = "Tunnel",
  Bridge = "Bridge",
}

export interface SegmentJson {
  startId: Id<TrackNode>
  endId: Id<TrackNode>
  priority?: number
  groupId?: string
  style?: SegmentStyle
}

export class Segment implements GraphPart<SegmentJson,Segment> {
  public style = SegmentStyle.Standard
  public priority = 0
  public groupId = ""
  constructor(public id: Id<Segment>, public startId: Id<TrackNode> = Id(""), public endId: Id<TrackNode> = Id("")) {}
  toJson() {
    return {
      style: this.style,
      startId: this.startId,
      endId: this.endId,
      priority: this.priority,
      groupId: this.groupId,
    }
  }
  static fromJson(id: Id<Segment>, data: SegmentJson): Segment {
    return Object.assign(new Segment(id as Id<Segment>), data)
  }
}