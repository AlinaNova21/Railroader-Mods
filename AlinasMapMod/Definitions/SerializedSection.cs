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

        private Section[] ApplyList(Section[] sections, Dictionary<string, bool> dict, Dictionary<string, Section> cached)
        {
            var items = sections.ToDictionary(s => s.identifier, s => s);
            foreach (var pair in dict)
            {
                var identifier = pair.Key;
                var val = pair.Value;
                if (val) {
                    if(!items.ContainsKey(identifier))
                    {
                        if (cached.TryGetValue(identifier, out var item))
                        {
                            items.Add(identifier, item);
                        } else {
                            Log.Warning("Section not found: {id}", identifier);
                        }
                    }
                }
                else
                {
                    items.Remove(identifier);
                }
            }
            return items.Values.ToArray();
        }

        private MapFeature[] ApplyList(MapFeature[] features, Dictionary<string, bool> dict, Dictionary<string, MapFeature> cached)
        {
            var items = features.ToDictionary(s => s.identifier, s => s);
            foreach (var pair in dict)
            {
                var identifier = pair.Key;
                var val = pair.Value;
                if (val) {
                    if(!items.ContainsKey(identifier))
                    {
                        if (cached.TryGetValue(identifier, out var item))
                        {
                            items.Add(identifier, item);
                        } else {
                            Log.Warning("MapFeature not found: {id}", identifier);
                        }
                    }
                }
                else
                {
                    items.Remove(identifier);
                }
            }
            return items.Values.ToArray();
        }

        internal void ApplyTo(Section sec, ObjectCache cache)
        {
            sec.displayName = DisplayName;
            sec.description = Description;
            sec.prerequisiteSections = ApplyList(sec.prerequisiteSections ?? [], PrerequisiteSections, cache.Sections);
            sec.disableFeaturesOnUnlock = ApplyList(sec.disableFeaturesOnUnlock ?? [], DisableFeaturesOnUnlock, cache.MapFeatures);
            sec.enableFeaturesOnUnlock = ApplyList(sec.enableFeaturesOnUnlock ?? [], EnableFeaturesOnUnlock, cache.MapFeatures);
            sec.enableFeaturesOnAvailable = ApplyList(sec.enableFeaturesOnAvailable ?? [], EnableFeaturesOnAvailable, cache.MapFeatures);

            sec.deliveryPhases = DeliveryPhases.Select(dp =>
            {
                var phase = new Section.DeliveryPhase();
                dp.ApplyTo(phase, cache);
                return phase;
            }).ToArray();
        }
    }
}