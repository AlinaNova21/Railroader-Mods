import { Vector3 } from "three"
import { GraphPart } from "./Graph.js"
import { Id, Vector3Json, vecToJSON } from "./utils.js"

export interface IndustryJson {
  localPosition: Vector3Json
  usesContract: boolean
  components: IndustryComponents
}

type IndustryComponents = {}

export class Industry implements GraphPart<IndustryJson,Industry> {
  public localPosition = new Vector3()
  public usesContract = false
  public components = {}
  constructor(public id: Id<Industry>) { }
  toJson(): IndustryJson {
    return {
      localPosition: vecToJSON(this.localPosition),
      usesContract: this.usesContract,
      components: this.components,
    }
  }
  static fromJson(id: Id<Industry>, data: IndustryJson) {
    const { localPosition, ...props } = data
    const pos = Object.assign(new Vector3(), localPosition)
    return Object.assign(new Industry(id), {
      localPosition: pos
    }, props)
  }
} 