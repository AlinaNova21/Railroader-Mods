# Progressions

```json
// progressions json format
{
  "mapFeatures": {
    "sampleMapFeature": {
      "displayName": "Sample MapFeature",
      "name": "Sample MapFeature",
      "description": "Description",
      "prerequisites": {
        "anotherMapFeature": true
      },
      "areasEnableOnUnlock": {
        "sampleArea": true
      },
      "defaultEnableInSandbox": false,
      "gameObjectsEnableOnUnlock": {
        // Existing object in world, primary here for dumps.
        "path://scene/world/path/to/gameObject/in/tree": true,
        // Requires scenery to be defined in a game-graph
        "scenery://sampleSceneryId": true
      },
      "trackGroupsAvailableOnUnlock": {
        "sampleGroup": true
      },
      "trackGroupsEnableOnUnlock": {
        "sampleGroup": true
      },
      "unlockExcludeIndustries": {
        "sampleIndustry": true
      },
      "unlockIncludeIndustries": {
        // Includes components by default
        "sampleIndustry": true
      },
      "unlockIncludeIndustryComponents": {
        "sampleIndustryComponent": true
      }
    }
  },
  "progressions": {
    "ewh": { // ewh is the only current progression tree.
      "sections": {
        "sampleSection": {
          "displayName": "sample milestone",
          "description": "Description here",
          "prerequisiteSections": {
            "anotherSampleSection": true
          },
          "deliveryPhases": [
            // Can have as many phases as you want here.
            {
              "cost": 1234,
              "industryComponent": "sampleIndustryId.componentId",
              "deliveries": [
                // Can be empty for cost only milestones.
                {
                  "carTypeFilter": "GB*",
                  "count": 8,
                  "load": "ballast",
                  "direction": 0 // 0 = LoadToIndustry, 1 = LoadFromIndustry
                },
                {
                  "carTypeFilter": "GB*",
                  "count": 12,
                  "load": "gravel",
                  "direction": 0 // 0 = LoadToIndustry, 1 = LoadFromIndustry
                }
              ]
            }
          ],
          // Important note: You cannot both disable and enable the same feature, not even in seperate sections.
          "disableFeaturesOnUnlock": {
            "sampleMapFeature": true
          },
          "enableFeaturesOnUnlock": {
            "sampleMapFeature": true
          },
          "enableFeaturesOnAvailable": {
            "sampleMapFeature": true
          }
        }
      }
    }
  }   
}   
```