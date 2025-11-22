using System;
using System.Linq;
using AlinasMapMod.Definitions;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Validation;
using Map.Runtime.MapModifiers;
using Map.Runtime.MaskComponents;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms;
using StrangeCustoms.Tracks;
using UnityEngine;

namespace AlinasMapMod;

public abstract class SerializedMapMaskBase : IValidatable
{
    public string Id { get; set; }
    public SerializedVector3 Position { get; set; }
    public float Radius { get; set; } = 10f;
    public float Falloff { get; set; } = 10f;
    public bool EnableSetHeight { get; set; }
    public bool EnableCutTrees { get; set; }
    public bool EnableMaskModifier { get; set; }
    public MaskName MaskName { get; set; } = MaskName.Object;
    public int Order { get; set; }
    public void ApplyTo(MapMaskBase mask)
    {
        mask.transform.position = Position;
        mask.radius = Radius;
        mask.falloff = Falloff;
        mask.enableSetHeight = EnableSetHeight;
        mask.enableCutTrees = EnableCutTrees;
        mask.enableMaskModifier = EnableMaskModifier;
        mask.maskName = MaskName;
        mask.order = Order;
    }
    
    public virtual void Validate()
    {
        var result = ValidateWithDetails();
        if (!result.IsValid)
        {
            var firstError = result.Errors.FirstOrDefault();
            throw new ValidationException(firstError?.Message ?? "Validation failed");
        }
    }
    
    public virtual ValidationResult ValidateWithDetails()
    {
        var combiner = new ValidationResultCombiner()
            .Add(new ValidationBuilder<float>(nameof(Radius))
                .GreaterThan(0f), Radius)
            .Add(new ValidationBuilder<float>(nameof(Falloff))
                .GreaterThanOrEqual(0f), Falloff)
            .Add(new ValidationBuilder<int>(nameof(MaskName))
                .AsValidEnum<MaskName>(), (int)MaskName);
      
        // Validate Position if it implements IValidatable
        if (Position is IValidatable validatablePosition)
        {
            combiner.Add(validatablePosition.ValidateWithDetails());
        }
      
        return combiner.Result;
    }
}

public class SerializedCircleMapMask : SerializedMapMaskBase
{
    // No additional validation needed beyond base class
}

public class SerializedRectangleMapMask : SerializedMapMaskBase
{
    public SerializedVector2 Size { get; set; } = new SerializedVector2() { x = 10f, y = 10f };
    public SerializedVector3 Rotation { get; set; } = new();

    public void ApplyTo(RectangleMapMask mask)
    {
        base.ApplyTo(mask);
        mask.sizeX = Size.x;
        mask.sizeZ = Size.y;
        mask.degrees = Rotation.y;
    }
    
    public override ValidationResult ValidateWithDetails()
    {
        var combiner = new ValidationResultCombiner()
            .Add(base.ValidateWithDetails())
            .Add(new ValidationBuilder<float>(nameof(Size) + ".x")
                .GreaterThan(0f), Size.x)
            .Add(new ValidationBuilder<float>(nameof(Size) + ".y")
                .GreaterThan(0f), Size.y);
      
        // Validate Rotation if it implements IValidatable
        if (Rotation is IValidatable validatableRotation)
        {
            combiner.Add(validatableRotation.ValidateWithDetails());
        }
      
        return combiner.Result;
    }
}

public class SerializedCurveMapMask : SerializedMapMaskBase
{
    public SerializedVector3 PositionA { get; set; } = new SerializedVector3() { z = -20f };
    public SerializedVector3 RotationA { get; set; } = new SerializedVector3();
    public SerializedVector3 PositionB { get; set; } = new SerializedVector3() { z = 20f };
    public SerializedVector3 RotationB { get; set; } = new SerializedVector3() { y = 180 };
    public float SizeA { get; set; } = 1;
    public float SizeB { get; set; } = 1;
    public float RadiusNoise { get; set; }
    public float NoiseScale { get; set; } = 1f;

    public void ApplyTo(CurveMapMask mask)
    {
        base.ApplyTo(mask);
        mask.positionA = PositionA;
        mask.rotationA = RotationA;
        mask.positionB = PositionB;
        mask.rotationB = RotationB;
        mask.sizeA = SizeA;
        mask.sizeB = SizeB;
        mask.radiusNoise = RadiusNoise;
        mask.noiseScale = NoiseScale;
    }
    
