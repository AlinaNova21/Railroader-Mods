using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Model.Ops;
using Model.Ops.Timetable;

namespace AlinasMapMod.Patches;

[HarmonyPatch(typeof(TimetableController), nameof(TimetableController.GetAllStations))]
[HarmonyPatchCategory("AlinasMapMod")]
internal static class TimetableControllerPatch
{
  [HarmonyPrefix]
  internal static bool GetAllStaitonsModified(TimetableController __instance,
    ref IReadOnlyList<TimetableStation> __result,
    TimetableBranch branch, bool includeDisabled, bool includeDuplicates)
  {
    if (branch != null)
    {
      __result = branch.stations.Where(st => includeDisabled || st.IsEnabled)
        .Where(ts => includeDuplicates || !ts.IsBranchJunctionDuplicate).Distinct().ToList();
      return false;
    }

    var branches = __instance.branches;
    var mainBranch = branches.FirstOrDefault(b => b.name == "Main");
    if (mainBranch == null)
    {
      // Fallback to original logic if no Main branch
      var list = branches.SelectMany(br => br.stations).Where(st => includeDisabled || st.IsEnabled)
        .Where(ts => includeDuplicates || !ts.IsBranchJunctionDuplicate).Distinct().ToList();
      __result = list;
      return false;
    }

    var resultList = new List<TimetableStation>();
    var visitedBranches = new HashSet<string>();
    
    void ProcessBranch(TimetableBranch currentBranch, int depth = 0)
    {
      if (currentBranch == null || visitedBranches.Contains(currentBranch.name) || depth > 10) return;
      visitedBranches.Add(currentBranch.name);

      ProcessStations(currentBranch.stations, depth);
    }

    void ProcessStations(List<TimetableStation> stationsToProcess, int depth)
    {
      if (depth > 10) return;

      foreach (var station in stationsToProcess)
      {
        if (station.junctionType == TimetableStation.JunctionType.JunctionStation)
        {
          // Find branches that have a JunctionDuplicate of this station
          var subBranches = branches.Where(b => !visitedBranches.Contains(b.name) &&
                                                b.stations.Any(s => s.code == station.code && s.junctionType == TimetableStation.JunctionType.JunctionDuplicate)).ToList();

          foreach (var subBranch in subBranches)
          {
            visitedBranches.Add(subBranch.name);
            var duplicateIndex = subBranch.stations.FindIndex(s => s.code == station.code && s.junctionType == TimetableStation.JunctionType.JunctionDuplicate);
            
            // Stations before the duplicate in the sub-branch
            var beforeStations = subBranch.stations.Take(duplicateIndex).ToList();
            ProcessStations(beforeStations, depth + 1);
          }

          // The JunctionStation itself
          AddStation(station);

          foreach (var subBranch in subBranches)
          {
            var duplicateIndex = subBranch.stations.FindIndex(s => s.code == station.code && s.junctionType == TimetableStation.JunctionType.JunctionDuplicate);
            // Stations after the duplicate
            var afterStations = subBranch.stations.Skip(duplicateIndex + 1).ToList();
            ProcessStations(afterStations, depth + 1);
          }
        }
        else
        {
          AddStation(station);
        }
      }
    }

    void AddStation(TimetableStation station)
    {
      if (!includeDisabled && !station.IsEnabled) return;
      if (!includeDuplicates && station.IsBranchJunctionDuplicate) return;
      if (resultList.Any(s => s.code == station.code)) return;
      
      resultList.Add(station);
    }

    ProcessBranch(mainBranch);
    __result = resultList.Distinct().ToList();
    return false;
  }
}