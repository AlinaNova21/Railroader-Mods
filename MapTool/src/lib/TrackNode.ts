
import { Euler, MathUtils, Vector3 } from 'three'

import { DEG2RAD } from 'three/src/math/MathUtils.js'
import { Graph, GraphPart } from './Graph.js'
import { Segment, createSegment } from './Segment.js'
import { EulerJson, Id, UP, Vector3Json, dirtyLogSym, eulToJSON, isDirtySym, vecToJSON } from './utils.js'

export interface TrackNodeJson {
  position?: Vector3Json
  rotation?: EulerJson
  flipSwitchStand?: boolean
}

export const createNode = (position?: Vector3, rotation?: Euler) => Graph.Shared.createNode(position, rotation)
export const getNode = (id: Id<TrackNode>) => Graph.Shared.getNode(id)

export const createSwitch = (node: TrackNode | Id<TrackNode>, distance: number, angle = 10, addAngle = 0, groupId = "", flip = false, og?: Graph) => {
  const n = (typeof node == 'string') ? Graph.Shared.getNode(node) : node
  const segs = Object.values(Graph.Shared.segments)
    .filter(s => s.startId == n.id || s.endId == n.id)
  const seg = segs[flip ? 0 : 1]
  const vec = seg.vector(seg.endId == n.id)
  const diff = vec.clone()
  diff.applyAxisAngle(UP, angle * DEG2RAD)
  diff.normalize()
  diff.multiplyScalar(distance)
  diff.add(n.position)
  const nnode = createNode(diff, new Euler(0, n.rotation.y + angle + addAngle, 0))
  const segment = createSegment(n.id, nnode.id)
  segment.groupId = groupId
  return { node: nnode, segment }
}
export class TrackNode implements GraphPart<TrackNodeJson,TrackNode> {
  public flipSwitchStand = false
  public graph: Graph = new Graph()
  public [isDirtySym] = false
  public [dirtyLogSym] = new Set<string>()

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
  switch(nodeId: Id<TrackNode>, segmentId: Id<Segment>, distance: number, angle = 10, addAngle = 0, groupId = "", flip = false, og?: Graph) {
    const segs = Object.values((og ?? this.graph).segments)
      .filter(s => s.startId == this.id || s.endId == this.id)
      const seg = segs[flip ? 0 : 1]
    const vec = seg.vector(seg.endId == this.id)
    const diff = vec.clone()
    diff.applyAxisAngle(UP, angle * DEG2RAD)
    diff.normalize()
    diff.multiplyScalar(distance)
    diff.add(this.position)
    const node = this.graph.newNode(nodeId, diff, new Euler(0, this.rotation.y + angle + addAngle, 0))
    const nseg = this.toNode(segmentId, node)
    nseg.groupId = groupId
    return node
  }
  offset (distance: number) {
    const rot = this.rotation.clone()
    rot.x *= DEG2RAD
    rot.y *= DEG2RAD
    rot.z *= DEG2RAD
    const dir = new Vector3(0,0,1)
    dir.applyEuler(rot)
    // dir.applyAxisAngle(UP, MathUtils.degToRad(this.rotation.y))
    dir.multiplyScalar(distance)
    console.log(`Offset ${this.id} by ${dir.toArray()} ${rot.toArray()} ${this.position.toArray()}`)
    this.position.add(dir)
  }
  markDirty(dirty = true) {
    this[isDirtySym] = dirty
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
    const node = new TrackNode(id as Id<TrackNode>, pos, rot)
    return Object.assign(node, props, { [isDirtySym]: false })
  }
}