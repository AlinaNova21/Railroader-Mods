using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Serilog;

namespace AlinasRailTools.Scripting;
class Runner
{
  private static readonly ILogger Logger = Log.ForContext<Runner>();
  readonly Dictionary<string, Script> scripts = [];
  FileSystemWatcher watcher;
  readonly string ModsDir = Path.GetFullPath("Mods/AlinasRailTools/Scripts");
  public Runner()
  {
    watcher = new(ModsDir, "*.csx")
    {
      IncludeSubdirectories = true,
      NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
      EnableRaisingEvents = true,
    };
    watcher.Changed += (s, e) => {
      var relPath = e.FullPath.Remove(0, ModsDir.Length).TrimStart(Path.DirectorySeparatorChar);
      Logger.Information("Script file changed: {FullPath}", e.FullPath);
      try {
        var id = Path.GetFileNameWithoutExtension(e.FullPath);
        var code = File.ReadAllText(e.FullPath);
        LoadScript(id, code);
        RunScript(id);
      } catch (Exception ex) {
        Logger.Error(ex, "Error running script from file change {FullPath}", e.FullPath);
      }
    };
  }
  public void FindAndRunScripts()
  {
    var scriptFiles = Directory.GetFiles("Mods/AlinasRailTools/Scripts", "*.csx", SearchOption.AllDirectories);
    foreach (var scriptFile in scriptFiles) {
      var id = Path.GetFileNameWithoutExtension(scriptFile);
      try {
        var code = File.ReadAllText(scriptFile);
        LoadScript(id, code);
        RunScript(id);
      } catch (Exception e) {
        Logger.Error(e, "Error running script {ScriptId}", id);
      }
    }
  }
  public Script LoadScript(string id, string code)
  {
    var logger = Log.ForContext<Runner>().ForContext("ScriptId", id);
    var scriptOptions = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default
      .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location)))
      .WithImports([
        "System",
        "System.Linq",
        "System.Collections.Generic",
        "UnityEngine",
        "Model",
        "Track",
        "Track.Signals",
        "Network",
        "AlinasRailTools.Scripting"
      ]);
    var script = CSharpScript.Create(code, scriptOptions);
    var compilation = script.GetCompilation();
    var diagnostics = compilation.GetDiagnostics();
    foreach (var diag in diagnostics) {
      logger.Information("{Diagnostic}", diag.ToString());
    }
    scripts[id] = script;
    return script;
  }
  public void RunScript(string id)
  {
    var logger = Log.ForContext<Runner>().ForContext("ScriptId", id);
    var script = scripts[id];
    var result = script.RunAsync().Result;
    if (result.Exception != null) {
      logger.Error(result.Exception, "Script execution failed");
    }
  }
}
