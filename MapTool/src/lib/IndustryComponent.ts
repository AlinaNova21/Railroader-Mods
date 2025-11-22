import { Load } from "./Load.js"
import { TrackSpan } from "./TrackSpan.js"
import { Id, isDirty } from "./utils.js"

enum CarTypes {
  All = "*",
  Hoppers = "HM,HT",
  Boxcar = "XM",
  _TeamTrack = "XM*,FM*",
  Skeleton = "XM",
  Flatbed = "FB",
  Tank = "TM*",

}

export enum IndustryComponentType {
  FormulaicIndustryComponent = "Model.Ops.FormulaicIndustryComponent",
  IndustryLoader = "Model.Ops.IndustryLoader",
  IndustryUnLoader = "Model.Ops.IndustryUnLoader",
  Interchange = "Model.Ops.Interchange",
  InterchangedIndustryLoader = "Model.Ops.InterchangedIndustryLoader",
  ProgressionIndustryComponent = "Model.Ops.ProgressionIndustryComponent",
  RepairTrack = "Model.Ops.RepairTrack",
  TeamTrack = "Model.Ops.TeamTrack",
  TeleportLoadingIndustry = "Model.Ops.TeleportLoadingIndustry",
  PaxStationComponent = "AlinasMapMod.PaxStationComponent",
}

export type Interchange = {
  type: IndustryComponentType.Interchange
  name: string
} & IndustryComponentBase

export type InterchangedIndustryLoader = {
  type: IndustryComponentType.InterchangedIndustryLoader
  name: string
  loadId: Id<Load>
} & IndustryComponentBase

export type RepairTrack = {
  type: IndustryComponentType.RepairTrack
} & IndustryComponentBase

export type TeamTrack = {
  type: IndustryComponentType.TeamTrack
} & IndustryComponentBase

export type TeleportLoadingIndustry = {
  type: IndustryComponentType.TeleportLoadingIndustry
} & IndustryComponentBase

export type ProgressionIndustryComponent = {
  type: IndustryComponentType.ProgressionIndustryComponent
} & IndustryComponentBase

export type FormulaicIndustryComponent = {
  type: IndustryComponentType.FormulaicIndustryComponent
  inputTermsPerDay: Record<Id<Load>, number>
  outputTermsPerDay: Record<Id<Load>, number>
} & IndustryComponentBase

export type IndustryLoader = {
  type: IndustryComponentType.IndustryLoader
} & _IndustryLoaderUnLoader

export type IndustryUnLoader = {
  type: IndustryComponentType.IndustryUnLoader
} & _IndustryLoaderUnLoader

export type _IndustryLoaderUnLoader = {
  loadId: Id<Load>
  storageChangeRate: number
  maxStorage: number
  orderAroundEmpties: boolean
  carTransferRate: number
  orderAroundLoaded: boolean
} & IndustryComponentBase

export type PaxStationComponent = {
  type: IndustryComponentType.PaxStationComponent
  loadId: Id<Load>
  basePopulation: number
  timetableCode: string
  neighborIds: string[]
  branch: string
} & IndustryComponentBase

export interface IndustryComponentBase extends isDirty{
  name: string
  trackSpans: Id<TrackSpan>[]
  carTypeFilter: string
  sharedStorage: boolean
}

export type IndustryComponent =
  | FormulaicIndustryComponent
  | IndustryLoader
  | IndustryUnLoader
  | Interchange
  | InterchangedIndustryLoader
  | PaxStationComponent
  | ProgressionIndustryComponent
  | RepairTrack
  | TeamTrack
  | TeleportLoadingIndustry

export type IndustryComponentTypeMap<T> =
  T extends "Model.Ops.FormulaicIndustryComponent" ? FormulaicIndustryComponent :
  T extends "Model.Ops.IndustryLoader" ? IndustryLoader :
  T extends "Model.Ops.IndustryUnLoader" ? IndustryUnLoader :
  T extends "Model.Ops.Interchange" ? Interchange :
  T extends "Model.Ops.InterchangedIndustryLoader" ? InterchangedIndustryLoader :
  T extends "Model.Ops.ProgressionIndustryComponent" ? ProgressionIndustryComponent :
  T extends "Model.Ops.RepairTrack" ? RepairTrack :
  T extends "Model.Ops.TeamTrack" ? TeamTrack :
  T extends "Model.Ops.TeleportLoadingIndustry" ? TeleportLoadingIndustry :
  never
