using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AlinasMapMod.Stations;
using HarmonyLib;
using Model.Ops;
using Serilog;
using Track;

namespace AlinasMapMod.Patches;

[HarmonyPatch(typeof(PassengerStop), "OnEnable")]
[HarmonyPatchCategory("AlinasMapMod")]
internal static class PassengerStopOnEnable
{
    static Serilog.ILogger logger = Log.ForContext(typeof(PassengerStopOnEnable));

    static TrackSpan[] PatchSpans(PassengerStop stop, TrackSpan[] spans)
    {
        var comp = stop.transform.parent.GetComponentInChildren<PaxStationComponent>();
        if (comp != null) {
            spans = comp.trackSpans.ToList().Where(span => span.upper?.segment != null && span.lower?.segment != null).ToArray();
            foreach (var span in spans) {
                logger.Information("Span {id} {upper} {lower}", span.id, span.upper, span.lower);
            }
        }
        logger.Information("PassengerStop {id} OnEnable() {spans} {comp}", stop.identifier, spans, comp);
        return spans;
    }

    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        // Without ILGenerator, the CodeMatcher will not be able to create labels
        var codeMatcher = new CodeMatcher(instructions, generator);
        codeMatcher
            .MatchStartForward(
                CodeMatch.StoresField(typeof(PassengerStop).GetField("_spans", BindingFlags.Instance | BindingFlags.NonPublic))
            )
            .ThrowIfInvalid("Could not find location to insert code")
            .Insert([
                CodeInstruction.Call(() => PatchSpans(default, default)),
            ])
            .Advance(-1)
            .Insert([
                CodeInstruction.LoadArgument(0),
            ]);
        return codeMatcher.Instructions();
    }
}