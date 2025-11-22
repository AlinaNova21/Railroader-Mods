import { Euler, MathUtils, Vector3 } from 'three'
import { Area, AreaJson } from './Area.js'
import { Load, LoadFromJson, Unit } from './Load.js'
import { Scenery } from './Scenery.js'
import { Segment, SegmentJson } from './Segment.js'
import { Spliney, SplineyFromJson, SplineyJson } from './Spliney.js'
import { TrackNode, TrackNodeJson } from './TrackNode.js'
import { TrackSpan, TrackSpanJson, TrackSpanPart } from './TrackSpan.js'
import { Id, IdGenerator, UP, _HasId, dirtyLogSym, dirtyWrap, idGenerator, isDirty, isDirtySym, recordExporter, recordImporter } from './utils.js'

export interface GraphPart<T,C extends _HasId> extends isDirty {
  id: Id<C>
  toJson(): T
}

export interface GraphJson {
  tracks?: {
    nodes?: Record<Id<TrackNode>, TrackNodeJson>
    segments?: Record<Id<Segment>, SegmentJson>
    spans?: Record<Id<TrackSpan>, TrackSpanJson>
  }
  scenery?: Record<Id<any>,any>
  areas?: Record<Id<Area>,AreaJson>
  loads?: Record<Id<Load>, Load>
  splineys?: Record<Id<any>, SplineyJson>
}

export class Graph implements isDirty {
  private static instance: Graph
  static get Shared() {
    if(!Graph.instance) Graph.instance = new Graph()
    return Graph.instance
  }
  public activate() {
    Graph.instance = this
  }
  public nodes:Record<Id<TrackNode>, TrackNode> = {}
  public segments:Record<Id<Segment>, Segment> = {}
  public areas:Record<Id<Area>, Area> = {}
  public spans:Record<Id<TrackSpan>, TrackSpan> = {}
  public scenery:Record<Id<Scenery>, Scenery> = {}
  public splineys:Record<Id<Spliney>, Spliney> = {}
  public loads:Record<Id<Load>, Load> = {}
  public [isDirtySym] = false
  public [dirtyLogSym] = new Set<string>()
  private idStack = [] as [string, IdGenerator][]
  private get idGenerator() {
    return this.idStack[this.idStack.length - 1][1]
  } 
  public get id() {
    return {
      Node: () => this.idGenerator.nid(),
      Segment: () => this.idGenerator.sid(),
      Span: () => this.idGenerator.pid(),      
      Scenery: () => this.idGenerator.scid(),
      Load: () => this.idGenerator.lid(),
    }
  } 

  constructor() {
    this.resetIdGenerator()
  }
  resetIdGenerator() {
    this.idStack = []
    this.pushIdGenerator('AN', true)
  }
  pushIdGenerator(zone: string, full = false) {
    if(!full) {
      const lastZone = this.idStack[this.idStack.length - 1][0]
      zone = `${lastZone}_${zone}`
    }
    this.idStack.push([zone, idGenerator(zone)])
    return this.idGenerator
  }
  popIdGenerator() {
    this.idStack.pop()
  }

