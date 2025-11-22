import { Id } from "./utils.js"

export type ModReference = string | {
  id: Id<ModDefinition>,
  notBefore?: string,
  notAfter?: string,
}

/**  Allows the definition of a mixinto with additional metadata. In manifests, these can be implicitly created
 * from strings, and usually are defined as arrays. */
export type MixintoDefinition = string | {
  /** The file()-encoded path to the file that should be returned */
  mixinto: string
  /**
   * If set, a list of requirements that this mixinto has. If any of the requirements are not met,
   * the mixinto will not be returned (i.e. will be ignored). A requirement is met when the mod is
   * enabled, loaded, and within the version range (if specified).
   **/
  requires?: ModReference[]
  /**
   * If set, a list of conflicting mods this mixinto has. If any of the conflicts are met,
   * the mixinto will not be returned (i.e. will be ignored). A requirement is met when the mod is enabled,
   * loaded, and within the version range (if specified).
   **/
  conflictsWith?: ModReference[]
}

export type MixintoDefinitions = MixintoDefinition | MixintoDefinition[]

export interface ModDefinition {
  /**
   * The manifest version this mod was made/configured for. Larger breaking changes may cause
	 * this version to be bumped, which will cause "incompatible" mods to not be loaded. 
   **/
  manifestVersion: number,
  /** Defines an unique ID for the mod, used for e.g. referencing other mods. */
  id: Id<ModDefinition>,
  /** Name of the mod. This is currently only displayed in the console/log. */
  name: string,
  /** Version of the mod. This is currently only displayed in the console/log. */
  version: string,
  /** List of assemblies to load. This will load the mentioned assemblies in the order
   * they're specified. This list should usually only contain assemblies containing Railloader.PluginBase,
   * but could contain dependencies. */
  assemblies?: string[],
  /** Defines the "priority" for this mod.
   * Mods with a lower number will be loaded before mods with a higher number (unless there's a dependency somewhere). */
  priority?: number,
  /** If set, defines a list of mod ids that should be loaded AFTER this one.
   * This property is optional, and entries are optional too. This is not a dependency feature. */
  loadBefore?: string[],
  /** If set, defines a list of mod ids that should be loaded BEFORE this one.
   * This property is optional, and entries are optional too. This is not a dependency feature. */
  loadAfter?: ModReference[],
  /** If set, defines a list of mod ids that should be loaded BEFORE this one.
   * This property is optional, but if set, all mods in the list must be available. Otherwise, this mod will not be loaded. */
  requires?: ModReference[],
  /** If set, defines a list of mod references that will cause this mod to fail to load. */
  conflictsWith?: ModReference[],
  /** If set, the mod can be updated by fetching the update manifest from this URL. */
  updateUrl?: string,
  /** If set, the mod defines that it wants to mix something into an existing resource */
  mixintos?: Record<string, MixintoDefinitions>,
}