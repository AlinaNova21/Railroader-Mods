import { Euler, Vector3 } from "three"
import { AlinasMapModMixin, AlinasMapModMixinItem } from "./AlinasMapMod.js"
import { Area } from "./Area.js"
import { ModReference } from "./Definition.js"
import { Graph, GraphJson, GraphPart } from "./Graph.js"
import { Industry } from "./Industry.js"
import { IndustryComponentType } from "./IndustryComponent.js"
import { Load } from "./Load.js"
import { ChangeLogEntry } from "./Mods.js"
import { Delivery, DeliveryDirection } from "./Progressions.js"
import { Scenery } from "./Scenery.js"
import { Segment } from "./Segment.js"
import { TrackNode } from "./TrackNode.js"
import { TrackSpan } from "./TrackSpan.js"

export const UP = new Vector3(0, 1, 0)

export interface Vector3Json {
  x?: Number
  y?: Number
  z?: Number
}


export type WithId<T> = T & { id: Id<WithId<T>> }

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
  const [x, y, z] = Array.from(vector) //.map(v => v || undefined)
  return { x, y, z }
}
export function eulToJSON(euler: Euler) {
  let {x, y, z} = euler
  return { x, y, z }
  // return {
  //   x: x || undefined,
  //   y: y || undefined,
  //   z: z || undefined
  // }
}

export interface IdGenerator {
  nid: idIncrementer<TrackNode>
  sid: idIncrementer<Segment>
  pid: idIncrementer<TrackSpan>
  scid: idIncrementer<Scenery>
  lid: idIncrementer<Load>
}

export function idGenerator(zone: string) {
  const nid = incId<TrackNode>(`N${zone}`)
  const sid = incId<Segment>(`S${zone}`)
  const pid = incId<TrackSpan>(`P${zone}`)
  const scid = incId<Scenery>(`Sc${zone}`)
  const lid = incId<Load>(`L${zone}`)
  return { nid, sid, pid, scid, lid }
}

interface idIncrementer<T extends _HasId> {
  (): Id<T>
  all(): Id<T>[]
  last(): Id<T>
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

export const recordImporter = <T, R extends _HasId & isDirty>(data: Record<Id<R>, T> | undefined, fn: isFromJson<T, R>, parent?: isDirty) => {
  const ret: Record<Id<R>, R> = {}
  if (!data) return ret
  for (const id in data) {
    ret[Id<R>(id)] = dirtyWrap(fn(Id(id), data[Id<R>(id)]), false, parent)
  }
  return ret
}

export const recordExporter = <T extends GraphPart<R, T>, R>(records: Record<Id<T>, T>) => {
  const ret: Record<Id<T>, R> = {}
  Object.values(records).forEach(r => {
    if (r.id == 'AN_Test_Mod_00') console.log(r, r[isDirtySym])
    if(!r[isDirtySym]) return
    const out = r.toJson()
    const log = r[dirtyLogSym]
    ret[r.id] = out
    if (!log.has('*')) {
      for(const key in out) {
        if(!log.has(key)) {
          delete out[key]
        }
      }
    }
  })
  return ret
}

export type isFromJson<T, R extends _HasId> = (id: Id<R>, data: T) => R

export interface Mixins {
  gameGraph?: GraphJson | GraphJson[],
  alinasMapMod?: AlinasMapModMixin
  [key: string]: any
}

export type LayoutFunction = (graph: Graph, originalTracks: Graph) => Promise<LayoutFunctionResult>

export interface LayoutFunctionResult {
  name: string
  desc?: string
  version?: string
  changelog?: ChangeLogEntry[]
  mixins?: Mixins
  conflicts?: ModReference[]
  requires?: ModReference[]
}

export const loadHelper = (load: string, count: number, carTypeFilter: string, direction = DeliveryDirection.LoadToIndustry) => ({
  carTypeFilter,
  count,
  load,
  direction,
} as Delivery)

export const generateIndustryForMilestones = (graph: Graph, id: Id<Industry>, mixin: AlinasMapModMixinItem) => {
  const area = graph.areas[Id<Area>(mixin.area || '')]
  if (!area) throw new ReferenceError(`Area '${mixin.area}' not found for mixin ${mixin.name}`)

  const ind = area.industries[id] ?? area.newIndustry(id, mixin.name)
  ind.newComponent(Id(mixin.identifier), mixin.name, {
    type: IndustryComponentType.ProgressionIndustryComponent,
    carTypeFilter: '*',
    sharedStorage: true,
    trackSpans: mixin.trackSpans || [],
  })
} 

export const isDirtySym = Symbol()
export const dirtyLogSym = Symbol()

export interface isDirty {
  [isDirtySym]: boolean
  readonly [dirtyLogSym]: Set<string>
}

export const dirtyWrap = <T extends isDirty>(tgt: T, markDirty = false, parent?: isDirty) => {
  tgt[isDirtySym] = tgt[isDirtySym] ?? false
  if (markDirty) {
    tgt[isDirtySym] = true
    tgt[dirtyLogSym].add('*')
  }
  return new Proxy(tgt, {
    get: (tgt, p) => {
      return p in tgt ? tgt[p as keyof typeof tgt] : tgt
    },
    set: (tgt, p, value) => {
      tgt[p as keyof typeof tgt] = value
      if(p == isDirtySym) return true
      tgt[dirtyLogSym].add(String(p))
      tgt[isDirtySym] = true
      if (parent) parent[isDirtySym] = true
      return true
    }
  })
}

export function WatchDirty<T extends { new (...args:any[]): {}}>(props: string[] = []) {
  let dirty = false
  return (ctr: T) => {
    for(const key in ctr.prototype) {
      if (props.length == 0 || props.includes(key)) {
        let orig = ctr.prototype[key]
        Object.defineProperty(ctr.prototype, key, {
          get: () => orig,
          set: v => {
            orig = v
            dirty = true
          }
        })
      }
    }
    Object.defineProperty(ctr.prototype, isDirtySym, {
      get: () => dirty
    })
  }
}

export function entriesWithId<T extends _HasId,V>(data: Record<Id<T>, V>): [Id<T>, V][] {
  return Object.entries(data).map(([id, item]) => [Id<T>(id), item])
}