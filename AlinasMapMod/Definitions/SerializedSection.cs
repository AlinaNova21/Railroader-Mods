using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using Game.Progression;
using Serilog;

namespace AlinasMapMod.Definitions
{
    internal class SerializedSection
    {
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public Dictionary<string, bool> PrerequisiteSections { get; set; } = new Dictionary<string, bool>();
        public IEnumerable<SerializedDeliveryPhase> DeliveryPhases { get; set; } = new List<SerializedDeliveryPhase>();
        public Dictionary<string, bool> DisableFeaturesOnUnlock { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> EnableFeaturesOnUnlock { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> EnableFeaturesOnAvailable { get; set; } = new Dictionary<string, bool>();

        public SerializedSection()
        {
        }

        public SerializedSection(Section s)
        {
            DisplayName = s.displayName;
            Description = s.description;
            PrerequisiteSections = s.prerequisiteSections.ToDictionary(s => s.identifier, s => true);
            DeliveryPhases = s.deliveryPhases.Select(dp => new SerializedDeliveryPhase(dp));
            DisableFeaturesOnUnlock = s.disableFeaturesOnUnlock.ToDictionary(f => f.identifier, f => true);
            EnableFeaturesOnUnlock = s.enableFeaturesOnUnlock.ToDictionary(f => f.identifier, f => true);
            EnableFeaturesOnAvailable = s.enableFeaturesOnAvailable.ToDictionary(f => f.identifier, f => true);
        }

        internal void ApplyTo(Section sec, ObjectCache cache)
        {
            sec.displayName = DisplayName;
            sec.description = Description;
            sec.prerequisiteSections = Utils.ApplyList(sec.prerequisiteSections ?? [], PrerequisiteSections, cache.Sections);
            sec.disableFeaturesOnUnlock = Utils.ApplyList(sec.disableFeaturesOnUnlock ?? [], DisableFeaturesOnUnlock, cache.MapFeatures);
            sec.enableFeaturesOnUnlock = Utils.ApplyList(sec.enableFeaturesOnUnlock ?? [], EnableFeaturesOnUnlock, cache.MapFeatures);
            sec.enableFeaturesOnAvailable = Utils.ApplyList(sec.enableFeaturesOnAvailable ?? [], EnableFeaturesOnAvailable, cache.MapFeatures);

            sec.deliveryPhases = DeliveryPhases.Select(dp =>
            {
                var phase = new Section.DeliveryPhase();
                dp.ApplyTo(phase, cache);
                return phase;
            }).ToArray();
        }
    }
}