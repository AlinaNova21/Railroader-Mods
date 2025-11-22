
export type BoolMap = Record<string, Boolean>

export enum DeliveryDirection {
  LoadToIndustry = 0,
  LoadFromIndustry = 1,
}

export interface Delivery {
  carTypeFilter: string
  count: Number
  load: string
  direction: DeliveryDirection
}

export interface DeliveryPhase {
  cost: Number
  industryComponent?: string
  deliveries?: Delivery[]
}

export interface Section {
  displayName: string
  description: string
  deliveryPhases: DeliveryPhase[]
  prerequisiteSections?: BoolMap
  disableFeaturesOnUnlock?: BoolMap
  enableFeaturesOnUnlock?: BoolMap
  enableFeaturesOnAvailable?: BoolMap
}

export interface Progression {
  sections: Record<string, Section>
}

export interface MapFeature {
  displayName: string
  description?: string
  prerequisites?: BoolMap
  areasEnableOnUnlock?: BoolMap
  defaultEnableInSandbox?: Boolean
  gameObjectsEnableOnUnlock?: BoolMap
  trackGroupsAvailableOnUnlock?: BoolMap
  trackGroupsEnableOnUnlock?: BoolMap
  unlockExcludeIndustries?: BoolMap
  unlockIncludeIndustries?: BoolMap
  unlockIncludeIndustryComponents?: BoolMap
}

export interface ProgressionsJson {
  progressions: Record<string, Progression>
  mapFeatures: Record<string, MapFeature>
}

export function mapToBool(list: string[]): BoolMap {
  const ret: BoolMap = {}
  for(const item of list) {
    ret[item] = true
  }
  return ret
}