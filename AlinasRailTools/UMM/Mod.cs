using AlinasRailTools.Hooks;
using Serilog;
using UnityModManagerNet;

namespace AlinasRailTools.UMM;

#if PRIVATETESTING
[EnableReloading]
#endif
internal class Mod
{
    private static ILogger Logger = Log.ForContext<Mod>();
    public static bool Loaded { get; private set; } = false;

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
        if (Loaded)
        {
            Logger.Information("Already loaded");
            return true;
        }

        Logger.Information("Loading AlinasRailTools via UMM");

        // Initialize hooks (idempotent - safe to call even if BepInEx already called it)
        HookInit.InitAllHooks();

        modEntry.OnUnload = Unload;
        Loaded = true;

        Logger.Information("AlinasRailTools loaded successfully");
        return true;
    }

    public static bool Unload(UnityModManager.ModEntry modEntry)
    {
        if (!Loaded)
        {
            Logger.Information("Already unloaded");
            return true;
        }

        Logger.Information("Unloading AlinasRailTools");
        Loaded = false;
        return true;
    }
}
