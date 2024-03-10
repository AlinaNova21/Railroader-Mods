import { Euler, MathUtils, Vector3 } from 'three'
import { Area, AreaJson } from './Area.js'
import { Segment, SegmentJson } from './Segment.js'
import { TrackNode, TrackNodeJson } from './TrackNode.js'
import { TrackSpan, TrackSpanJson, TrackSpanPart } from './TrackSpan.js'
import { Id, UP, _HasId, recordImporter } from './utils.js'

export interface GraphPart<T,C extends _HasId> {
  id: Id<C>
  toJson(): T
}

export interface GraphJson {
  nodes: Record<Id<TrackNode>,TrackNodeJson>
  segments: Record<Id<Segment>,SegmentJson>
  areas: Record<Id<Area>,AreaJson>
  spans: Record<Id<TrackSpan>,TrackSpanJson>
}

export class Graph {
  public nodes = new Map<Id<TrackNode>, TrackNode>()
  public segments = new Map<Id<Segment>, Segment>()
  public areas = new Map<Id<Area>, Area>()
  public spans = new Map<Id<TrackSpan>, TrackSpan>()
  constructor() {
  }
  newNode(id: Id<TrackNode>, position: Vector3, rotation = new Euler()) {
    const node = new TrackNode(id, position, rotation)
    node.graph = this
    this.nodes.set(id, node)
    return node
  }
  newSegment(id: Id<Segment>, node1: TrackNode | Id<TrackNode>, node2: TrackNode | Id<TrackNode>) {
    if (node1 instanceof TrackNode) node1 = node1.id
    if (node2 instanceof TrackNode) node2 = node2.id
    const segment = new Segment(id, node1, node2)
    this.segments.set(id, segment)
    return segment
  }
  newArea(id: Id<Area>) {
    const area = new Area(id)
    this.areas.set(id, area)
    return area
  }
  newSpan(id: Id<TrackSpan>, upper: TrackSpanPart, lower: TrackSpanPart) {
    const span = new TrackSpan(id, upper, lower)
    this.spans.set(id, span)
    return span
  }
  importNode(node: TrackNode) {
    this.nodes.set(node.id, node)
    node.graph = this
    return node
  }
  importSegment(segment: Segment) {
    this.segments.set(segment.id, segment)
    return segment
  }
  cloneNode(id: Id<TrackNode>, newId: Id<TrackNode>) {
    const { position, rotation } = this.nodes.get(id) as TrackNode
    return this.newNode(newId, position.clone(), rotation.clone())
  }
  extend(srcNode: TrackNode, id: Id<TrackNode>, segmentId: Id<Segment>, distance: number, angle = 0, addAngle = 0) {
    const dir = new Vector3(0, 0, 1)
    dir.applyAxisAngle(UP, MathUtils.degToRad(srcNode.rotation.y + angle))
    dir.multiplyScalar(distance)
    const node = this.newNode(id, dir.add(srcNode.position), new Euler(0, angle + srcNode.rotation.y + addAngle, 0))
    srcNode.toNode(segmentId, node)
    return node
  }
  static fromJSON(data: GraphJson) {
    const g = new Graph()
    
    g.nodes = recordImporter(data.nodes, TrackNode.fromJson)
    g.nodes.forEach(node => node.graph = g)

    g.segments = recordImporter(data.segments, Segment.fromJson)
    g.spans = recordImporter(data.spans, TrackSpan.fromJson)
    g.areas = recordImporter(data.areas, Area.fromJson)
    
    return g
  }
  toJSON() {
    const ret: GraphJson = {
      nodes: {},
      segments: {},
      areas: {},
      spans: {},
    }
    this.nodes.forEach(node => ret.nodes[node.id] = node.toJson())
    this.segments.forEach(segment => ret.segments[segment.id] = segment.toJson())
    this.spans.forEach(span => ret.spans[span.id] = span.toJson())
    this.areas.forEach(area => ret.areas[area.id] = area.toJson())
    return ret
  }
  merge(graph: Graph) {
    this.nodes = new Map([...this.nodes.entries(), ...graph.nodes.entries()])
    this.segments = new Map([...this.segments.entries(), ...graph.segments.entries()])
    this.spans = new Map([...this.spans.entries(), ...graph.spans.entries()])
    this.areas = new Map([...this.areas.entries(), ...graph.areas.entries()])
    return this
  }
  static merge(g1: Graph, g2: Graph) {
    return g1.merge(g2)
  }
  static mergeAll(graphs: Graph[]) {
    const graph = new Graph()
    graphs.forEach(g => graph.merge(g))
    return graph
  }
}