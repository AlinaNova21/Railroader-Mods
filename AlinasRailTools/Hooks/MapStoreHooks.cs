using AlinasRailTools.Runtime;
using HarmonyLib;
using Map.Runtime;
using Serilog;
using UnityEngine;

namespace AlinasRailTools.Hooks;

[HarmonyPatch]
static class MapStoreHooks
{
    private static readonly Serilog.ILogger Logger = Log.ForContext(typeof(MapStoreHooks));

    [HarmonyPatch(typeof(MapStore), "Load", [typeof(string)])]
    [HarmonyPostfix]
    static void MapStore_Load_Postfix(MapStore __instance, string basePath)
    {
        Logger.Debug("MapStore Load({BasePath})", basePath);
        TileManager.Instance?.LoadMaps(__instance);
    }

    [HarmonyPatch(typeof(MapStore), "PathFor", [typeof(Vector2Int)])]
    [HarmonyPostfix]
    static void MapStore_PathFor_Postfix(Vector2Int tp, ref string __result)
    {
        if (TileManager.Instance == null) return;

        string tile = TileManager.Instance.GetMapTile(tp);
        if (tile != "")
        {
            __result = tile;
        }
        //Logger.Debug("MapStore PathFor({X},{Y}): {Result}", tp.x, tp.y, __result);
    }

    [HarmonyPatch(typeof(MapStore), "HasTileDataAt", [typeof(Vector2Int)])]
    [HarmonyPostfix]
    static void MapStore_HasTileDataAt_Postfix(Vector2Int tilePosition, bool __result)
    {
        if (!__result && TileManager.AllowDownloadingTiles && TileManager.Instance != null)
        {
            TileManager.Instance.RequestTileDownload(tilePosition);
        }
    }
}
