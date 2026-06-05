using AlinasRailTools.Scripting;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;

class GameGraphImporter
{
    public void ImportFromJson(string jsonPath, string zoneName)
    {
        var json = File.ReadAllText(jsonPath);
        var data = JObject.Parse(json);

        var helper = new Helper(zoneName);

        // Store node references
        var nodeBuilders = new Dictionary<string, TrackNodeBuilder>();
        var externalNodes = new Dictionary<string, TrackNodeBuilder>();

        // Get nodes and segments, defaulting to empty if missing
        var nodes = data["tracks"]?["nodes"]?.ToObject<JObject>() ?? new JObject();
        var segments = data["tracks"]?["segments"]?.ToObject<JObject>() ?? new JObject();

        // Find external nodes (referenced in segments but not defined in nodes)
        var allNodeIds = new HashSet<string>(nodes.Properties().Select(p => p.Name));
        var referencedNodeIds = new HashSet<string>();

        foreach (var segment in segments.Properties())
        {
            var segData = segment.Value.ToObject<JObject>();
            var startId = segData["startId"]?.ToString();
            var endId = segData["endId"]?.ToString();

            if (!string.IsNullOrEmpty(startId)) referencedNodeIds.Add(startId);
            if (!string.IsNullOrEmpty(endId)) referencedNodeIds.Add(endId);
        }

        var externalNodeIds = referencedNodeIds.Except(allNodeIds).ToHashSet();

        // Get external node references first
        foreach (var externalId in externalNodeIds.OrderBy(x => x))
        {
            var nodeRef = helper.GetTrackNode(externalId);
            externalNodes[externalId] = nodeRef;
        }

        // Process nodes
        foreach (var nodeProp in nodes.Properties().OrderBy(p => p.Name))
        {
            var nodeId = nodeProp.Name;
            var nodeData = nodeProp.Value;

            // Special case: if node data is null, remove it if it exists
            if (nodeData == null || nodeData.Type == JTokenType.Null)
            {
                if (helper.TrackNodes.ContainsKey(nodeId))
                {
                    var nodeToRemove = helper.GetTrackNode(nodeId);
                    nodeToRemove.Remove();
                }
                continue;
            }

            var nodeObj = nodeData.ToObject<JObject>();
            TrackNodeBuilder nodeBuilder;

            // Check if this is an external node we already retrieved
            if (externalNodes.ContainsKey(nodeId))
            {
                nodeBuilder = externalNodes[nodeId];
            }
            else
            {
                nodeBuilder = helper.CreateTrackNode(nodeId);
            }

            // Always set position if specified
            var position = nodeObj["position"];
            if (position != null)
            {
                var x = position["x"]?.ToObject<float>() ?? 0f;
                var y = position["y"]?.ToObject<float>() ?? 0f;
                var z = position["z"]?.ToObject<float>() ?? 0f;
                nodeBuilder = nodeBuilder.At(x, y, z);
            }

            // Always set rotation if specified
            var rotation = nodeObj["rotation"];
            if (rotation != null)
            {
                var x = rotation["x"]?.ToObject<float>() ?? 0f;
                var y = rotation["y"]?.ToObject<float>() ?? 0f;
                var z = rotation["z"]?.ToObject<float>() ?? 0f;
                nodeBuilder = nodeBuilder.WithRotation(x, y, z);
            }

            // Always set flipSwitchStand if specified
            if (nodeObj["flipSwitchStand"] != null)
            {
                var flipSwitchStand = nodeObj["flipSwitchStand"].ToObject<bool>();
                nodeBuilder = nodeBuilder.WithFlipSwitchStand(flipSwitchStand);
            }

            nodeBuilders[nodeId] = nodeBuilder;
        }

        // Group segments by their groupId for efficient WithGroupId calls
        var segmentsByGroup = new Dictionary<string, List<(string segmentId, JToken segmentData)>>();

        foreach (var segmentProp in segments.Properties())
        {
            var segmentId = segmentProp.Name;
            var segmentData = segmentProp.Value;

            // Special case: if segment data is null, add to removal list
            if (segmentData == null || segmentData.Type == JTokenType.Null)
            {
                // Add to a special "remove" group
                if (!segmentsByGroup.ContainsKey("__REMOVE__"))
                    segmentsByGroup["__REMOVE__"] = new List<(string, JToken)>();
                segmentsByGroup["__REMOVE__"].Add((segmentId, segmentData));
                continue;
            }

            var segmentObj = segmentData.ToObject<JObject>();
            var groupId = segmentObj["groupId"]?.ToString() ?? "";

            if (!segmentsByGroup.ContainsKey(groupId))
                segmentsByGroup[groupId] = new List<(string, JToken)>();

            segmentsByGroup[groupId].Add((segmentId, segmentData));
        }

        // Process segments grouped by groupId
        foreach (var group in segmentsByGroup.OrderBy(kvp => kvp.Key))
        {
            var groupId = group.Key;
            var segmentsInGroup = group.Value;

            // Special case: handle removals
            if (groupId == "__REMOVE__")
            {
                foreach (var (segmentId, _) in segmentsInGroup)
                {
                    if (helper.TrackSegments.ContainsKey(segmentId))
                    {
                        var segmentToRemove = helper.GetTrackSegment(segmentId);
                        segmentToRemove.Remove();
                    }
                }
                continue;
            }

            // Set group if not empty
            if (!string.IsNullOrEmpty(groupId))
            {
                helper.WithGroupId(groupId);
            }

            foreach (var (segmentId, segmentData) in segmentsInGroup.OrderBy(x => x.segmentId))
            {
                var segmentObj = segmentData.ToObject<JObject>();
                var startId = segmentObj["startId"]?.ToString();
                var endId = segmentObj["endId"]?.ToString();

                // Skip segments with incomplete node references
                if (string.IsNullOrEmpty(startId) || string.IsNullOrEmpty(endId))
                    continue;

                TrackNodeBuilder startNodeBuilder = null;
                TrackNodeBuilder endNodeBuilder = null;

                // Get start node
                if (nodeBuilders.ContainsKey(startId))
                    startNodeBuilder = nodeBuilders[startId];
                else if (externalNodes.ContainsKey(startId))
                    startNodeBuilder = externalNodes[startId];

                // Get end node
                if (nodeBuilders.ContainsKey(endId))
                    endNodeBuilder = nodeBuilders[endId];
                else if (externalNodes.ContainsKey(endId))
                    endNodeBuilder = externalNodes[endId];

                if (startNodeBuilder == null || endNodeBuilder == null)
                {
                    Debug.LogWarning($"Segment {segmentId} references missing nodes: {startId}, {endId}");
                    continue;
                }

                var segmentBuilder = helper.CreateTrackSegment(segmentId, startNodeBuilder.Node, endNodeBuilder.Node);

                // Always set style if specified
                var style = segmentObj["style"]?.ToString();
                if (!string.IsNullOrEmpty(style) && System.Enum.TryParse<TrackSegment.Style>(style, out var styleEnum))
                {
                    segmentBuilder.Style = styleEnum;
                }

                // Always set priority if specified
                if (segmentObj["priority"] != null)
                {
                    var priority = segmentObj["priority"].ToObject<int>();
                    segmentBuilder.Priority = priority;
                }

                // GroupId is already set via helper.WithGroupId() above, but set it explicitly too
                if (!string.IsNullOrEmpty(groupId))
                {
                    segmentBuilder.GroupId = groupId;
                }
            }
        }

        Debug.Log($"Successfully imported track layout from {jsonPath}");
    }
}

// Example usage:
public void ImportWhittierYard()
{
    var importer = new GameGraphImporter();
    var jsonPath = "MapTool/dist/AMM_WhittierYard/game-graph.json";
    importer.ImportFromJson(jsonPath, "AN_Whittier_Yard");
}

// Execute the import
ImportWhittierYard();