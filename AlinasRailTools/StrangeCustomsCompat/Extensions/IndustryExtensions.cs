using System.Collections.Generic;
using AlinasRailTools.Scripting;
using AlinasRailTools.Scripting.Ops;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AlinasRailTools.StrangeCustomsCompat.Extensions;

/// <summary>
/// StrangeCustoms JSON extension methods for IndustryBuilder
/// </summary>
public static class IndustryExtensions
{
    /// <summary>
    /// Apply StrangeCustoms JSON data to an IndustryBuilder
    /// </summary>
    public static IndustryBuilder ApplySCJSON(this IndustryBuilder builder, string id, JObject data)
    {
        UnityEngine.Debug.Log($"SCCompat: Applying industry '{id}'");

        // Name
        var name = SCCompatHelpers.ParseString(data["name"]);
        if (!string.IsNullOrEmpty(name))
        {
            builder.WithName(name);
        }

        // Local Position
        var localPosition = SCCompatHelpers.ParseVector3(data["localPosition"]);
        if (localPosition != Vector3.zero)
        {
            builder.At(localPosition);
        }

        // Uses Contract
        var usesContract = SCCompatHelpers.ParseBool(data["usesContract"]);
        if (usesContract.HasValue)
        {
            builder.WithUsesContract(usesContract.Value);
        }

        builder.SetActive(true);

        // Apply Components
        var components = data["components"]?.ToObject<JObject>();
        if (components != null)
        {
            foreach (var component in components)
            {
                var componentId = component.Key;
                var componentData = component.Value?.ToObject<JObject>();
                if (componentData != null)
                {
                    ApplyIndustryComponent(builder, componentId, componentData);
                }
            }
        }

        return builder;
    }

    /// <summary>
    /// Apply industry component data based on component type
    /// </summary>
    private static void ApplyIndustryComponent(IndustryBuilder builder, string componentId, JObject componentData)
    {
        var componentType = SCCompatHelpers.ParseString(componentData["type"]);
        if (string.IsNullOrEmpty(componentType))
        {
            UnityEngine.Debug.LogWarning($"SCCompat: Component '{componentId}' has no type specified");
            return;
        }

        // Auto-replace Model.OpsNew with Model.Ops for compatibility
        var originalType = componentType;
        componentType = componentType.Replace("Model.OpsNew.", "Model.Ops.");
        if (originalType != componentType)
        {
            UnityEngine.Debug.Log($"SCCompat: Replaced component type '{originalType}' -> '{componentType}'");
        }

        UnityEngine.Debug.Log($"SCCompat: Applying component '{componentId}' of type '{componentType}'");

        switch (componentType)
        {
            case "Model.Ops.RepairTrack":
                ApplyRepairTrack(builder.CreateRepairTrack(componentId), componentData);
                break;

            case "Model.Ops.IndustryLoader":
                ApplyIndustryLoader(builder.CreateLoader(componentId), componentData);
                break;

            case "Model.Ops.IndustryUnloader":
                ApplyIndustryUnloader(builder.CreateUnloader(componentId), componentData);
                break;

            case "Model.Ops.Interchange":
                ApplyInterchange(builder.CreateInterchange(componentId), componentData);
                break;

            case "Model.Ops.InterchangedIndustryLoader":
                ApplyInterchangedLoader(builder.CreateInterchangedLoader(componentId), componentData);
                break;

            case "Model.Ops.LoadExporter":
                ApplyLoadExporter(builder.CreateExporter(componentId), componentData);
                break;

            case "Model.Ops.LoadImporter":
                ApplyLoadImporter(builder.CreateImporter(componentId), componentData);
                break;

            case "Model.Ops.FormulaicIndustryComponent":
                ApplyFormulaicComponent(builder.CreateFormulaic(componentId), componentData);
                break;

            case "Model.Ops.ProgressionIndustryComponent":
                ApplyProgressionComponent(builder.CreateProgression(componentId), componentData);
                break;

            case "Model.Ops.TeamTrack":
                ApplyTeamTrack(builder.CreateTeamTrack(componentId), componentData);
                break;

            case "Model.Ops.TeleportLoadingIndustry":
                ApplyTeleportLoader(builder.CreateTeleportLoader(componentId), componentData);
                break;

            case "Model.Ops.PassengerStop":
            case "AlinasMapMod.PaxStationComponent":
                ApplyPassengerStop(builder.CreatePassengerStop(componentId), componentData);
                break;

            default:
                UnityEngine.Debug.LogWarning($"SCCompat: Unknown component type '{componentType}' for component '{componentId}'");
                break;
        }
    }

