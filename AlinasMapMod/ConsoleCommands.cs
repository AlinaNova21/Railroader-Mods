using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Ops;
using Model.Ops.Timetable;
using UI.Console;
using UnityEngine;

namespace AlinasMapMod;

internal class ConsoleCommands
{
  public static void RegisterCommands()
  {
    var cch = GameObject.FindObjectOfType<ConsoleCommandHandler>();
    var register = cch.GetType()
      .GetMethod("Register", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    var processor = cch.GetType()
      .GetField("_processor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
      ?.GetValue(cch) as CommandProcessor;

    if (register == null) throw new InvalidOperationException();

    var commands = new List<IConsoleCommand>() {
      new DumpStationsConsoleCommand()
    };
    foreach (var command in commands) {
      register.MakeGenericMethod([command.GetType()]).Invoke(cch, [command]);
    }
    processor.RegisterHandlers(typeof(ConsoleCommands).Assembly);
  }
}

[ConsoleCommand("/amm-dump-stations", "Dump all stations to the console")]
public class DumpStationsConsoleCommand : IConsoleCommand
{
  public string Execute(string[] components)
  {
    var sb = new StringBuilder();
    var stops = GameObject.FindObjectsOfType<PassengerStop>();
    foreach (var stop in stops) {
      var area = stop.GetComponentInParent<Area>();
      var industry = stop.GetComponentInParent<Industry>();

      var ttc = GameObject.FindObjectOfType<TimetableController>();
      var branch = ttc.branches.FirstOrDefault(b => b.stations.Any(s => s.passengerStop == stop));

      sb.AppendLine($"Station: {stop.identifier} ({stop.name})");
      sb.AppendLine($"  Area: {area.identifier}");
      sb.AppendLine($"  Industry: {industry.identifier}");
      sb.AppendLine($"  TimetableCode: {stop.timetableCode}");
      sb.AppendLine($"  BasePopulation: {stop.basePopulation}");
      sb.AppendLine($"  Neighbors: {string.Join(", ", stop.neighbors.Select(n => n.identifier))}");
      sb.AppendLine($"  Branch: {branch.name}");
      sb.AppendLine($"  Spans: {string.Join(", ", stop.TrackSpans.Select(s => s.name))}");
      sb.AppendLine();
    }
    Serilog.Log.Logger.Information(sb.ToString());
    return sb.ToString();
  }
}
