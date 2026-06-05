using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.StrangeCustomsCompat;

/// <summary>
/// Represents a StrangeCustoms mod definition from definition.json
/// </summary>
public class ModDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string DirectoryPath { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public Dictionary<string, object> Mixintos { get; set; } = new();

    /// <summary>
    /// Extract game-graph file paths from mixintos, stripping file() wrappers if present
    /// </summary>
    public List<string> GetGameGraphFiles()
    {
        var files = new List<string>();

        if (!Mixintos.TryGetValue("game-graph", out var gameGraphMixinto))
        {
            return files;
        }

        if (gameGraphMixinto is JArray array)
        {
            foreach (var item in array)
            {
                var fileRef = SCCompatHelpers.StripFileWrapper(item.ToString());
                files.Add(fileRef);
            }
        }
        else if (gameGraphMixinto is string singleFile)
        {
            files.Add(SCCompatHelpers.StripFileWrapper(singleFile));
        }
        else if (gameGraphMixinto is JValue jValue)
        {
            files.Add(SCCompatHelpers.StripFileWrapper(jValue.ToString()));
        }

        return files;
    }
}
