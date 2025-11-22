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

      internal const float MaxHeight = 2000f;

      [HarmonyPatch(typeof(MapStore), "Load", [typeof(string)])]
      [HarmonyPostfix]
      internal static void MapStoreLoadPostfix(MapStore __instance, string basePath)
      {
            logger.Debug("MapStore Load({path})");
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
      internal static void MapStoreHasTileDataAtPostfix(Vector2Int tilePosition, bool __result)
      {
            if (!__result && TileManager.AllowDownloadingTiles) {
                TileManager.Instance.RequestTileDownload(tilePosition);
            }
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
        //var tex = TileDataTexture.GetValue(tileData) as Texture2D;
        //var pixels = tex.GetRawTextureData<TileData.ColorARGB32>();
        //int y = 0;
        //for(int i = 0; i < pixels.Length; i++) {
        //    var color = pixels[i];
        //    if (color.b > 0) {
        //        y = color.b;
        //        break;
        //    }
        //}
        //if (y > 0) y -= 128; // Adjust for signed byte
        //var offset = Vector3.up * y * 100;
        __result.transform.localPosition -= Vector3.up * 500;
        var size = __result.terrain.terrainData.size;
        size.y = MaxHeight;
        __result.terrain.terrainData.size = size;
        //logger.Information("Applied height offset of {offset} to terrain tile {x},{y}, ({b})", offset, tileData.TilePosition.x, tileData.TilePosition.y, y);
    }

    // public void ReadHeightTexture(float heightMin, float heightMax, Texture2D target)
    // override heightMin to 0 and heightMax to 3000
    [HarmonyPatch(typeof(TileData), "ReadHeightTexture", [typeof(float), typeof(float), typeof(Texture2D)])]
    [HarmonyPrefix]
    internal static void TileDataReadHeightTexturePrefix(ref float heightMin, ref float heightMax)
    {
        heightMin = 0f;
        heightMax = MaxHeight;
    }

    private struct PopulateHeightmap : IJobParallelFor
    {
        public void Execute(int index)
        {
            TileData.ColorARGB32 colorARGB = Source[index];
            ushort num = (ushort)(((int)colorARGB.r * 256) + (int)colorARGB.g);
            var num2 = colorARGB.b == 0 ? 0 : colorARGB.b - 128;
            Target[index] = ((float)num / 65.535f) + 500 + (num2 * 100);
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
        var heightBase = (float)Math.Floor((td.Heightmap.Average() - 500f) / 100) * 100; // Calculate height base from average heightmap value, offset by 500m
        for (int j = 0; j < resolution; j++) {
            for (int k = 0; k < resolution; k++) {
                ushort num2 = FloatToUshort(td.Heightmap[(j * resolution) + k], heightBase);
                byte b3;
                byte b4;
                if (k < num && j < num) {
                    int num3 = (j * num) + k;
                    byte b = (byte)(byte.MaxValue - rawTextureData2[num3]);
                    byte b2 = rawTextureData3[num3];
                    b3 = (byte)((heightBase - 500) / 100);
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
#endif
    #endregion
}
