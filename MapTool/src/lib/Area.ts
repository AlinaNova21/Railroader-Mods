import { Color, Vector3 } from "three"
import { Graph, GraphPart } from "./Graph.js"
import { Industry, IndustryJson } from "./Industry.js"
import { Id, Vector3Json, dirtyLogSym, dirtyWrap, isDirtySym, recordExporter, recordImporter, vecToJSON as vecToJson } from "./utils.js"

export interface AreaJson {
  position?: Vector3Json
  radius?: number
  tagColor?: ColorJson
  industries: Record<Id<Industry>, IndustryJson>
}

export type ColorJson = [r: number, g: number, b: number]

export const createArea = (id: Id<Area>, position: Vector3, radius: number) => new Area(id, position, radius)
export const getArea = (id: Id<Area>) => Graph.Shared.areas[id]

export class Area implements GraphPart<AreaJson, Area> {
  public industries: Record<Id<Industry>, Industry> = {}
  public tagColor = new Color()
  public [isDirtySym] = false
  public [dirtyLogSym] = new Set<string>()
  
  constructor(public id: Id<Area>, public position = new Vector3(), public radius = 100) {}

  getIndustry(id: Id<Industry>) {
    const ret = this.industries[id]
    if (!ret) throw new Error(`Industry ${id} not found`)
    return ret
  }
  createIndustry(id: Id<Industry>, name: string) {
    const ind = dirtyWrap(new Industry(id, name), true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('industries')
    this.industries[id] = ind
    return ind
  }
  newIndustry(id: Id<Industry>, name: string) {
    const ind = dirtyWrap(new Industry(id, name), true)
    this[isDirtySym] = true
    this[dirtyLogSym].add('industries')
    this.industries[id] = ind
    return ind
  }
  toJson(): AreaJson {
    return {
      position: vecToJson(this.position),
      radius: this.radius,
      // tagColor: this.tagColor.toArray() as ColorJson,
      industries: recordExporter(this.industries)
    }
  }
  static fromJson(id: Id<Area>, data: AreaJson): Area {
    const area = new Area(id as Id<Area>)
    const { industries, tagColor = [], ...props } = data
    return Object.assign(area, {
      tagColor: new Color(...tagColor),
      industries: recordImporter(industries, Industry.fromJson, area)
    }, props, { [isDirtySym]: false })
  }
}