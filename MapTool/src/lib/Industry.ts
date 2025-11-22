import { Vector3 } from "three"
import { Area, getArea } from "./Area.js"
import { GraphPart } from "./Graph.js"
import { IndustryComponent } from "./IndustryComponent.js"
import { Id, Vector3Json, dirtyLogSym, dirtyWrap, isDirtySym, vecToJSON } from "./utils.js"


export type IndustryComponentId = string

export interface IndustryJson{
  name: string
  localPosition: Vector3Json
  usesContract: boolean
  components: Record<IndustryComponentId, IndustryComponent>
}


const serializableSym = Symbol()

const SerializeSym = Symbol()
const DeserializeSym = Symbol()

function Serializable<T>() {
  return function (ctr: Function) {
    ctr.prototype[SerializeSym] = function () {
      const props: Partial<T> = {}
      for (const key in this) {
        if (key in props) {
          props[key as keyof T] = this[key]
        }
      }
      return props as T
    }
    ctr.prototype[DeserializeSym] = function (data: Partial<T>) {
      for (const key in data) {
        // if (key in props) {
        //   props[key as keyof T] = this[key]
        // }
      }
      // return props as T
    }
    return ctr
  }
}

export const createIndustry = (area: Id<Area>, id: Id<Industry>, name: string) => getArea(area).createIndustry(id, name)
export const getIndustry = (area: Id<Area>, id: Id<Industry>) => getArea(area).getIndustry(id)
export class Industry implements GraphPart<IndustryJson,Industry> {
  public localPosition = new Vector3()
  public usesContract = false
  public components: Record<IndustryComponentId, IndustryComponent> = {}
  private dirty = false
  public [isDirtySym]: boolean = false
  public [dirtyLogSym] = new Set<string>()

  constructor(public id: Id<Industry>, public name: string = '') { }

  getComponent(id: IndustryComponentId) {
    const ret = this.components[id]
    if (!ret) throw new Error(`IndustryComponent ${id} not found on industry ${this.id}`)
    return ret
  }

  newComponent<T extends IndustryComponent>(id: IndustryComponentId, name: string, params: Omit<T, 'id' | 'name' | typeof isDirtySym | typeof dirtyLogSym >): T {
    const ret = dirtyWrap({
      name,
      [isDirtySym]: true,
      [dirtyLogSym]: new Set(),
      ...params,
    } as T, true)
    this.components[id] = ret
    return ret
  }

  toJson(): IndustryJson {
    return {
      name: this.name,
      localPosition: vecToJSON(this.localPosition),
      usesContract: this.usesContract,
      components: this.components,
    }
  }
  
  static fromJson(id: Id<Industry>, data: IndustryJson) {
    const { localPosition, name, ...props } = data
    const pos = Object.assign(new Vector3(), localPosition)
    const components = Object.fromEntries(Object.entries(data.components).map(([id, v]) => [id, dirtyWrap(v)]))
    return Object.assign(new Industry(id, name), {
      localPosition: pos,
      components,
    }, props, { [isDirtySym]: false, [dirtyLogSym]: new Set() })
  }
} 