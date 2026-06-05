using System;
using System.IO;
using AlinasRailTools.Runtime;
using AlinasRailTools.Scripting;
using AlinasRailTools.Scripting.Graph;
using AlinasRailTools.StrangeCustomsCompat.Extensions;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat;

/// <summary>
/// Processes StrangeCustoms game-graph JSON files and applies them using ART builders
/// </summary>
public class GameGraphProcessor
{
    private readonly Helper helper;

    public GameGraphProcessor(Helper helper)
    {
        this.helper = helper;
    }

    /// <summary>
    /// Process a game graph from a file path
    /// </summary>
    public void ProcessGameGraph(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new System.IO.FileNotFoundException($"Game graph file not found: {filePath}");
        }

        var jsonText = File.ReadAllText(filePath);
        var data = JObject.Parse(jsonText);
        ProcessGameGraph(data);
    }

    /// <summary>
    /// Process a game graph from VFS
    /// </summary>
    public void ProcessGameGraphFromVFS(VirtualFileSystem vfs, string modId, string filename)
    {
        var vfsPath = $"sc-mods/{modId}/game-graph/{filename}";
        var jsonText = vfs.ReadFile(vfsPath);

        if (string.IsNullOrEmpty(jsonText))
        {
            throw new Runtime.FileNotFoundException($"Game graph file not found in VFS: {vfsPath}");
        }

        var data = JObject.Parse(jsonText);
        ProcessGameGraph(data);
    }

    /// <summary>
    /// Process a game graph from a JObject
    /// </summary>
    public void ProcessGameGraph(JObject graphData)
    {
        Debug.Log("GameGraphProcessor: Processing game graph data");

        // Process objects in correct dependency order
        ProcessTrackNodes(graphData);
        ProcessTrackSegments(graphData);
        ProcessTrackSpans(graphData);
        ProcessLoads(graphData);
        ProcessAreas(graphData);
        ProcessScenery(graphData);
        ProcessSplineys(graphData);

        Debug.Log("GameGraphProcessor: Finished processing game graph data");
    }

    private void ProcessTrackNodes(JObject graphData)
    {
        var objects = ExtractObjects(graphData, "tracks", "nodes");
        if (objects == null) return;

        Debug.Log($"GameGraphProcessor: Processing {objects.Count} TrackNodes");

        foreach (var objProp in objects.Properties())
        {
            var objectId = objProp.Name;
            var objectData = objProp.Value?.ToObject<JObject>();

            if (objectData == null)
            {
                // Removal
                Debug.Log($"GameGraphProcessor: Removing TrackNode '{objectId}'");
                helper.Graph.RemoveTrackNode(objectId);
            }
            else
            {
                // Creation/Modification
                Debug.Log($"GameGraphProcessor: Applying TrackNode '{objectId}'");
                helper.Graph.TrackNode(objectId).ApplySCJSON(objectId, objectData);
            }
        }
    }

    private void ProcessTrackSegments(JObject graphData)
    {
        var objects = ExtractObjects(graphData, "tracks", "segments");
        if (objects == null) return;

        Debug.Log($"GameGraphProcessor: Processing {objects.Count} TrackSegments");

        foreach (var objProp in objects.Properties())
        {
            var objectId = objProp.Name;
            var objectData = objProp.Value?.ToObject<JObject>();

            if (objectData == null)
            {
                // Removal
                Debug.Log($"GameGraphProcessor: Removing TrackSegment '{objectId}'");
                helper.Graph.RemoveTrackSegment(objectId);
            }
            else
            {
                // Creation/Modification
                Debug.Log($"GameGraphProcessor: Applying TrackSegment '{objectId}'");

                var startId = objectData["startId"]?.ToString();
                var endId = objectData["endId"]?.ToString();

                TrackSegmentBuilder segment;
                if (!string.IsNullOrEmpty(startId) && !string.IsNullOrEmpty(endId))
                {
                    // Full record - can create or get
                    var startNode = helper.Graph.TrackNode(startId).Node;
                    var endNode = helper.Graph.TrackNode(endId).Node;
                    segment = helper.Graph.GetOrCreateTrackSegment(objectId, startNode, endNode);
                }
                else
                {
                    // Partial record - can only get existing segment
                    segment = helper.Graph.GetTrackSegment(objectId);
                }

                segment.ApplySCJSON(objectId, objectData);
            }
        }
    }

    private void ProcessTrackSpans(JObject graphData)
    {
        var objects = ExtractObjects(graphData, "tracks", "spans");
        if (objects == null) return;

        Debug.Log($"GameGraphProcessor: Processing {objects.Count} TrackSpans");

        foreach (var objProp in objects.Properties())
        {
            var objectId = objProp.Name;
            var objectData = objProp.Value?.ToObject<JObject>();

            if (objectData == null)
            {
                // Removal
                Debug.Log($"GameGraphProcessor: Removing TrackSpan '{objectId}'");
                helper.Graph.RemoveTrackSpan(objectId);
            }
            else
            {
                // Creation/Modification
                Debug.Log($"GameGraphProcessor: Applying TrackSpan '{objectId}'");
                helper.Graph.TrackSpan(objectId).ApplySCJSON(objectId, objectData);
            }
        }
    }

    private void ProcessLoads(JObject graphData)
    {
        var objects = ExtractObjects(graphData, null, "loads");
        if (objects == null) return;

        Debug.Log($"GameGraphProcessor: Processing {objects.Count} Loads");

        foreach (var objProp in objects.Properties())
        {
            var objectId = objProp.Name;
            var objectData = objProp.Value?.ToObject<JObject>();

            if (objectData == null)
            {
                // Removal
                Debug.Log($"GameGraphProcessor: Removing Load '{objectId}'");
                helper.Ops.RemoveLoad(objectId);
            }
            else
            {
                // Creation/Modification
                Debug.Log($"GameGraphProcessor: Applying Load '{objectId}'");
                helper.Ops.Load(objectId).ApplySCJSON(objectId, objectData);
            }
        }
    }

    private void ProcessAreas(JObject graphData)
    {
        var objects = ExtractObjects(graphData, null, "areas");
        if (objects == null) return;

        Debug.Log($"GameGraphProcessor: Processing {objects.Count} Areas");

        foreach (var objProp in objects.Properties())
        {
            var objectId = objProp.Name;
            var objectData = objProp.Value?.ToObject<JObject>();

            if (objectData == null)
            {
                // Removal
                Debug.Log($"GameGraphProcessor: Removing Area '{objectId}'");
                helper.Ops.RemoveArea(objectId);
            }
            else
            {
                // Creation/Modification
                Debug.Log($"GameGraphProcessor: Applying Area '{objectId}'");
                helper.Ops.Area(objectId).ApplySCJSON(objectId, objectData);
            }
        }
    }

    private void ProcessScenery(JObject graphData)
    {
        var objects = ExtractObjects(graphData, null, "scenery");
        if (objects == null) return;

        Debug.Log($"GameGraphProcessor: Processing {objects.Count} Scenery");

        foreach (var objProp in objects.Properties())
        {
            var objectId = objProp.Name;
            var objectData = objProp.Value?.ToObject<JObject>();

            if (objectData == null)
            {
                // Removal
                Debug.Log($"GameGraphProcessor: Removing Scenery '{objectId}'");
                helper.Scenery.RemoveScenery(objectId);
            }
            else
            {
                // Creation/Modification
                Debug.Log($"GameGraphProcessor: Applying Scenery '{objectId}'");
                helper.Scenery.Scenery(objectId).ApplySCJSON(objectId, objectData);
            }
        }
    }

    private void ProcessSplineys(JObject graphData)
    {
        var objects = ExtractObjects(graphData, null, "splineys");
        if (objects == null) return;

        Debug.Log($"GameGraphProcessor: Processing {objects.Count} Splineys");

        foreach (var objProp in objects.Properties())
        {
            var objectId = objProp.Name;
            var objectData = objProp.Value?.ToObject<JObject>();

            if (objectData == null)
            {
                // Removal - determine type and remove accordingly
                Debug.Log($"GameGraphProcessor: Removing Spliney '{objectId}'");
                // Try removing as loader, turntable, roundhouse, or river
                helper.Loaders.RemoveLoader(objectId);
                helper.Turntables.RemoveTurntable(objectId);
                helper.Roundhouses.RemoveRoundhouse(objectId);
                helper.Rivers.RemoveRiver(objectId);
            }
            else
            {
                // Check handler to determine spliney type
                var handler = objectData["handler"]?.ToString();

                if (handler == "AlinasMapMod.LoaderBuilder" || handler == "AlinasMapMod.Loaders.LoaderBuilder")
                {
                    Debug.Log($"GameGraphProcessor: Applying Loader '{objectId}'");
                    helper.Loaders.Loader(objectId).ApplySCJSON(objectId, objectData);
                }
                else if (handler == "AlinasMapMod.TurntableBuilder" || handler == "AlinasMapMod.Turntable.TurntableBuilder")
                {
                    Debug.Log($"GameGraphProcessor: Applying Turntable '{objectId}'");

                    // Build turntable first
                    var turntableBuilder = helper.Turntables.Turntable(objectId);
                    turntableBuilder.ApplySCJSON(objectId, objectData);

                    // Check if roundhouse data exists
                    var hasRoundhouseStalls = objectData["RoundhouseStalls"] != null || objectData["roundhouseStalls"] != null;
                    if (hasRoundhouseStalls)
                    {
                        Debug.Log($"GameGraphProcessor: Applying Roundhouse for '{objectId}'");

                        // Set turntable reference directly, then apply JSON
                        var roundhouseBuilder = helper.Roundhouses.Roundhouse(objectId + ".roundhouse");
                        roundhouseBuilder.WithTurntable(turntableBuilder.Turntable);
                        roundhouseBuilder.ApplySCJSON(objectId, objectData);
                    }
                }
                else if (handler == "StrangeCustoms.FlowyThingBuilder")
                {
                    Debug.Log($"GameGraphProcessor: Applying RiverPath '{objectId}'");
                    helper.Rivers.River(objectId).ApplySCJSON(objectId, objectData);
                }
                else
                {
                    Debug.Log($"GameGraphProcessor: Unknown spliney handler '{handler}' for '{objectId}'");
                }
            }
        }
    }

    /// <summary>
    /// Extract objects from the game graph data
    /// </summary>
    private JObject ExtractObjects(JObject data, string parentKey, string key)
    {
        if (parentKey != null)
        {
            return data[parentKey]?[key]?.ToObject<JObject>();
        }
        else
        {
            return data[key]?.ToObject<JObject>();
        }
    }
}
