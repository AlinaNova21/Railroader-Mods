using System.Collections.Generic;

namespace AlinasMapMod.Definitions;

public class PatchState
{
    public Dictionary<string, SerializedMapFeature> MapFeatures;
    public Dictionary<string, SerializedProgression> Progressions;
    public PatchState()
    {
        MapFeatures = new Dictionary<string, SerializedMapFeature>();
        Progressions = new Dictionary<string, SerializedProgression>();
    }
}