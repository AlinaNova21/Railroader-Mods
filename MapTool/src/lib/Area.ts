import { Color, Vector3 } from "three"
import { GraphPart } from "./Graph.js"
import { Industry, IndustryJson } from "./Industry.js"
import { Id, Vector3Json, recordExporter, recordImporter, vecToJSON as vecToJson } from "./utils.js"

export interface AreaJson {
  position: Vector3Json
  radius: number
  tagColor?: ColorJson
  industries: Record<Id<Industry>, IndustryJson>
}

export type ColorJson = [r: number, g: number, b: number]

export class Area implements GraphPart<AreaJson, Area> {
  public industries = new Map<Id<Industry>, Industry>()
  public tagColor = new Color()
  constructor(public id: Id<Area>, public position = new Vector3(), public radius = 100) {

  }
  toJson(): AreaJson {
    return {
      position: vecToJson(this.position),
      radius: this.radius,
      tagColor: this.tagColor.toArray() as ColorJson,
      industries: recordExporter(this.industries)
    }
  }
  static fromJson(id: Id<Area>, data: AreaJson): Area {
    const { industries, ...props } = data
    return Object.assign(new Area(id as Id<Area>), {
      industries: recordImporter(industries, Industry.fromJson)
    }, props)
  }

}