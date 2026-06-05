using AlinasRailTools.Scripting;
using AlinasRailTools.StrangeCustomsCompat;
using UnityEngine;

/// <summary>
/// SCCompatLoader - StrangeCustoms Compatibility Loader
/// Thin wrapper that loads mods with definition.json files using the ModLoader system
/// </summary>

Debug.Log("SCCompatLoader: Script starting execution...");

// Create helper with shared zone name
var helper = new Helper("SCCompatLoader");

// Load all mods from the Mods directory
var modLoader = new ModLoader(helper);
modLoader.LoadMods("Mods");

Debug.Log("SCCompatLoader: Script execution completed.");
