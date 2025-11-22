import { Euler, Vector3 } from "three"
import { Graph, GraphPart } from "./Graph.js"
import { EulerJson, Id, Vector3Json, dirtyLogSym, eulToJSON, isDirtySym, vecToJSON } from "./utils.js"

interface SceneryJson {
  position: Vector3Json
  rotation: EulerJson
  scale: Vector3Json
  modelIdentifier: string
}

export const createScenery = (modelIdentifier: string) => Graph.Shared.createScenery(modelIdentifier)
export const getScenery = (id: Id<Scenery>) => Graph.Shared.getScenery(id)

export class Scenery implements GraphPart<SceneryJson, Scenery>{
  public position = new Vector3()
  public rotation = new Euler()
  public scale = new Vector3(1, 1, 1)
  public modelIdentifier = ''
  public [isDirtySym] = false
  public [dirtyLogSym] = new Set<string>

  constructor(public id: Id<Scenery>) {}

  toJson() {
    return {
      position: vecToJSON(this.position),
      rotation: eulToJSON(this.rotation),
      scale: vecToJSON(this.scale),
      modelIdentifier: this.modelIdentifier
    }
  }
  
  static fromJson(id: Id<Scenery>, data: SceneryJson) {
    const ret = new Scenery(id)
    ret.position = Object.assign(new Vector3(), data.position)
    ret.rotation = Object.assign(new Euler(), data.rotation)
    ret.scale = Object.assign(new Vector3(), data.scale)
    ret.modelIdentifier = data.modelIdentifier
    ret[isDirtySym] = false
    ret[dirtyLogSym] = new Set<string>()
    return ret
  }
}