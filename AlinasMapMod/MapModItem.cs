using System.Collections.Generic;
using Game.Progression;

namespace AlinasMapMod
{
    public class MixinDef
    {
        public Dictionary<string, MapModItem> Items { get; set; } = new Dictionary<string, MapModItem>();
    }
    public class MapModItem
    {
        public string Identifier { get; set; } = "";
        public string Name { get; set; } = "";
        public string[] GroupIds { get; set; } = [];
        public string Description { get; set; } = "";
        public string[] PrerequisiteSections { get; set; } = [];
        public Section.DeliveryPhase[] DeliveryPhases { get; set; } = [];
        public string Area { get; set; } = "";
        public string[] TrackSpans { get; set; } = [];
        public string IndustryComponent {get; set; } = "";
    }
}