    // Component-specific apply methods

    // Helper to apply base IndustryComponent properties (trackSpans, carTypeFilter, etc.)
    private static void ApplyBaseComponentProperties<TBuilder>(BaseIndustryComponentBuilder<TBuilder> builder, JObject data)
        where TBuilder : BaseIndustryComponentBuilder<TBuilder>
    {
        // Track spans
        var trackSpansArray = data["trackSpans"]?.ToObject<JArray>();
        if (trackSpansArray != null)
        {
            var trackSpans = new List<Track.TrackSpan>();
            foreach (var spanId in trackSpansArray)
            {
                var id = SCCompatHelpers.ParseString(spanId);
                if (!string.IsNullOrEmpty(id))
                {
                    var span = builder.Helper.Graph.GetTrackSpan(id).Span;
                    trackSpans.Add(span);
                }
            }
            if (trackSpans.Count > 0)
            {
                builder.WithTrackSpans(trackSpans.ToArray());
            }
        }

        // Car type filter
        var carTypeFilter = SCCompatHelpers.ParseString(data["carTypeFilter"]);
        if (!string.IsNullOrEmpty(carTypeFilter))
        {
            builder.WithCarTypeFilter(carTypeFilter);
        }

        // Shared storage
        var sharedStorage = SCCompatHelpers.ParseBool(data["sharedStorage"]);
        if (sharedStorage.HasValue)
        {
            builder.WithSharedStorage(sharedStorage.Value);
        }

        // Name
        var name = SCCompatHelpers.ParseString(data["name"]);
        if (!string.IsNullOrEmpty(name))
        {
            builder.WithName(name);
        }
    }

    private static void ApplyRepairTrack(RepairTrackBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);

        var canOverhaul = SCCompatHelpers.ParseBool(data["canOverhaul"]);
        if (canOverhaul.HasValue)
        {
            builder.WithCanOverhaul(canOverhaul.Value);
        }

        var repairPartsLoad = SCCompatHelpers.ParseString(data["repairPartsLoad"]);
        if (!string.IsNullOrEmpty(repairPartsLoad))
        {
            var load = builder.Helper.Ops.GetLoad(repairPartsLoad).Load;
            builder.WithRepairPartsLoad(load);
        }

