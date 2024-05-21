import { Id, isDirty, isDirtySym } from "./utils.js"

export interface SplineyJson {
  handler: string
  [key: string]: any
}
export interface Spliney extends isDirty {
  id: Id<Spliney>
  handler: string
  [key: string]: any
}

export function SplineyFromJson(id: Id<Spliney>, data: Spliney) {
  const origDirty = data[isDirtySym]
  data.id = id
  data[isDirtySym] = origDirty
  return data
}
