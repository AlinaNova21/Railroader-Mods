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
  FormulaicIndustryComponent = "Model.OpsNew.FormulaicIndustryComponent",
  IndustryLoader = "Model.OpsNew.IndustryLoader",
  IndustryUnLoader = "Model.OpsNew.IndustryUnLoader",
  Interchange = "Model.OpsNew.Interchange",
  InterchangedIndustryLoader = "Model.OpsNew.InterchangedIndustryLoader",
  ProgressionIndustryComponent = "Model.OpsNew.ProgressionIndustryComponent",
  RepairTrack = "Model.OpsNew.RepairTrack",
  TeamTrack = "Model.OpsNew.TeamTrack",
  TeleportLoadingIndustry = "Model.OpsNew.TeleportLoadingIndustry",
}

export type Interchange = {
  type: IndustryComponentType.Interchange
} & IndustryComponentBase

export type InterchangedIndustryLoader = {
  type: IndustryComponentType.InterchangedIndustryLoader
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
  | ProgressionIndustryComponent
  | RepairTrack
  | TeamTrack
  | TeleportLoadingIndustry

export type IndustryComponentTypeMap<T> =
  T extends "Model.OpsNew.FormulaicIndustryComponent" ? FormulaicIndustryComponent :
  T extends "Model.OpsNew.IndustryLoader" ? IndustryLoader :
  T extends "Model.OpsNew.IndustryUnLoader" ? IndustryUnLoader :
  T extends "Model.OpsNew.Interchange" ? Interchange :
  T extends "Model.OpsNew.InterchangedIndustryLoader" ? InterchangedIndustryLoader :
  T extends "Model.OpsNew.ProgressionIndustryComponent" ? ProgressionIndustryComponent :
  T extends "Model.OpsNew.RepairTrack" ? RepairTrack :
  T extends "Model.OpsNew.TeamTrack" ? TeamTrack :
  T extends "Model.OpsNew.TeleportLoadingIndustry" ? TeleportLoadingIndustry :
  never
