using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Game.Progression;
using Game.State;
using HarmonyLib;
using KeyValue.Runtime;
using Map.Runtime;
using Serilog;
using UI.Builder;
using UI.Menu;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AlinasMapMod.Map;

[HarmonyPatchCategory("AlinasMapMod")]
internal class Patches
{
      private static Serilog.ILogger logger = Log.ForContext(typeof(Patches));

      #region Map Tiles

      // MaxHeight covers full blue channel range: 500 + (127 * 100) + 1000 = 14200m
      // Min height: 500 + (-128 * 100) = -12300m
      // Total range: -12300m to 14200m (26.5km vertical)
      internal const float MaxHeight = 14200f;

      // FlatMap generation utility
      internal static class FlatMapGenerator
      {
            private const float FLAT_HEIGHT = 0f; // Sea level
            private const byte FLAT_BLUE = 123;   // Blue channel for 0m: signed_offset=-5, 123=(-5+128)
            private const int RESOLUTION = 513;

            internal static bool IsFlatMap()
            {
                  return MapManager.Instance?.directoryName == "FlatMap";
            }

            internal static void GenerateFlatHeightmap(NativeArray<float> heightmap)
            {
                  for (int i = 0; i < heightmap.Length; i++) {
                        heightmap[i] = FLAT_HEIGHT;
                  }
            }

            internal static Texture2D CreateFlatTexture()
            {
                  var texture = new Texture2D(RESOLUTION, RESOLUTION, TextureFormat.ARGB32, false);
                  texture.name = "FlatMap TileData";

                  var pixels = new Color32[RESOLUTION * RESOLUTION];
                  // Encode 0m height: ushort=0 (0m offset within band), blue=123 (band at 0m)
                  for (int i = 0; i < pixels.Length; i++) {
                        pixels[i] = new Color32(0, 0, FLAT_BLUE, 0);
                  }

                  texture.SetPixels32(pixels);
                  texture.Apply();
                  return texture;
            }
      }

      [HarmonyPatch(typeof(MapStore), "Load", [typeof(string)])]
      [HarmonyPrefix]
      internal static bool MapStoreLoadPrefix(MapStore __instance, string basePath)
      {
            // Try to create a tile provider from Map.json
            var providerProxy = TileProviderFactory.CreateFromMapJson(basePath);

            if (providerProxy != null)
            {
                  // Store provider in TileManager for later use
                  TileManager.Instance.RegisterProvider(__instance, providerProxy);

                  logger.Information("Loading map with {providerType} provider", providerProxy.GetProviderType());

                  // Set required MapStore fields manually
                  var basePathField = typeof(MapStore).GetField("_basePath", BindingFlags.NonPublic | BindingFlags.Instance);
                  basePathField?.SetValue(__instance, basePath);

                  // Set origin (0,0 for synthetic maps like FlatMap)
                  __instance.Origin = new LatLng(0f, 0f);

                  // Set standard tile dimension
                  __instance.TileDimension = 500;

                  logger.Information("Initialized map with {providerType} provider", providerProxy.GetProviderType());

                  return false; // Skip original method
            }

            return true; // Run original method for vanilla provider or if no provider configured
      }

      [HarmonyPatch(typeof(MapStore), "Load", [typeof(string)])]
      [HarmonyPostfix]
      internal static void MapStoreLoadPostfix(MapStore __instance, string basePath)
      {
            logger.Debug("MapStore Load({path})", basePath);
            TileManager.Instance.LoadMaps(__instance);
      }

    [HarmonyPatch(typeof(MapStore), "PathFor", [typeof(Vector2Int)])]
      [HarmonyPostfix]
      internal static void MapStorePathForPostfix(Vector2Int tp, ref string __result)
      {
            string tile = TileManager.Instance.GetMapTile(tp);
            if (tile != "") {
              __result = tile;
            }
            //logger.Debug("MapStore PathFor({x},{y}): {__result}", tp.x, tp.y, __result);
      }

