
import { Euler, MathUtils, Vector3 } from 'three'

import { Graph, GraphPart } from './Graph.js'
import { Segment } from './Segment.js'
import { EulerJson, Id, UP, Vector3Json, eulToJSON, vecToJSON } from './utils.js'

export interface TrackNodeJson {
  position?: Vector3Json
  rotation?: EulerJson
  flipSwitchStand?: boolean
}

export class TrackNode implements GraphPart<TrackNodeJson,TrackNode> {
  public flipSwitchStand = false
  public graph: Graph = new Graph()

  constructor(public id: Id<TrackNode>, public position: Vector3, public rotation: Euler) {
  }
  toNode(id: Id<Segment>, node: TrackNode) {
    return this.graph.newSegment(id, this, node)
  }
  // /** @deprecated Use Graph.extend instead */
  extend(id: Id<TrackNode>, segmentId: Id<Segment>, distance: number, angle = 0, addAngle = 0, groupId = "") {
    const dir = new Vector3(0, 0, 1)
    dir.applyAxisAngle(UP, MathUtils.degToRad(this.rotation.y + angle))
    dir.multiplyScalar(distance)
    const node = this.graph.newNode(id, dir.add(this.position), new Euler(0, angle + this.rotation.y + addAngle, 0))
    const seg = this.toNode(segmentId, node)
    seg.groupId = groupId
    return node
  }
  toJson() {
    return {
      position: vecToJSON(this.position),
      rotation: eulToJSON(this.rotation),
      flipSwitchStand: this.flipSwitchStand,
    }
  }
  static fromJson(id: Id<TrackNode>, data: TrackNodeJson): TrackNode {
    const { position = {}, rotation = {}, ...props } = data
    const pos = Object.assign(new Vector3(), position)
    const rot = Object.assign(new Euler(), rotation)
    return Object.assign(new TrackNode(id as Id<TrackNode>, pos, rot), props)
  }
}