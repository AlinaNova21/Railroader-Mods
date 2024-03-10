import { GraphPart } from "./Graph.js"
import { TrackSpan } from "./TrackSpan.js"
import { Id } from "./utils.js"

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

export interface IndustryComponentJson {
  type: IndustryComponentType
  trackSpans: Id<TrackSpan>[]
  carTypeFiler: string
  sharedStorage: boolean
}

export class IndustryComponent implements GraphPart<IndustryComponentJson, IndustryComponent> {
  public trackSpans: Id<TrackSpan>[] = []
  public carTypeFilter = ""
  public sharedStorage = false
  constructor(public id: Id<IndustryComponent>, public type: IndustryComponentType) { }
  toJson(): IndustryComponentJson {
    return {
      type: this.type,
      trackSpans: this.trackSpans,
      carTypeFiler: this.carTypeFilter,
      sharedStorage: this.sharedStorage,
    }
  }
  static fromJson(id: Id<IndustryComponent>, data: IndustryComponentJson): IndustryComponent {
    const { type, ...props } = data
    return Object.assign(new IndustryComponent(id, type), props)
  }

}