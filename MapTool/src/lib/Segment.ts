import { DEG2RAD } from "three/src/math/MathUtils.js"
import { Graph, GraphPart, } from "./Graph.js"
import { TrackNode } from "./TrackNode.js"
import { Id, UP, dirtyLogSym, isDirtySym } from "./utils.js"

export enum SegmentStyle {
  Standard = "Standard",
  Tunnel = "Tunnel",
  Bridge = "Bridge",
}

export interface SegmentJson {
  startId?: Id<TrackNode>
  endId?: Id<TrackNode>
  priority?: number
  groupId?: string
  style?: SegmentStyle
}

export const createSegment = (start: Id<TrackNode>, end: Id<TrackNode>) => Graph.Shared.createSegment(start, end)
export const getSegment = (id: Id<Segment>) => Graph.Shared.getSegment(id)

export class Segment implements GraphPart<SegmentJson,Segment> {
  public style = SegmentStyle.Standard
  public priority = 0
  public groupId = ""
  public graph: Graph = new Graph()
  public [isDirtySym] = false
  public [dirtyLogSym] = new Set<string>()
  
  constructor(public id: Id<Segment>, public startId: Id<TrackNode> = Id(""), public endId: Id<TrackNode> = Id("")) {}
  flip() {
    const tmp = this.startId
    this.startId = this.endId
    this.endId = tmp
    this[isDirtySym] = true
  }
  vector(flip = false) {
    const n1 = this.graph.getNode(this.startId)
    const n2 = this.graph.getNode(this.endId)
    const diff = n1.position.clone().sub(n2.position)
    if (flip) diff.applyAxisAngle(UP, 180 * DEG2RAD)
    return diff.normalize()
  }
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
    return Object.assign(new Segment(id as Id<Segment>), data, { [isDirtySym]: false })
  }
}