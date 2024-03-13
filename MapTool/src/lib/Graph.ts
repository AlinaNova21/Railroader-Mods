import { Euler, MathUtils, Vector3 } from 'three'
import { Area, AreaJson } from './Area.js'
import { Load, LoadFromJson, Unit } from './Load.js'
import { Segment, SegmentJson } from './Segment.js'
import { TrackNode, TrackNodeJson } from './TrackNode.js'
import { TrackSpan, TrackSpanJson, TrackSpanPart } from './TrackSpan.js'
import { Id, UP, _HasId, dirtyLogSym, dirtyWrap, isDirty, isDirtySym, recordExporter, recordImporter } from './utils.js'

export interface GraphPart<T,C extends _HasId> extends isDirty {
  id: Id<C>
  toJson(): T
}

export interface GraphJson {
  scenery: Record<Id<any>,any>
  nodes: Record<Id<TrackNode>,TrackNodeJson>
  segments: Record<Id<Segment>,SegmentJson>
  areas: Record<Id<Area>,AreaJson>
  spans: Record<Id<TrackSpan>,TrackSpanJson>
  loads: Record<Id<Load>, Load>
}

export class Graph implements isDirty {
  public nodes:Record<Id<TrackNode>, TrackNode> = {}
  public segments:Record<Id<Segment>, Segment> = {}
  public areas:Record<Id<Area>, Area> = {}
  public spans:Record<Id<TrackSpan>, TrackSpan> = {}
  public scenery:Record<Id<any>, any> = {}
  public loads:Record<Id<Load>, Load> = {}
  public [isDirtySym] = false
  public [dirtyLogSym] = new Set<string>()

  constructor() {
  }
  getNode(id: Id<TrackNode>) {
    const ret = this.nodes[id]
    if (!ret) throw new Error(`Node ${id} not found`)
    return ret
  }
  newNode(id: Id<TrackNode>, position: Vector3, rotation = new Euler()) {
    const node = dirtyWrap(new TrackNode(id, position, rotation), true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('nodes')
    this.nodes[id] = node
    node.graph = this
    return node
  }
  importNode(node: TrackNode) {
    if (this.nodes[node.id]) return this.nodes[node.id] 
    node = dirtyWrap(node, true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('nodes')
    this.nodes[node.id] = node
    node.graph = this
    return node
  }
  getSegment(id: Id<Segment>) {
    const ret = this.segments[id]
    if (!ret) throw new Error(`Segment ${id} not found`)
    return ret
  }
  newSegment(id: Id<Segment>, node1: TrackNode | Id<TrackNode>, node2: TrackNode | Id<TrackNode>) {
    if (node1 instanceof TrackNode) node1 = node1.id
    if (node2 instanceof TrackNode) node2 = node2.id
    const segment = dirtyWrap(new Segment(id, node1, node2), true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('segments')
    this.segments[id] = segment
    segment.graph = this
    return segment
  }
  importSegment(segment: Segment) {
    this[isDirtySym] = true
    this[dirtyLogSym].add('segments')
    this.segments[segment.id] = segment
    segment.graph = this
    return segment
  }
  getArea(id: Id<Area>) {
    const ret = this.areas[id]
    if (!ret) throw new Error(`Area ${id} not found`)
    return ret
  }
  newArea(id: Id<Area>) {
    const area = dirtyWrap(new Area(id), true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('areas')
    this.areas[id] = area
    return area
  }
  importArea(area: Area) {
    this[isDirtySym] = true
    this[dirtyLogSym].add('areas')
    this.areas[area.id] = area
    return area
  }
  getSpan(id: Id<TrackSpan>) {
    const ret = this.spans[id]
    if (!ret) throw new Error(`Span ${id} not found`)
    return ret
  }
  newSpan(id: Id<TrackSpan>, upper: TrackSpanPart, lower: TrackSpanPart) {
    const span = dirtyWrap(new TrackSpan(id, upper, lower), true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('spans')
    this.spans[id] = span
    return span
  }
  getLoad(id: Id<Load>) {
    const ret = this.loads[id]
    if (!ret) throw new Error(`Load ${id} not found`)
    return ret
  }
  newLoad(id: Id<Load>, params: Partial<Load>) {
    const load = dirtyWrap(Object.assign({
      id,
      description: '',
      units: Unit.Pounds,
      density: 30.0,
      unitWeightInPounds: 0.0,
      importable: true,
      payPerQuantity: 0.0,
      costPerUnit: 0.0,
      [isDirtySym]: true,
      [dirtyLogSym]: new Set(),
    }, params))
    this[isDirtySym] = true
    this[dirtyLogSym].add('loads')
    this.loads[id] = load
    return load
  }
  cloneNode(id: Id<TrackNode>, newId: Id<TrackNode>) {
    const { position, rotation } = this.nodes[id] as TrackNode
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
    Object.values(g.nodes).forEach(node => {
      node.graph = g
      node[isDirtySym] = false
    })
    
    g.segments = recordImporter(data.segments, Segment.fromJson)
    Object.values(g.segments).forEach(segment => {
      segment.graph = g
      segment[isDirtySym] = false
    })
    
    g.spans = recordImporter(data.spans, TrackSpan.fromJson, g)
    g.areas = recordImporter(data.areas, Area.fromJson, g)
    
    g.loads = recordImporter(data.loads, LoadFromJson, g)
    return g
  }
  toJSON() {
    const ret: GraphJson = {
      nodes: {},
      segments: {},
      areas: {},
      spans: {},
      scenery: {},
      loads: {},
    }
    ret.nodes = recordExporter(this.nodes)
    ret.segments = recordExporter(this.segments)


    // const exp = <T extends GraphPart<R, T>, R>(records: Record<Id<T>, T>) => {
    //   const ret: Record<Id<T>, R> = {}
    //   Object.values(records).forEach(r => {
    //     console.log(r.id, r[isDirtySym])
    //     if (!r[isDirtySym]) return
    //     ret[r.id] = r.toJson()
    //   })
    //   return ret
    // }

    // ret.segments = exp(this.segments)

    ret.spans = recordExporter(this.spans)
    ret.areas = recordExporter(this.areas)
    ret.scenery = recordExporter(this.scenery)
    Object.values(this.loads).forEach(load => { 
      if(load.id == Id("water") && load[isDirtySym]) {
        console.log(`Load ${load.id} is dirty ${JSON.stringify(load)} ${load[isDirtySym]}`)
        ret.loads[load.id] = load
      }
    })
    return ret
  }

  merge(graph: Graph) {
    const mergeObjs = <T extends _HasId>(obj1: Record<Id<T>, T >, obj2: Record<Id<T>, T>) => 
      Object.fromEntries([...Object.entries(obj1), ...Object.entries(obj2)])
    this.nodes = mergeObjs(this.nodes, graph.nodes)
    this.segments = mergeObjs(this.segments, graph.segments)
    this.spans = mergeObjs(this.spans, graph.spans)
    this.areas = mergeObjs(this.areas, graph.areas)
    this.scenery = mergeObjs(this.scenery, graph.scenery)
    this.loads = mergeObjs(this.loads, graph.loads)
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