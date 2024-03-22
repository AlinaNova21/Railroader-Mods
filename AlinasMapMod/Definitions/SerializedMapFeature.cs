using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Game.Progression;
using Helpers;
using Model.OpsNew;
using Serilog;
using UnityEngine;

namespace AlinasMapMod.Definitions
{
    public class SerializedMapFeature
    {
        public string Description { get; set; } = "";
        public Dictionary<string, bool> Prerequisites { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> AreasEnableOnUnlock { get; set; } = new Dictionary<string, bool>();
        public bool DefaultEnableInSandbox { get; set; } = false;
        public string DisplayName { get; set; } = "";
        public Dictionary<string, bool> GameObjectsEnableOnUnlock { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> TrackGroupsAvailableOnUnlock { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> TrackGroupsEnableOnUnlock { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> UnlockExcludeIndustries { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> UnlockIncludeIndustries { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, bool> UnlockIncludeIndustryComponents { get; set; } = new Dictionary<string, bool>();

        public SerializedMapFeature()
        {
        }
        public SerializedMapFeature(MapFeature feat)
        {
            Description = feat.description; // Assign a value to Description
            if (feat.prerequisites != null)
            {
                Prerequisites = feat.prerequisites.ToDictionary(p => p.identifier, p => true);
            }
            if (feat.areasEnableOnUnlock != null)
            {
                AreasEnableOnUnlock = feat.areasEnableOnUnlock.ToDictionary(a => a.identifier, a => true);
            }
            DefaultEnableInSandbox = feat.defaultEnableInSandbox;
            DisplayName = feat.displayName;
            if (feat.gameObjectsEnableOnUnlock != null)
            {
                GameObjectsEnableOnUnlock = feat.gameObjectsEnableOnUnlock.ToDictionary(g => getPath(g), g => true);
            }
            if (feat.trackGroupsAvailableOnUnlock != null)
            {
                TrackGroupsAvailableOnUnlock = feat.trackGroupsAvailableOnUnlock.ToDictionary(t => t, t => true);
            }
            if (feat.trackGroupsEnableOnUnlock != null)
            {
                TrackGroupsEnableOnUnlock = feat.trackGroupsEnableOnUnlock.ToDictionary(t => t, t => true);
            }
            if (feat.unlockExcludeIndustries != null)
            {
                UnlockExcludeIndustries = feat.unlockExcludeIndustries.ToDictionary(i => i.identifier, i => true);
            }
            if (feat.unlockIncludeIndustries != null)
            {
                UnlockIncludeIndustries = feat.unlockIncludeIndustries.ToDictionary(i => i.identifier, i => true);
            }
            if (feat.unlockIncludeIndustryComponents != null)
            {
                UnlockIncludeIndustryComponents = feat.unlockIncludeIndustryComponents.ToDictionary(i => i.Identifier, i => true);
            }
        }

        internal void ApplyTo(MapFeature feat, ObjectCache cache)
        {
            feat.description = Description;
            feat.defaultEnableInSandbox = DefaultEnableInSandbox;
            feat.displayName = DisplayName;
            // TODO: Handle GameObjectsEnableOnUnlock
            feat.gameObjectsEnableOnUnlock = [];
            // feat.gameObjectsEnableOnUnlock = GameObjectsEnableOnUnlock.Keys.Select(g => gameObjectFromPath(g)).ToArray();
            feat.prerequisites = Utils.ApplyList(feat.prerequisites ?? [], Prerequisites, cache.MapFeatures);
            feat.areasEnableOnUnlock = Utils.ApplyList(feat.areasEnableOnUnlock ?? [], AreasEnableOnUnlock, cache.Areas);
            feat.trackGroupsAvailableOnUnlock = Utils.ApplyList(feat.trackGroupsAvailableOnUnlock ?? [], TrackGroupsAvailableOnUnlock);
            feat.trackGroupsEnableOnUnlock = Utils.ApplyList(feat.trackGroupsEnableOnUnlock ?? [], TrackGroupsEnableOnUnlock);
            feat.unlockExcludeIndustries = Utils.ApplyList(feat.unlockExcludeIndustries ?? [], UnlockExcludeIndustries, cache.Industries);
            feat.unlockIncludeIndustries = Utils.ApplyList(feat.unlockIncludeIndustries ?? [], UnlockIncludeIndustries, cache.Industries);
            feat.unlockIncludeIndustryComponents = Utils.ApplyList(feat.unlockIncludeIndustryComponents ?? [], UnlockIncludeIndustryComponents, cache.IndustryComponents);
        }

        private string getPath(GameObject go)
        {
            return string.Join("/", go.GetComponentsInParent<Transform>().Select(t => t.name).Reverse().ToArray()) + "/" + go.name;
        }

        private GameObject gameObjectFromPath(string path)
        {
            // TODO: Handle objects not found
            var parts = path.Split('/');
            var go = GameObject.Find(parts[0]);
            for (int i = 1; i < parts.Length; i++)
            {
                go = go.transform.Find(parts[i]).gameObject;
            }
            return go;
        }
    }
}