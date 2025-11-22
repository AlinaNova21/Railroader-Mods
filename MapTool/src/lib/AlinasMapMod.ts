import { DeliveryPhase } from "./Progressions.js"
import { TrackSpan } from "./TrackSpan.js"
import { Id } from "./utils.js"

export interface AlinasMapModMixin {
    items: Record<string, AlinasMapModMixinItem>
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