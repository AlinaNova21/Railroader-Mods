import { Euler, Vector3 } from "three"
import { AlinasMapModMixin, DeliveryDirection } from "./AlinasMapMod.js"
import { Graph, GraphJson, GraphPart } from "./Graph.js"
import { Segment } from "./Segment.js"
import { TrackNode } from "./TrackNode.js"
import { TrackSpan } from "./TrackSpan.js"

export const UP = new Vector3(0, 1, 0)

export interface Vector3Json {
  x?: Number
  y?: Number
  z?: Number
}

export interface _HasId {
  id: Id<this>
}

interface _ConstructorById<T extends _HasId> extends _Constructor<T> {
  new(id: Id<T>): T
  (id: Id<T>): T
}

interface _Constructor<T> {
  readonly prototype: T
}

interface _ConstructorById<T extends _HasId> extends _Constructor<T> {
  new(id: Id<T>): T
  (id: Id<T>): T
}

declare namespace Tag {
  const OpaqueTagSymbol: unique symbol

  class OpaqueTag<T> {
    private [OpaqueTagSymbol]: T
  }
}

export type Id<T extends _HasId> = string & Tag.OpaqueTag<T>

export type fromId<T> = T extends Id<infer R> ? R : never;

export type EulerJson = Vector3Json

export function vecToJSON(vector: Vector3) {
  const [x, y, z] = Array.from(vector).map(v => v || undefined)
  return { x, y, z }
}
export function eulToJSON(euler: Euler) {
  let {x, y, z} = euler
  return {
    x: x || undefined,
    y: y || undefined,
    z: z || undefined
  }
}

export function idGenerator(area: string) {
  const nid = incId<TrackNode>(`N${area}`)
  const sid = incId<Segment>(`S${area}`)
  const pid = incId<TrackSpan>(`P${area}`)
  return { nid, sid, pid }
}

export function incId<T extends _HasId>(prefix: string) {
  let id = 0
  let ids = [] as Id<T>[]
  const fn = () => {
    const nid = `${prefix}_${('00' + id++).slice(-2)}` as Id<T>
    ids.push(nid)
    return nid
  }
  fn.all = () => Array.from(ids)
  fn.last = () => ids.slice(-1)[0]
  return fn
}

export function Id<T extends _HasId>(id: string): Id<T> {
  return id as Id<T>
}

export const recordImporter = <T, R extends _HasId>(data: Record<Id<R>, T>, fn: isFromJson<T, R>) => {
  const ret = new Map<Id<R>, R>()
  for (const id in data) {
    ret.set(Id(id), fn(Id(id), data[Id<R>(id)]))
  }
  return ret
}

export const recordExporter = <T extends GraphPart<R, T>, R>(records: Map<Id<T>, T>) => {
  const ret: Record<Id<T>, R> = {}
  records.forEach(r => ret[r.id] = r.toJson())
  return ret
}

export type isFromJson<T, R extends _HasId> = (id: Id<R>, data: T) => R


export type LayoutFunction = (graph: Graph, originalTracks: Graph) => Promise<LayoutFunctionResult>

export interface Mixins {
  gameGraph?: GraphJson,
  alinasMapMod?: AlinasMapModMixin
  [key: string]: any
}

export interface LayoutFunctionResult {
  name?: string
  mixins?: Mixins
}

export const loadHelper = (load: string, count: number, carTypeFilter: string, direction = DeliveryDirection.LoadToIndustry) => ({
  carTypeFilter,
  count,
  load,
  direction,
})