  getNode(id: Id<TrackNode>) {
    const ret = this.nodes[id]
    if (!ret) throw new Error(`Node ${id} not found`)
    return ret
  }
  createNode(position = new Vector3(), rotation = new Euler()) {
    return this.newNode(this.idGenerator.nid(), position, rotation)
  }
  /** @deprecated use createNode instead */
  newNode(id?: Id<TrackNode>, position: Vector3 = new Vector3(), rotation = new Euler()): TrackNode {
    id = id || this.idGenerator.nid()
    const node = dirtyWrap(new TrackNode(id, position, rotation), true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('nodes')
    this.nodes[id] = node
    node.graph = this
    return node
  }

  createSegment(node1: Id<TrackNode>, node2: Id<TrackNode>) {
    return this.newSegment(this.idGenerator.sid(), node1, node2)
  }
  getSegment(id: Id<Segment>) {
    const ret = this.segments[id]
    if (!ret) throw new Error(`Segment ${id} not found`)
    return ret
  }
  /** @deprecated use createSegment instead */
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

  createArea(id: Id<Area>) {
    return this.newArea(id)
  }
  getArea(id: Id<Area>) {
    const ret = this.areas[id]
    if (!ret) throw new Error(`Area ${id} not found`)
    return ret
  }
  /** @deprecated use createArea instead */
  newArea(id: Id<Area>) {
    const area = dirtyWrap(new Area(id), true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('areas')
    this.areas[id] = area
    return area
  }

  createSpan(upper: TrackSpanPart, lower: TrackSpanPart) {
    return this.newSpan(this.idGenerator.pid(), upper, lower)
  }
  getSpan(id: Id<TrackSpan>) {
    const ret = this.spans[id]
    if (!ret) throw new Error(`Span ${id} not found`)
    return ret
  }
  /** @deprecated use createSpan instead */
  newSpan(id: Id<TrackSpan>, upper: TrackSpanPart, lower: TrackSpanPart) {
    const span = dirtyWrap(new TrackSpan(id, upper, lower), true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('spans')
    this.spans[id] = span
    return span
  }

  createScenery(modelIdentifier: string, position = new Vector3(), rotation = new Euler(), scale = new Vector3(1, 1, 1)) {
    const id = this.idGenerator.scid()
    this.scenery[id] = dirtyWrap(new Scenery(id), true)
    this.scenery[id].modelIdentifier = modelIdentifier
    this.scenery[id].position = position
    this.scenery[id].rotation = rotation
    this.scenery[id].scale = scale
    this[isDirtySym] = true
    this[dirtyLogSym].add('scenery')
    return this.scenery[id]
  }
  getScenery(id: Id<Scenery>) {
    const ret = this.scenery[id]
    if (!ret) throw new Error(`Scenery ${id} not found`)
    return ret
  }

  createLoad(id: Id<Load>, params: Partial<Load>) {
    return this.newLoad(id, params)
  }
  getLoad(id: Id<Load>) {
    const ret = this.loads[id]
    if (!ret) throw new Error(`Load ${id} not found`)
    return ret
  }
  /** @deprecated use createLoad instead */
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
  createSpliney(id: Id<Spliney>, handler: string, params: Record<string, any> = {}) {
    const spliney = dirtyWrap(Object.assign({
      id,
      handler,
      [isDirtySym]: true,
      [dirtyLogSym]: new Set<string>(),
    }, params))
    this[isDirtySym] = true
    this[dirtyLogSym].add('splineys')
    this.splineys[id] = spliney
    return spliney
  }
  getSpliney(id: Id<Spliney>) {
    const ret = this.splineys[id]
    if (!ret) throw new Error(`Spliney ${id} not found`)
    return ret
  }

  cloneNode(id: Id<TrackNode>) {
    const { position, rotation } = this.nodes[id] as TrackNode
    return this.newNode(this.idGenerator.nid(), position.clone(), rotation.clone())
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
    if (!Graph.instance) Graph.instance = g
    g.nodes = recordImporter(data.tracks?.nodes, TrackNode.fromJson)
    Object.values(g.nodes).forEach(node => {
      node.graph = g
      node[isDirtySym] = false
    })
    g.segments = recordImporter(data.tracks?.segments, Segment.fromJson)
    Object.values(g.segments).forEach(segment => {
      segment.graph = g
      segment[isDirtySym] = false
    })
    g.spans = recordImporter(data.tracks?.spans, TrackSpan.fromJson, g)
    g.areas = recordImporter(data.areas, Area.fromJson, g)
    g.loads = recordImporter(data.loads, LoadFromJson, g)
    g.scenery = recordImporter(data.scenery, SplineyFromJson)
    return g
  }
  toJSON() {
    const ret: GraphJson = {}
    ret.tracks = ret.tracks || {}
    ret.tracks.nodes = recordExporter(this.nodes)
    ret.tracks.segments = recordExporter(this.segments)
    ret.tracks.spans = recordExporter(this.spans)
    ret.areas = recordExporter(this.areas)
    ret.scenery = recordExporter(this.scenery)
    ret.splineys = {}
    ret.loads = {}
    Object.values(this.splineys).forEach(spl => {
      if(spl[isDirtySym]) {
        console.log(`Spliney ${spl.id} is dirty ${JSON.stringify(spl)} ${spl[isDirtySym]}`)
        const { id, ...rest } = spl
        // @ts-ignore undefined
        ret.splineys[id] = rest
      }
    })
    Object.values(this.loads).forEach(load => { 
      if(load.id == Id("water") && load[isDirtySym]) {
        console.log(`Load ${load.id} is dirty ${JSON.stringify(load)} ${load[isDirtySym]}`)
        // @ts-ignore undefined
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
    this.splineys = mergeObjs(this.splineys, graph.splineys)
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