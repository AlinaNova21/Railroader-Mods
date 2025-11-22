# Pax stations

This allows creating custom stations:

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
            // Which branch this stations belongs to:
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

## Branches

A station can be in multiple branches at the same time. E.g. Alarka Jct is in the Main branch and the Alarka branch.

You can specifiy this by separating branche names with a ':':
```json
"branch": "Main:My Custom Branch"
```

This will create a custom branch called "My Custom Branch" that connects to the Main branch at this station.


