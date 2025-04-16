# Pax stations

Industry:
```json
{
 "barkers": {
    "industries": {
      "barkers-station": {
        "name": "Barkers Station",
        "localPosition": { "x": 0, "y": 0, "z": 0},
        "usesContract": false,
        "components": {
          "ammBarkersStation": {
            "name": "Barkers Station",
            "type": "AlinasMapMod.PaxStationComponent",
            "timetableCode": "BC",
            // Reference values: Whittier: 30, Ela: 25, Bryson: 50
            "basePopulation": 10,
            "loadId": "passengers",
            "trackSpans": [ // Spans for loading/unloading
              "PAN_Test_Mod_00"
            ],
            // Future support for custom branches, currently supported is "Main" and "Alarka Branch"
            "branch": "Main",
            // List of ids of other passenger stations.
            // Unsure of exact impact
            "neighborIds": [],
            "carTypeFilter": "*",
            "sharedStorage": true
          }
        }
      }
    }
  }
}
```