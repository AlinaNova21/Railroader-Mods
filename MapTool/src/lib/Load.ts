import { Id, isDirty, isDirtySym } from "./utils.js"

export enum Unit {
  Pounds = "Pounds",
  Gallons = "Gallons",
}

export interface Load extends isDirty {
  id: Id<Load>
  description: string,
  units: Unit,
  density: number,
  unitWeightInPounds: number,
  importable: boolean,
  payPerQuantity: number,
  costPerUnit: number
}

export function LoadFromJson(id: Id<Load>, data: Load) {
  const origDirty = data[isDirtySym]
  data.id = id
  data[isDirtySym] = origDirty
  return data
}

export function LoadToJson(id: Id<Load>, data: Load) {
  return data[isDirtySym] ? data : undefined
}