      [HarmonyPatch(typeof(MapStore), "HasTileDataAt", [typeof(Vector2Int)])]
      [HarmonyPostfix]
      internal static void MapStoreHasTileDataAtPostfix(MapStore __instance, Vector2Int tilePosition, ref bool __result)
      {
            // Check if we have a custom provider for this map
            var providerProxy = TileManager.Instance.GetProvider(__instance);
            if (providerProxy != null && providerProxy.CanProvideTile(tilePosition))
            {
                  __result = true;
                  return;
            }

            // Legacy FlatMap check (for backward compatibility if provider not configured)
            if (FlatMapGenerator.IsFlatMap()) {
                  const int MAX_TILE_COORD = 25;
                  if (Mathf.Abs(tilePosition.x) <= MAX_TILE_COORD && Mathf.Abs(tilePosition.y) <= MAX_TILE_COORD) {
                        __result = true;
                  }
                  return;
            }

            // Regular tile download trigger
            if (!__result && TileManager.AllowDownloadingTiles) {
                TileManager.Instance.RequestTileDownload(tilePosition);
            }
    }

      [HarmonyPatch(typeof(MapStore), "TileDataAt", [typeof(Vector2Int)])]
      [HarmonyPrefix]
      internal static bool MapStoreTileDataAtPrefix(MapStore __instance, Vector2Int tilePosition, ref TileData __result)
      {
            // Check if we have a custom provider for this map
            var providerProxy = TileManager.Instance.GetProvider(__instance);
            if (providerProxy != null && providerProxy.CanProvideTile(tilePosition))
            {
                  try
                  {
                        __result = providerProxy.GetOrCreateTile(tilePosition, __instance);
                        return false; // Skip original method
                  }
                  catch (Exception ex)
                  {
                        logger.Error(ex, "Failed to generate tile ({x},{y}) with provider",
                              tilePosition.x, tilePosition.y);
                        // Fall through to original method or legacy FlatMap
                  }
            }

            // Legacy FlatMap handling (for backward compatibility)
            if (FlatMapGenerator.IsFlatMap()) {
                  // Check if tile is within 50x50 bounds (-25 to +25)
                  const int MAX_TILE_COORD = 25;
                  if (Mathf.Abs(tilePosition.x) > MAX_TILE_COORD || Mathf.Abs(tilePosition.y) > MAX_TILE_COORD) {
                        // Tile out of bounds - let original method throw exception
                        return true;
                  }

                  // Check if already loaded
                  var loadedField = typeof(MapStore).GetField("_loaded", BindingFlags.NonPublic | BindingFlags.Instance);
                  var loaded = loadedField?.GetValue(__instance) as System.Collections.Generic.Dictionary<Vector2Int, TileData>;

                  if (loaded != null && loaded.TryGetValue(tilePosition, out __result)) {
                        return false; // Return cached tile
                  }

                  // Create new FlatMap tile on-demand
                  var basePathField = typeof(MapStore).GetField("_basePath", BindingFlags.NonPublic | BindingFlags.Instance);
                  string basePath = basePathField?.GetValue(__instance) as string ?? "";

                  // Create a synthetic path (won't be used for actual file loading)
                  string dataPath = Path.Combine(basePath, $"tile_{tilePosition.x:000}_{tilePosition.y:000}.data");

                  // Calculate bounds for this tile
                  int tileDim = __instance.TileDimension;
                  var bounds = new Bounds(
                        new Vector3((tilePosition.x + 0.5f) * tileDim, 0f, (tilePosition.y + 0.5f) * tileDim),
                        new Vector3(tileDim, float.MaxValue, tileDim)
                  );

                  // Create TileData
                  const int resolution = 513;
                  __result = new TileData(tilePosition, dataPath, resolution, bounds);

                  // Load it (will trigger TileDataLoadIfNeededPrefix which generates flat data)
                  __result.LoadIfNeeded();

                  // Cache it in _loaded dictionary
                  if (loaded != null) {
                        loaded[tilePosition] = __result;
                  }

                  logger.Information("Created legacy FlatMap tile at ({x},{y})", tilePosition.x, tilePosition.y);

                  return false; // Skip original method
            }

            return true; // Run original method for vanilla maps
      }

#if PRIVATETESTING
    // Currently results in loss of accuracy in heightmaps, need to investigate float usage to resolve.

