using System;
using System.IO;
using AlinasRailTools.Shared.Definitions;
using Newtonsoft.Json;
using Serilog;

namespace AlinasRailTools.Shared.Serialization;

/// <summary>
/// Handles serialization and deserialization of game graphs
/// </summary>
public class GameGraphSerializer
{
  private static readonly ILogger Logger = Log.ForContext<GameGraphSerializer>();
  private readonly JsonSerializerSettings settings;

  public GameGraphSerializer()
  {
    settings = new JsonSerializerSettings
    {
      Formatting = Formatting.Indented,
      NullValueHandling = NullValueHandling.Ignore,
      DefaultValueHandling = DefaultValueHandling.Ignore
    };
  }

  /// <summary>
  /// Saves a game graph to a JSON file
  /// </summary>
  public void SaveGameGraph(SerializedGameGraph gameGraph, string filePath)
  {
    try
    {
      var json = JsonConvert.SerializeObject(gameGraph, settings);
      File.WriteAllText(filePath, json);
      Logger.Information("Saved game graph to {FilePath}", filePath);
    }
    catch (Exception ex)
    {
      Logger.Error(ex, "Failed to save game graph to {FilePath}", filePath);
      throw;
    }
  }

  /// <summary>
  /// Loads a game graph from a JSON file
  /// </summary>
  public SerializedGameGraph LoadGameGraph(string filePath)
  {
    try
    {
      var json = File.ReadAllText(filePath);
      var gameGraph = JsonConvert.DeserializeObject<SerializedGameGraph>(json, settings);
      Logger.Information("Loaded game graph from {FilePath}", filePath);
      return gameGraph;
    }
    catch (Exception ex)
    {
      Logger.Error(ex, "Failed to load game graph from {FilePath}", filePath);
      throw;
    }
  }

  /// <summary>
  /// Saves a game graph to a JSON string
  /// </summary>
  public string SerializeGameGraph(SerializedGameGraph gameGraph)
  {
    try
    {
      return JsonConvert.SerializeObject(gameGraph, settings);
    }
    catch (Exception ex)
    {
      Logger.Error(ex, "Failed to serialize game graph");
      throw;
    }
  }

  /// <summary>
  /// Loads a game graph from a JSON string
  /// </summary>
  public SerializedGameGraph DeserializeGameGraph(string json)
  {
    try
    {
      return JsonConvert.DeserializeObject<SerializedGameGraph>(json, settings);
    }
    catch (Exception ex)
    {
      Logger.Error(ex, "Failed to deserialize game graph");
      throw;
    }
  }
}
