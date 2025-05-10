# Splineys

All listed values are defaults, and may usually be omitted unless otherwise noted

- [Prefab Formats](#prefab-formats)
- [Telegraph Poles](#telegraph-poles)
- [Turntables](#turntables)
- [Loaders](#loaders)
- [Passenger Station Agent](#passenger-station-agent-includes-building)]
- [Passenger Stations](#passenger-stations)


## Prefab formats
All prefabs use the following formats:
- Path based: `path://scene/world/path/to/gameObject/in/tree`
- Scenery: `scenery://sampleSceneryId`
- Vanilla: `vanilla://vanillaObjectid`  
  This is a special one for specific structures,  
  as of 05/10/2025, it has the following:
  - roundhouseStall
  - roundhouseStart
  - roundhouseEnd
  - coalConveyor
  - coalTower
  - dieselFuelingStand
  - waterTower
  - waterColumn
  - flagStopStation
  - brysonDepot
  - dillsboroStation
  - southernCombinationDepot

## Telegraph poles

```json
{
  "handler": "AlinasMapMod.TelegraphPoleBuilder",
  "polesToRaise": [1,2,3]
}
```

```json
{
  "handler": "AlinasMapMod.TelegraphPoleMover",
  "polesToMove": [1,2,3],
  "poleMovement": [
    [0,1,2],
    [22,2,0],
    [33,3,5],
  ]
}

```
## Turntables

```json
{
  "handler":"AlinasMapMod.Turntable.TurntableBuilder",
  "radius": 15,
  "subdivisions": 32,
  "position": { "x": 0, "y": 0, "z": 0 },
  "rotation": { "x": 0, "y": 0, "z": 0 },
  "roundhouseStalls": 0,
  "roundhouseTrackLength": 46,
  "stallPrefab": "vanilla://roundhouseStall",
  "startPrefab": "vanilla://roundhouseStart",
  "endPrefab": "vanilla://roundhouseEnd"
}
```

## Loaders
```json
{
  "handler": "AlinasMapMod.LoaderBuilder",
  "position": { "x": 0, "y": 0, "z": 0 },
  "rotation": { "x": 0, "y": 0, "z": 0 },
  // Must be set to one of the coal, diesel, or water prefabs
  "prefab": "empty://", 
  // Required for coal and diesel, see example industry below
  "industry": "",
}
```

Example industry:
```json
{
  "loader-industry-example": {
    "name": "Example industry for loaders",
    "localPosition": { "x": -271.6577, "y": 0.0, "z": -22.8286133 },
    "usesContract": false,
    "components": {
      "coaling": {
        "type": "Model.Ops.IndustryUnloader",
        "name": "Example Coaling Tower",
        "trackSpans": [ "PExampleSpan" ],
        "carTypeFilter": "HM,HT",
        "sharedStorage": true,
        "loadId": "coal",
        "storageChangeRate": 0.0,
        "maxStorage": 300000.0,
        "orderAroundEmpties": false,
        "carTransferRate": 1E+07,
        "orderAroundLoaded": false
      },
      "diesel": {
        "type": "Model.Ops.IndustryUnloader",
        "name": "Example Diesel Stand",
        "trackSpans": [ "PExampleSpan" ],
        "carTypeFilter": "TM",
        "sharedStorage": true,
        "loadId": "diesel-fuel",
        "storageChangeRate": 0.0,
        "maxStorage": 16000.0,
        "orderAroundEmpties": false,
        "carTransferRate": 32000.0,
        "orderAroundLoaded": false
      }
    }
  }
}
```

## Passenger Station Agent (Includes building)

```json
{
  "handler": "AlinasMapMod.StationAgentBuilder",
  "position": {
    "x": 12886,
    "y": 562,
    "z": 4703
  },
  "rotation": {
    "x": 0,
    "y": 0,
    "z": 0
  },
  "prefab": "vanilla://flagStopStation",
  "passengerStop": "whittier"
}
```

## Passenger Stations

### <span style="color:red">***DEPRECATED: Use the industry component instead***


***NOTE***: There are known issues with pax stations not always showing in the Timetable, especially on branch lines. 

```json
  {
    "handler": "AlinasMapMod.PaxBuilder",
    "spanIds": [], // Spans for loading/unloading
    "industry": "", // Required, see example below
    "timetableCode": "", // Required
    // Reference values: Whittier: 30, Ela: 25, Bryson: 50
    "basePopulation": 40,
    // List of ids of other passenger stations.
    // Unsure of exact impact
    "neighborIds": []
  }
```

Industry:
```json
{
 "barkers": {
    "industries": {
      "barkers-station": {
        "name": "Barkers Station",
        "localPosition": { "x": 0, "y": 0, "z": 0},
        "usesContract": false,
        "components": {}
      }
    }
  }
}
```