    public override ValidationResult ValidateWithDetails()
    {
        var combiner = new ValidationResultCombiner()
            .Add(base.ValidateWithDetails())
            .Add(new ValidationBuilder<float>(nameof(SizeA))
                .GreaterThan(0f), SizeA)
            .Add(new ValidationBuilder<float>(nameof(SizeB))
                .GreaterThan(0f), SizeB)
            .Add(new ValidationBuilder<float>(nameof(NoiseScale))
                .GreaterThan(0f), NoiseScale)
            .Add(new ValidationBuilder<float>(nameof(RadiusNoise))
                .GreaterThanOrEqual(0f), RadiusNoise);
      
        // Validate positions and rotations if they implement IValidatable
        if (PositionA is IValidatable validatablePositionA)
        {
            combiner.Add(validatablePositionA.ValidateWithDetails());
        }
        if (PositionB is IValidatable validatablePositionB)
        {
            combiner.Add(validatablePositionB.ValidateWithDetails());
        }
        if (RotationA is IValidatable validatableRotationA)
        {
            combiner.Add(validatableRotationA.ValidateWithDetails());
        }
        if (RotationB is IValidatable validatableRotationB)
        {
            combiner.Add(validatableRotationB.ValidateWithDetails());
        }
      
        return combiner.Result;
    }
}

[HarmonyLib.HarmonyPatchCategory("AlinasMapMod")]
internal class MapMaskBuilder : SplineyBuilderBase, IObjectFactory
{
    public string Name => "Map Mask";
    public bool Enabled => true;
    public Type ObjectType => typeof(MapMaskBase);
    
    public IEditableObject CreateObject(PatchEditor editor, string id)
    {
        // Map masks don't create editable objects in the map editor
        throw new NotSupportedException("Map masks cannot be created directly in the map editor");
    }

    //[HarmonyLib.HarmonyPatch(typeof(MapManager), "AddModifier")]
    //[HarmonyLib.HarmonyPostfix]
    //public static void PrefixAddModifier(string __result)
    //{
    //  Logger.Information($"Adding Map Mask Modifier {__result}");
    //}

    //[HarmonyLib.HarmonyPatch(typeof(MapManager), "RemoveModifier")]
    //[HarmonyLib.HarmonyPrefix]
    //public static void PrefixRemoveModifier(string modifierKey)
    //{
    //  Logger.Information($"Removing Map Mask Modifier {modifierKey}");
    //}

    protected override GameObject BuildSplineyInternal(string id, Transform parentTransform, JObject data)
    {
        var type = data["type"]?.ToString();
        Logger.Information($"Building {type} map mask {id}");
        var world = GameObject.Find("World");
        var parent = world.transform.Find("Spliney Map Masks")?.gameObject ?? new GameObject("Spliney Map Masks");
        parent.transform.parent = world.transform;
        parent.transform.localPosition = Vector3.zero;

        //var go = parent.transform.Find(id)?.gameObject ?? new GameObject(id);
        var go = new GameObject(id);
        go.name = id;
        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.SetActive(false);
        switch (type) {
            case "circle":
                BuildCircle(go, data, id);
                break;
            case "rectangle":
                BuildRectangle(go, data, id);
                break;
            case "curve":
                BuildCurve(go, data, id);
                break;
            default:
                throw new System.Exception($"Unknown type: {type}, valid types are: circle, curve, rectangle");
        }
        go.SetActive(true);
        //var comp = go.GetComponent<MapMaskBase>();
        //comp.Rebuild();
        //typeof(MapMaskBase).GetMethod("ApplyModifiers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(comp, null);
        //Logger.Debug("Built {0} with id {1} {2}", type, id, Utils.GetPathFromGameObject(go));
        //var pos = MapManager.Instance.TilePositionFromPoint(go.transform.position);
        return go;
    }

    private void BuildCurve(GameObject go, JObject data, string id)
    {
        var mask = go.GetComponent<CurveMapMask>() ?? go.AddComponent<CurveMapMask>();
        var serialized = DeserializeAndValidate<SerializedCurveMapMask>(data);
        serialized.ApplyTo(mask);
    }

    private void BuildRectangle(GameObject go, JObject data, string id)
    {
        var mask = go.GetComponent<RectangleMapMask>() ?? go.AddComponent<RectangleMapMask>();
        var serialized = DeserializeAndValidate<SerializedRectangleMapMask>(data);
        serialized.ApplyTo(mask);
    }

    private void BuildCircle(GameObject go, JObject data, string id)
    {
        var mask = go.GetComponent<CircleMapMask>() ?? go.AddComponent<CircleMapMask>();
        var serialized = DeserializeAndValidate<SerializedCircleMapMask>(data);
        serialized.ApplyTo(mask);
    }
}