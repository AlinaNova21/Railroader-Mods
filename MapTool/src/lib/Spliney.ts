import { Euler, Vector3 } from "three"
import { Id, dirtyLogSym, isDirty, isDirtySym } from "./utils.js"

export interface SplineyJson {
  handler: string
  [key: string]: any
}
export interface SplineyBase extends isDirty {
  id: Id<Spliney>
  handler: string
  [key: string]: any
}

export type Spliney = SplineyBase

export function SplineyFromJson(id: Id<Spliney>, data: Spliney) {
  const origDirty = data[isDirtySym]
  data.id = id
  data[isDirtySym] = origDirty
  return data
}

export function makeTurntable(id: Id<Spliney>, position: Vector3, rotation: Euler = new Euler(), stalls = 0): SplineyJson {
  return {
    id,
    handler: 'AlinasMapMod.Turntable.TurntableBuilder',
    position,
    rotation,
    roundhouseStalls: stalls,
    [isDirtySym]: true,
    [dirtyLogSym]: new Set('*'),
  }
}
