import { TrackSpan } from "./TrackSpan.js"
import { Id } from "./utils.js"

export interface AlinasMapModMixin {
    items: Record<string, AlinasMapModMixinItem>
}

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

export interface AlinasMapModMixinItem {
    identifier: string
    name: string
    groupIds: string[]
    description: string
    prerequisiteSections?: string[]
    deliveryPhases: DeliveryPhase[]
    area?: string
    trackSpans?: Id<TrackSpan>[]
    industryComponent?: string
}