    private readonly static FieldInfo TileDataTexture = typeof(TileData).GetField("_dataTexture", BindingFlags.NonPublic | BindingFlags.Instance);
    private readonly static FieldInfo TileDataDataPath = typeof(TileData).GetField("_dataPath", BindingFlags.NonPublic | BindingFlags.Instance);
    private readonly static FieldInfo TileDataMarkerLoad = typeof(TileData).GetField("_markerLoad", BindingFlags.NonPublic | BindingFlags.Static);
    private readonly static MethodInfo TileDataReset = typeof(TileData).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Instance);
    [HarmonyPatch(typeof(MapManager), "PrepareTerrain", [typeof(TileData), typeof(Material)])]
    [HarmonyPostfix]
    internal static void MapManagerPrepareTerrainPostfix(TileData tileData, Material material, ref MapTerrain __result)
    {
        // Read blue channel from PNG to get the encoded base offset
        var _dataTexture = TileDataTexture.GetValue(tileData) as Texture2D;
        int signed_offset = 0;
        byte blue = 0;
        if (_dataTexture != null) {
            var pixels = _dataTexture.GetRawTextureData<TileData.ColorARGB32>();
            // Sample a pixel to get the blue channel (should be consistent across tile)
            blue = pixels[0].b;
            signed_offset = (blue == 0) ? 0 : blue - 128;
        }
        float base_offset = 500f + (signed_offset * 100f);

        // Set terrain Y position to base offset
        var pos = __result.transform.localPosition;
        pos.y = base_offset;
        __result.transform.localPosition = pos;

        // Keep terrain height at 1000m for proper precision (not MaxHeight)
        var size = __result.terrain.terrainData.size;
        size.y = 1000f;
        __result.terrain.terrainData.size = size;

        logger.Debug("Tile ({x},{y}): blue={blue}, signed_offset={offset}, base_offset={base:F0}m",
            tileData.TilePosition.x, tileData.TilePosition.y, blue, signed_offset, base_offset);
    }

    // public void ReadHeightTexture(float heightMin, float heightMax, Texture2D target)
    // Override heightMin/heightMax to use tile-specific range based on band
    [HarmonyPatch(typeof(TileData), "ReadHeightTexture", [typeof(float), typeof(float), typeof(Texture2D)])]
    [HarmonyPrefix]
    internal static void TileDataReadHeightTexturePrefix(TileData __instance, ref float heightMin, ref float heightMax)
    {
        // Read blue channel from PNG to get the encoded base offset (same as PrepareTerrain)
        var _dataTexture = TileDataTexture.GetValue(__instance) as Texture2D;
        int signed_offset = 0;
        byte blue = 0;
        if (_dataTexture != null) {
            var pixels = _dataTexture.GetRawTextureData<TileData.ColorARGB32>();
            blue = pixels[0].b;
            signed_offset = (blue == 0) ? 0 : blue - 128;
        }
        float base_offset = 500f + (signed_offset * 100f);

        // Use tile-specific normalization range (1000m span for 1.5cm precision)
        heightMin = base_offset;
        heightMax = base_offset + 1000f;

        logger.Debug("ReadHeightTexture tile ({x},{y}): blue={blue}, range=[{min:F0}, {max:F0}]",
            __instance.TilePosition.x, __instance.TilePosition.y, blue, heightMin, heightMax);
    }

    private struct PopulateHeightmap : IJobParallelFor
    {
        public void Execute(int index)
        {
            TileData.ColorARGB32 colorARGB = Source[index];
            ushort num = (ushort)(((int)colorARGB.r * 256) + (int)colorARGB.g);
            var num2 = colorARGB.b == 0 ? 0 : colorARGB.b - 128;
            // Base offset: 500m + (signed_offset * 100m)
            // Height within band: encoded / 65.535 (spans 0-1000m with 1.5cm precision)
            Target[index] = 500f + (num2 * 100f) + ((float)num / 65.535f);
        }

        // Token: 0x040001D8 RID: 472
        [ReadOnly]
        public NativeArray<TileData.ColorARGB32> Source;

        // Token: 0x040001D9 RID: 473
        [WriteOnly]
        public NativeArray<float> Target;

        public PopulateHeightmap() { }
    }

    [HarmonyPatch(typeof(TileData), "LoadIfNeeded")]
    [HarmonyPrefix]
    internal static bool TileDataLoadIfNeededPrefix(TileData __instance)
    {
        // Handle FlatMap generation
        if (FlatMapGenerator.IsFlatMap()) {
            return LoadFlatMapTile(__instance);
        }

        // Regular tile loading with extended heights
        var td = __instance;
        var _dataTexture = TileDataTexture.GetValue(td) as Texture2D;
        var _dataPath = TileDataDataPath.GetValue(td) as string;
        if (_dataTexture != null) {
            return false;
        }
        _dataTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false)
        {
            name = "TileData"
        };
        TileDataTexture.SetValue(td, _dataTexture);
        byte[] array = File.ReadAllBytes(_dataPath);
        _dataTexture.LoadImage(array, false);
        int width = _dataTexture.width;
        TileDataReset.Invoke(td, [width]);
        NativeArray<TileData.ColorARGB32> rawTextureData = _dataTexture.GetRawTextureData<TileData.ColorARGB32>();
        new PopulateHeightmap
        {
            Source = rawTextureData,
            Target = td.Heightmap
        }.Schedule(width * width, width, default(JobHandle)).Complete();
        td.Dirty = false;
        return false; // skip original method
    }

    private static bool LoadFlatMapTile(TileData tileData)
    {
        var _dataTexture = TileDataTexture.GetValue(tileData) as Texture2D;
        if (_dataTexture != null) {
            return false; // Already loaded
        }

        // Create texture and reset heightmap
        const int resolution = 513;
        _dataTexture = FlatMapGenerator.CreateFlatTexture();
        TileDataTexture.SetValue(tileData, _dataTexture);
        TileDataReset.Invoke(tileData, new object[] { resolution });

        // Fill heightmap with flat 0m elevation
        FlatMapGenerator.GenerateFlatHeightmap(tileData.Heightmap);

        tileData.Dirty = false;

        logger.Information("Generated flat tile at ({x},{y}) for FlatMap",
            tileData.TilePosition.x, tileData.TilePosition.y);

        return false; // Skip original method
    }

    private static ushort FloatToUshort(float value, float heightBase = 500f) => (ushort)Mathf.Clamp(Mathf.FloorToInt((value - heightBase) * 65.535f), 0, 65535);
    private static byte PackValue(byte value, int bits)
	{
		int num = (1 << bits) - 1;
		return (byte) Mathf.RoundToInt((float) value / 255f * (float) num);
	}

    [HarmonyPatch(typeof(TileData), "Save")]
    [HarmonyPrefix]
    internal static bool TileDataSavePrefix(TileData __instance)
    { 
        var td = __instance;
        var _dataTexture = TileDataTexture.GetValue(td) as Texture2D;
        var _dataPath = TileDataDataPath.GetValue(td) as string;
        int resolution = td.Resolution;
        if (_dataTexture == null) {
            _dataTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
            NativeArray<TileData.ColorARGB32> rawTextureData = _dataTexture.GetRawTextureData<TileData.ColorARGB32>();
            TileData.ColorARGB32 colorARGB = default(TileData.ColorARGB32);
            for (int i = 0; i < rawTextureData.Length; i++) {
                rawTextureData[i] = colorARGB;
            }
        }
        Color32[] pixels = _dataTexture.GetPixels32();
        td.GetMask(TileMaskName.BiomeControl).GetRawTextureData<byte>();
        NativeArray<byte> rawTextureData2 = td.GetMask(TileMaskName.Vegetation).GetRawTextureData<byte>();
        NativeArray<byte> rawTextureData3 = td.GetMask(TileMaskName.Water).GetRawTextureData<byte>();
        int num = resolution - 1;

        // Calculate base offset from tile minimum, rounded down to 100m increments
        // This ensures all heights fit within [base, base+1000] range without clamping
        float min = td.Heightmap.Min();
        int signed_offset = (int)Math.Floor((min - 500.0) / 100.0);
        float base_offset = 500f + (signed_offset * 100f);

        for (int j = 0; j < resolution; j++) {
            for (int k = 0; k < resolution; k++) {
                ushort num2 = FloatToUshort(td.Heightmap[(j * resolution) + k], base_offset);
                byte b3;
                byte b4;
                if (k < num && j < num) {
                    int num3 = (j * num) + k;
                    byte b = (byte)(byte.MaxValue - rawTextureData2[num3]);
                    byte b2 = rawTextureData3[num3];
                    // Store signed offset in blue channel: offset = (b - 128) * 100
                    b3 = (byte)(signed_offset + 128);
                    b4 = (byte)(((int)PackValue(b2, 1) << 7) | ((int)PackValue(b, 3) << 4));
                } else {
                    b3 = 0;
                    b4 = 0;
                }
                Color32 color = new Color32((byte)((num2 >> 8) & 255), (byte)(num2 & 255), b3, b4);
                pixels[(j * resolution) + k] = color;
            }
        }
        _dataTexture.SetPixels32(pixels);
        _dataTexture.Apply();
        byte[] array = _dataTexture.EncodeToPNG();
        File.WriteAllBytes(_dataPath, array);
        td.Dirty = false;
        return false;
    }

    // Patch seam fixing to handle cross-band boundaries where terrains have different Y positions
    [HarmonyPatch(typeof(TerrainDataCopySeam), nameof(TerrainDataCopySeam.CopySeam),
        new Type[] { typeof(MapTerrain), typeof(MapTerrain), typeof(SeamEdge) })]
    [HarmonyPrefix]
    internal static bool CopySeamBandAwarePrefix(MapTerrain target, MapTerrain reference, SeamEdge seamEdge)
    {
        // Null and status checks
        if (target == null || reference == null) return false;
        if (target.buildStatus == MapTerrain.BuildStatus.Pending ||
            reference.buildStatus == MapTerrain.BuildStatus.Pending) return false;

        // Get terrain Y positions
        float targetY = target.transform.localPosition.y;
        float refY = reference.transform.localPosition.y;
        float yDelta = targetY - refY;

        // If same band (within 1m tolerance), use original method
        if (Mathf.Abs(yDelta) < 1f) {
            return true; // Run original
        }

        // Cross-band boundary detected - need to adjust heights
        logger.Debug("Cross-band seam: target tile ({x},{y}) at Y={targetY:F0}m, ref tile ({rx},{ry}) at Y={refY:F0}m",
            target.tileData.TilePosition.x, target.tileData.TilePosition.y, targetY,
            reference.tileData.TilePosition.x, reference.tileData.TilePosition.y, refY);

        // Get terrain data
        TerrainData targetTD = target.terrain.terrainData;
        TerrainData refTD = reference.terrain.terrainData;
        int resolution = targetTD.heightmapResolution;
        float terrainHeight = targetTD.size.y; // Should be 1000m for both

        float[,] targetHeights;
        float[,] refHeights;

        if (seamEdge == SeamEdge.MinX) {
            // Copy from reference's right edge (max X) to target's left edge (min X)
            targetHeights = targetTD.GetHeights(0, 0, 1, resolution);
            refHeights = refTD.GetHeights(resolution - 1, 0, 1, resolution);

            int clampCount = 0;
            for (int i = 0; i < resolution; i++) {
                // Denormalize: convert reference normalized height to absolute world height
                float absHeight = refY + (refHeights[i, 0] * terrainHeight);

                // Renormalize: convert absolute height to target's normalized space
                float normalized = (absHeight - targetY) / terrainHeight;

                // Track clamping
                if (normalized < 0f || normalized > 1f) clampCount++;

                // Clamp to valid range and assign
                targetHeights[i, 0] = Mathf.Clamp01(normalized);
            }

            targetTD.SetHeights(0, 0, targetHeights);

            if (clampCount > 0) {
                logger.Warning("Cross-band seam MinX: {count}/{total} heights clamped (elevation mismatch)",
                    clampCount, resolution);
            }
        }
        else if (seamEdge == SeamEdge.MinY) {
            // Copy from reference's top edge (max Y) to target's bottom edge (min Y)
            targetHeights = targetTD.GetHeights(0, 0, resolution, 1);
            refHeights = refTD.GetHeights(0, resolution - 1, resolution, 1);

            int clampCount = 0;
            for (int j = 0; j < resolution; j++) {
                // Denormalize to absolute world height
                float absHeight = refY + (refHeights[0, j] * terrainHeight);

                // Renormalize to target's space
                float normalized = (absHeight - targetY) / terrainHeight;

                // Track clamping
                if (normalized < 0f || normalized > 1f) clampCount++;

                // Clamp and assign
                targetHeights[0, j] = Mathf.Clamp01(normalized);
            }

            targetTD.SetHeights(0, 0, targetHeights);

            if (clampCount > 0) {
                logger.Warning("Cross-band seam MinY: {count}/{total} heights clamped (elevation mismatch)",
                    clampCount, resolution);
            }
        }

        return false; // Skip original method
    }
#endif
    #endregion
}