        builder.SetActive(true);
    }

    private static void ApplyIndustryLoader(IndustryLoaderBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);
        ApplyLoaderBase(builder, data);

        var carLoadRate = SCCompatHelpers.ParseFloat(data["carLoadRate"]);
        if (carLoadRate.HasValue)
        {
            builder.WithCarLoadRate(carLoadRate.Value);
        }

        var orderAwayLoaded = SCCompatHelpers.ParseBool(data["orderAwayLoaded"]);
        if (orderAwayLoaded.HasValue)
        {
            builder.WithOrderAwayLoaded(orderAwayLoaded.Value);
        }

        builder.SetActive(true);
    }

    private static void ApplyIndustryUnloader(IndustryUnloaderBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);
        ApplyUnloaderBase(builder, data);

        var carUnloadRate = SCCompatHelpers.ParseFloat(data["carUnloadRate"]);
        if (carUnloadRate.HasValue)
        {
            builder.WithCarUnloadRate(carUnloadRate.Value);
        }

        builder.SetActive(true);
    }

    private static void ApplyInterchange(InterchangeBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);
        // Interchange has no specific properties in the builder currently
        builder.SetActive(true);
    }

    private static void ApplyInterchangedLoader(InterchangedIndustryLoaderBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);
        ApplyInterchangedLoaderBase(builder, data);
        // Interchange is automatically populated - finds matching Interchange on same industry
        builder.SetActive(true);
    }

    private static void ApplyLoadExporter(LoadExporterBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);

        var load = SCCompatHelpers.ParseString(data["load"]);
        if (!string.IsNullOrEmpty(load))
        {
            var loadObj = builder.Helper.Ops.GetLoad(load).Load;
            builder.WithLoad(loadObj);
        }

        var minimumLoad = SCCompatHelpers.ParseFloat(data["minimumLoad"]);
        if (minimumLoad.HasValue)
        {
            builder.WithMinimumLoad(minimumLoad.Value);
        }

        builder.SetActive(true);
    }

    private static void ApplyLoadImporter(LoadImporterBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);

        var load = SCCompatHelpers.ParseString(data["load"]);
        if (!string.IsNullOrEmpty(load))
        {
            var loadObj = builder.Helper.Ops.GetLoad(load).Load;
            builder.WithLoad(loadObj);
        }

        var desiredCarCount = SCCompatHelpers.ParseInt(data["desiredCarCount"]);
        if (desiredCarCount.HasValue)
        {
            builder.WithDesiredCarCount(desiredCarCount.Value);
        }

        var maxOrderCount = SCCompatHelpers.ParseInt(data["maxOrderCount"]);
        if (maxOrderCount.HasValue)
        {
            builder.WithMaxOrderCount(maxOrderCount.Value);
        }

        // Forward to component reference
        var forwardToId = SCCompatHelpers.ParseString(data["forwardToOnArrival"]);
        if (!string.IsNullOrEmpty(forwardToId) && builder.Done().Components.TryGetValue(forwardToId, out var component))
        {
            builder.WithForwardToOnArrival(component);
        }

        builder.SetActive(true);
    }

    private static void ApplyFormulaicComponent(FormulaicIndustryComponentBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);

        // Parse input terms
        var inputs = data["inputs"]?.ToObject<JArray>();
        if (inputs != null)
        {
            builder.ClearInputTerms();
            foreach (var input in inputs)
            {
                var loadId = SCCompatHelpers.ParseString(input["load"]);
                var unitsPerDay = SCCompatHelpers.ParseFloat(input["unitsPerDay"]) ?? 1f;
                if (!string.IsNullOrEmpty(loadId))
                {
                    var load = builder.Helper.Ops.GetLoad(loadId).Load;
                    builder.AddInputTerm(load, unitsPerDay);
                }
            }
        }

        // Parse output terms
        var outputs = data["outputs"]?.ToObject<JArray>();
        if (outputs != null)
        {
            builder.ClearOutputTerms();
            foreach (var output in outputs)
            {
                var loadId = SCCompatHelpers.ParseString(output["load"]);
                var unitsPerDay = SCCompatHelpers.ParseFloat(output["unitsPerDay"]) ?? 1f;
                if (!string.IsNullOrEmpty(loadId))
                {
                    var load = builder.Helper.Ops.GetLoad(loadId).Load;
                    builder.AddOutputTerm(load, unitsPerDay);
                }
            }
        }

        builder.SetActive(true);
    }

    private static void ApplyProgressionComponent(ProgressionIndustryComponentBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);
        // ProgressionIndustryComponent has no additional properties beyond base IndustryComponent
        // NOTE: Do not enable by default - part of not-yet-implemented progression system
    }

    private static void ApplyTeamTrack(TeamTrackBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);

        // TeamTrackProfile reference
        var profileId = SCCompatHelpers.ParseString(data["profile"]);
        if (!string.IsNullOrEmpty(profileId) && builder.Done().TeamTrackProfiles.TryGetValue(profileId, out var profile))
        {
            builder.WithProfile(profile);
        }

        var idealCars = SCCompatHelpers.ParseFloat(data["idealCars"]);
        if (idealCars.HasValue)
        {
            builder.WithIdealCars(idealCars.Value);
        }

        builder.SetActive(true);
    }

    private static void ApplyTeleportLoader(TeleportLoadingIndustryBuilder builder, JObject data)
    {
        ApplyBaseComponentProperties(builder, data);
        ApplyLoaderBase(builder, data);

        var carLoadPeriod = SCCompatHelpers.ParseFloat(data["carLoadPeriod"]);
        if (carLoadPeriod.HasValue)
        {
            builder.WithCarLoadPeriod(carLoadPeriod.Value);
        }

        var carLengthFeet = SCCompatHelpers.ParseFloat(data["carLengthFeet"]);
        if (carLengthFeet.HasValue)
        {
            builder.WithCarLengthFeet(carLengthFeet.Value);
        }

        // Input spans
        var inputSpansArray = data["inputSpans"]?.ToObject<JArray>();
        if (inputSpansArray != null)
        {
            var inputSpans = new List<Track.TrackSpan>();
            foreach (var spanId in inputSpansArray)
            {
                var id = SCCompatHelpers.ParseString(spanId);
                if (!string.IsNullOrEmpty(id))
                {
                    var span = builder.Helper.Graph.GetTrackSpan(id).Span;
                    inputSpans.Add(span);
                }
            }
            if (inputSpans.Count > 0)
            {
                builder.WithInputSpans(inputSpans.ToArray());
            }
        }

        // Output spans
        var outputSpansArray = data["outputSpans"]?.ToObject<JArray>();
        if (outputSpansArray != null)
        {
            var outputSpans = new List<Track.TrackSpan>();
            foreach (var spanId in outputSpansArray)
            {
                var id = SCCompatHelpers.ParseString(spanId);
                if (!string.IsNullOrEmpty(id))
                {
                    var span = builder.Helper.Graph.GetTrackSpan(id).Span;
                    outputSpans.Add(span);
                }
            }
            if (outputSpans.Count > 0)
            {
                builder.WithOutputSpans(outputSpans.ToArray());
            }
        }

        builder.SetActive(true);
    }

    private static void ApplyPassengerStop(PassengerStopBuilder builder, JObject data)
    {
        var timetableCode = SCCompatHelpers.ParseString(data["timetableCode"]);
        if (!string.IsNullOrEmpty(timetableCode))
        {
            builder.WithTimetableCode(timetableCode);
        }

        var basePopulation = SCCompatHelpers.ParseInt(data["basePopulation"]);
        if (basePopulation.HasValue)
        {
            builder.WithBasePopulation(basePopulation.Value);
        }

        var flagStop = SCCompatHelpers.ParseBool(data["flagStop"]);
        if (flagStop.HasValue)
        {
            builder.WithFlagStop(flagStop.Value);
        }

        var name = SCCompatHelpers.ParseString(data["name"]);
        if (!string.IsNullOrEmpty(name))
        {
            builder.WithName(name);
        }

        // Load - check both "loadId" (AMM) and "passengerLoad" (ART) field names
        var loadId = SCCompatHelpers.ParseString(data["loadId"]) ?? SCCompatHelpers.ParseString(data["passengerLoad"]);
        if (!string.IsNullOrEmpty(loadId))
        {
            var load = builder.Helper.Ops.GetLoad(loadId).Load;
            builder.WithPassengerLoad(load);
        }

        // Neighbors - check both "neighborIds" (AMM) and "neighbors" (ART) field names
        var neighborsArray = data["neighborIds"]?.ToObject<JArray>() ?? data["neighbors"]?.ToObject<JArray>();
        if (neighborsArray != null)
        {
            var neighbors = new List<Model.Ops.PassengerStop>();
            foreach (var neighborId in neighborsArray)
            {
                var id = SCCompatHelpers.ParseString(neighborId);
                if (!string.IsNullOrEmpty(id) && builder.Done().PassengerStops.TryGetValue(id, out var neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }
            if (neighbors.Count > 0)
            {
                builder.WithNeighbors(neighbors.ToArray());
            }
        }

        // Track spans
        var trackSpansArray = data["trackSpans"]?.ToObject<JArray>();
        if (trackSpansArray != null)
        {
            var trackSpans = new List<Track.TrackSpan>();
            foreach (var spanId in trackSpansArray)
            {
                var id = SCCompatHelpers.ParseString(spanId);
                if (!string.IsNullOrEmpty(id))
                {
                    var span = builder.Helper.Graph.GetTrackSpan(id).Span;
                    trackSpans.Add(span);
                }
            }
            if (trackSpans.Count > 0)
            {
                builder.WithTrackSpans(trackSpans.ToArray());
            }
        }

        builder.SetActive(true);

        // Note: "branch" field from AMM is for TimetableController branching, not a PassengerStop property
        // This would need to be handled at a higher level if needed
    }

    // Helper for common IndustryLoaderBase properties
    private static void ApplyLoaderBase(IndustryLoaderBaseBuilder<IndustryLoaderBuilder> builder, JObject data)
    {
        var load = SCCompatHelpers.ParseString(data["load"]);
        if (!string.IsNullOrEmpty(load))
        {
            var loadObj = builder.Helper.Ops.GetLoad(load).Load;
            builder.WithLoad(loadObj);
        }

        var productionRate = SCCompatHelpers.ParseFloat(data["productionRate"]);
        if (productionRate.HasValue)
        {
            builder.WithProductionRate(productionRate.Value);
        }

        var maxStorage = SCCompatHelpers.ParseFloat(data["maxStorage"]);
        if (maxStorage.HasValue)
        {
            builder.WithMaxStorage(maxStorage.Value);
        }

        var orderEmpties = SCCompatHelpers.ParseBool(data["orderEmpties"]);
        if (orderEmpties.HasValue)
        {
            builder.WithOrderEmpties(orderEmpties.Value);
        }
    }

    private static void ApplyUnloaderBase(IndustryUnloaderBuilder builder, JObject data)
    {
        var load = SCCompatHelpers.ParseString(data["load"]);
        if (!string.IsNullOrEmpty(load))
        {
            var loadObj = builder.Helper.Ops.GetLoad(load).Load;
            builder.WithLoad(loadObj);
        }

        var storageConsumptionRate = SCCompatHelpers.ParseFloat(data["storageConsumptionRate"])
                                     ?? SCCompatHelpers.ParseFloat(data["productionRate"]);
        if (storageConsumptionRate.HasValue)
        {
            builder.WithStorageConsumptionRate(storageConsumptionRate.Value);
        }

        var maxStorage = SCCompatHelpers.ParseFloat(data["maxStorage"]);
        if (maxStorage.HasValue)
        {
            builder.WithMaxStorage(maxStorage.Value);
        }

        var orderLoads = SCCompatHelpers.ParseBool(data["orderLoads"])
                        ?? SCCompatHelpers.ParseBool(data["orderEmpties"]);
        if (orderLoads.HasValue)
        {
            builder.WithOrderLoads(orderLoads.Value);
        }

        var orderAwayEmpties = SCCompatHelpers.ParseBool(data["orderAwayEmpties"]);
        if (orderAwayEmpties.HasValue)
        {
            builder.WithOrderAwayEmpties(orderAwayEmpties.Value);
        }
    }

    private static void ApplyInterchangedLoaderBase(InterchangedIndustryLoaderBuilder builder, JObject data)
    {
        var load = SCCompatHelpers.ParseString(data["load"]);
        if (!string.IsNullOrEmpty(load))
        {
            var loadObj = builder.Helper.Ops.GetLoad(load).Load;
            builder.WithLoad(loadObj);
        }
    }

    private static void ApplyLoaderBase(IndustryLoaderBaseBuilder<TeleportLoadingIndustryBuilder> builder, JObject data)
    {
        var load = SCCompatHelpers.ParseString(data["load"]);
        if (!string.IsNullOrEmpty(load))
        {
            var loadObj = builder.Helper.Ops.GetLoad(load).Load;
            builder.WithLoad(loadObj);
        }

        var productionRate = SCCompatHelpers.ParseFloat(data["productionRate"]);
        if (productionRate.HasValue)
        {
            builder.WithProductionRate(productionRate.Value);
        }

        var maxStorage = SCCompatHelpers.ParseFloat(data["maxStorage"]);
        if (maxStorage.HasValue)
        {
            builder.WithMaxStorage(maxStorage.Value);
        }

        var orderEmpties = SCCompatHelpers.ParseBool(data["orderEmpties"]);
        if (orderEmpties.HasValue)
        {
            builder.WithOrderEmpties(orderEmpties.Value);
        }
    }
}
