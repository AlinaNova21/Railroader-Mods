using System;
using System.IO;
using Newtonsoft.Json;
using Serilog;

namespace AlinasRailTools.Shared.Serialization;

/// <summary>
/// Generic serializer for all data types
/// </summary>
public class DataSerializer
{
  private static readonly ILogger Logger = Log.ForContext<DataSerializer>();
  private readonly JsonSerializerSettings settings;

  public DataSerializer(JsonSerializerSettings customSettings = null)
  {
    settings = customSettings ?? new JsonSerializerSettings
    {
      Formatting = Formatting.Indented,
      NullValueHandling = NullValueHandling.Ignore,
      DefaultValueHandling = DefaultValueHandling.Ignore
    };
  }

  /// <summary>
  /// Saves any object to a JSON file
  /// </summary>
  public void Save<T>(T data, string filePath)
  {
    try
    {
      var directory = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
        Logger.Debug("Created directory {Directory}", directory);
      }

      var json = JsonConvert.SerializeObject(data, settings);
      File.WriteAllText(filePath, json);
      Logger.Information("Saved {Type} to {FilePath}", typeof(T).Name, filePath);
    }
    catch (Exception ex)
    {
      Logger.Error(ex, "Failed to save {Type} to {FilePath}", typeof(T).Name, filePath);
      throw;
    }
  }

  /// <summary>
  /// Loads an object from a JSON file
  /// </summary>
  public T Load<T>(string filePath)
  {
    try
    {
      if (!File.Exists(filePath))
      {
        Logger.Warning("File not found: {FilePath}", filePath);
        throw new FileNotFoundException($"File not found: {filePath}");
      }

      var json = File.ReadAllText(filePath);
      var data = JsonConvert.DeserializeObject<T>(json, settings);
      Logger.Information("Loaded {Type} from {FilePath}", typeof(T).Name, filePath);
      return data;
    }
    catch (Exception ex)
    {
      Logger.Error(ex, "Failed to load {Type} from {FilePath}", typeof(T).Name, filePath);
      throw;
    }
  }

  /// <summary>
  /// Serializes an object to a JSON string
  /// </summary>
  public string Serialize<T>(T data)
  {
    try
    {
      return JsonConvert.SerializeObject(data, settings);
    }
    catch (Exception ex)
    {
      Logger.Error(ex, "Failed to serialize {Type}", typeof(T).Name);
      throw;
    }
  }

  /// <summary>
  /// Deserializes an object from a JSON string
  /// </summary>
  public T Deserialize<T>(string json)
  {
    try
    {
      return JsonConvert.DeserializeObject<T>(json, settings);
    }
    catch (Exception ex)
    {
      Logger.Error(ex, "Failed to deserialize {Type}", typeof(T).Name);
      throw;
    }
  }

  /// <summary>
  /// Checks if a file exists
  /// </summary>
  public bool FileExists(string filePath)
  {
    return File.Exists(filePath);
  }

  /// <summary>
  /// Tries to load a file, returns default if file doesn't exist
  /// </summary>
  public T TryLoad<T>(string filePath, T defaultValue = default)
  {
    try
    {
      if (!File.Exists(filePath))
      {
        Logger.Debug("File not found, returning default: {FilePath}", filePath);
        return defaultValue;
      }

      return Load<T>(filePath);
    }
    catch (Exception ex)
    {
      Logger.Warning(ex, "Failed to load {Type} from {FilePath}, returning default", typeof(T).Name, filePath);
      return defaultValue;
    }
  }
}
