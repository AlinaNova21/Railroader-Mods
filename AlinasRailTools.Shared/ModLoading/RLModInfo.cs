using System.IO;
using AlinasRailTools.Shared.Definitions;

namespace AlinasRailTools.Shared.ModLoading;

/// <summary>
/// Contains information about a loaded Railloader mod
/// </summary>
public class RLModInfo
{
  public string Directory { get; set; }
  public RLModDefinition Definition { get; set; }

  /// <summary>
  /// Gets the full path to a file within the mod directory
  /// </summary>
  public string GetFilePath(string relativePath)
  {
    return Path.Combine(Directory, relativePath);
  }

  /// <summary>
  /// Checks if a file exists in the mod directory
  /// </summary>
  public bool FileExists(string relativePath)
  {
    return File.Exists(GetFilePath(relativePath));
  }

  /// <summary>
  /// Reads a file from the mod directory
  /// </summary>
  public string ReadFile(string relativePath)
  {
    return File.ReadAllText(GetFilePath(relativePath));
  }

  /// <summary>
  /// Reads and deserializes a JSON file from the mod directory
  /// </summary>
  public T ReadJson<T>(string relativePath)
  {
    var json = ReadFile(relativePath);
    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
  }
}
