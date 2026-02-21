using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Model.Ops.Timetable;
using UI.Timetable;

namespace AlinasMapMod.Patches;

[HarmonyPatch(typeof(TimetableWindow), "GetTimetableStationsAndTrains")]
[HarmonyPatchCategory("AlinasMapMod")]
internal static class TimetableWindowPatch
{
  [HarmonyPrefix]
  static bool Prefix(TimetableWindow __instance,
    TimetableController timetableController,
    Timetable timetable,
    TimetableBranch branch,
    out IReadOnlyList<TimetableStation> allStations,
    out List<Timetable.Train> trainsWest,
    out List<Timetable.Train> trainsEast)
  {
    var stations = allStations = timetableController.GetAllStations(branch);
    trainsWest = timetable.Trains.Values.Where(t => t.Direction == Timetable.Direction.West)
      .Where(t => TrainStationShouldBeVisible(t, stations)).OrderByDescending(t => t.TrainClass)
      .ThenByDescending(t => t.SortOrderWithinClass).ToList();
    trainsEast = timetable.Trains.Values.Where(t => t.Direction == Timetable.Direction.East)
      .Where(t => TrainStationShouldBeVisible(t, stations)).OrderBy(t => t.TrainClass)
      .ThenBy(t => t.SortOrderWithinClass).ToList();
    return false;
  }

  private static bool TrainStationShouldBeVisible(Timetable.Train train, IReadOnlyList<TimetableStation> stations)
  {
    int entriesOnBranch = 0;
    foreach (Timetable.Entry entry in train.Entries)
    {
      var station = stations.FirstOrDefault(s => s.code == entry.Station);
      if (station == null) continue;
      if (station.junctionType == TimetableStation.JunctionType.None) return true;
      if (station.junctionType == TimetableStation.JunctionType.JunctionStation) {
        entriesOnBranch++;
        if (entriesOnBranch > 1) return true;
      }
    }

    return false;
  }
}