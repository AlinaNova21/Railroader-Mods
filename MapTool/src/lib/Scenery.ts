import { Euler, Vector3 } from "three"
import { Graph, GraphPart } from "./Graph.js"
import { Id, dirtyLogSym, isDirtySym } from "./utils.js"

interface SceneryJson {
  position: Vector3
  rotation: Euler
  scale: Vector3
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
      position: this.position,
      rotation: this.rotation,
      scale: this.scale,
      modelIdentifier: this.modelIdentifier
    }
  }
  
  static fromJson(id: Id<Scenery>, data: SceneryJson) {
    const ret = new Scenery(id)
    ret.position = data.position
    ret.rotation = data.rotation
    ret.scale = data.scale
    ret.modelIdentifier = data.modelIdentifier
    ret[isDirtySym] = false
    ret[dirtyLogSym] = new Set<string>()
    return ret
  }
}