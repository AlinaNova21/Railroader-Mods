using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AlinasMapMod.Mods;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using HarmonyLib;
using Map.Runtime;
using StrangeCustoms;
using UnityEngine;

namespace AlinasMapMod.Map;
internal class TileManager : SingletonModBase<TileManager>
{
  public static bool AllowDownloadingTiles { get; set; } = false;

  private Dictionary<Vector2Int, string> tilepaths = [];
  private HashSet<Vector2Int> requested = [];

  private Thread downloadThread;

  private ConcurrentQueue<Vector2Int> downloadQueue = new();
  private ConcurrentQueue<Vector2Int> resultQueue = new();

  private Dictionary<Vector2Int, string> index = [];
  private bool HasAttemptedToDownloadIndex = false;

  public TileManager()
  {
        Messenger.Default.Register<MapWillLoadEvent>(this, e => {
            HasAttemptedToDownloadIndex = false;
            tilepaths = [];
            requested.Clear();
            index.Clear();
        });
        Messenger.Default.Register<GraphWillChangeEvent>(this, e => {
            if (!e.State.Texts.TryGetValue("mapName", out var mapName)) {
                mapName = "BushnellWhittier";
            }
            //e.State.Areas.Clear();
            //e.MarkChanged("areas");
            //e.State.Splineys.Clear();
            //e.MarkChanged("splineys");
        });
  }

  public void Update()
  {
    if (MapManager.Instance == null) return;
    if (downloadThread == null || !downloadThread.IsAlive) {
      downloadThread = new Thread(DownloadProcessor);
      downloadThread.Start();
    }
    Vector2Int pos;
    if (resultQueue.TryDequeue(out pos)) {
      Logger.Debug("Tile {0:000}_{1:000} downloaded", pos.x, pos.y);
      var store = AccessTools.Field(typeof(MapManager), "_store").GetValue(MapManager.Instance) as MapStore;
      var desc = AccessTools.Field(typeof(MapStore), "_descriptors").GetValue(store) as Dictionary<Vector2Int, TileDescriptor>;
      desc[pos] = new TileDescriptor(pos, TileDescriptorStatus.Real);
      tilepaths[pos] = CachePathFor(pos);
    }
  }

  internal void RequestTileDownload(Vector2Int position)
  {
    if (!AllowDownloadingTiles || (index.Count == 0 && HasAttemptedToDownloadIndex)) return;
    if (requested.Contains(position)) return;
    requested.Add(position);
    Logger.Debug($"Requesting tile download for {position.x}_{position.y}");
    downloadQueue.Enqueue(position);
  }

  void DownloadProcessor()
  {
    var store = AccessTools.Field(typeof(MapManager), "_store").GetValue(MapManager.Instance) as MapStore;
    var desc = AccessTools.Field(typeof(MapStore), "_descriptors").GetValue(store) as Dictionary<Vector2Int, TileDescriptor>;
    Vector2Int pos;

    while (true) {
      if (!downloadQueue.TryDequeue(out pos)) {
        Thread.Sleep(1000);
        continue;
      }
      var t = DownloadTile(pos);
      t.Wait();
      if (t.Result) {
        resultQueue.Enqueue(pos);
      }
    }
  }

  string CachePathFor(Vector2Int tp)
  {
    return Path.Combine(AlinasMapMod.ModDirectory, "Maps", MapManager.Instance.directoryName, $"tile_{tp.x:000}_{tp.y:000}.data");
  }

  internal async Task<bool> DownloadIndexFile()
  {
    if (!AllowDownloadingTiles) {
      return false; // don't download if not enabled  
    }
    if (HasAttemptedToDownloadIndex) {
      return false; // don't download again
    }
    HasAttemptedToDownloadIndex = true;

    var url = $"https://whoverse.nyc3.cdn.digitaloceanspaces.com/railroader-tiles/{MapManager.Instance.directoryName}/index.txt";
    Logger.Debug("Downloading index file from {url}", url);

    using (var client = new HttpClient()) {
      var response = await client.GetAsync(url);
      Logger.Debug("Response: {status}", response.StatusCode);

      if (response.IsSuccessStatusCode) {
        var data = await response.Content.ReadAsStringAsync();
        var list = data
          .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
          .Select(l => l.Trim('\n', '\r', ' '))
          .ToList();
        foreach (var line in list) {
          var file = line.Split('/').Last();
          if (!file.EndsWith(".data")) {
            Logger.Debug("File '{file}' from '{line}' does not end with .data, skipping", file, line);
            continue;
          }
          file = file.Substring(0, file.Length - 5);
          var pos = file.Split('_');
          var x = int.Parse(pos[1]);
          var y = int.Parse(pos[2]);
          index.Add(new Vector2Int(x, y), line);
        }
        Logger.Debug($"Downloaded index with {list.Count} lines, {index.Count} entries added to index");
        return true;
      } else {
        Logger.Error("Failed to download index file: {status}", response.StatusCode);
        return false;
      }
    }
  }

  internal async Task<bool> DownloadTile(Vector2Int pos)
  {
    var baseUrl = $"https://whoverse.nyc3.cdn.digitaloceanspaces.com/";
    var path = $"railroader-tiles/{MapManager.Instance.directoryName}/tile_{pos.x:000}_{pos.y:000}.data";
    var url = baseUrl + path;

    if (!AllowDownloadingTiles) {
      return false; // don't download if not enabled
    }
    if (index.Count == 0) {
      if (HasAttemptedToDownloadIndex) return false; // don't download again
      try {
        await DownloadIndexFile();
      } catch (Exception e) {
        Logger.Error(e, "Failed to download index file");
        return false;
      }
    }
    if (!index.ContainsKey(pos)) {
      //logger.Debug("Tile {0:000}_{1:000} not in index ({2})", pos.x, pos.y, path);
      return false;
    }
    var tilepath = CachePathFor(pos);
    if (File.Exists(tilepath)) {
      Logger.Debug("Tile {0:000}_{1:000} already exists", pos.x, pos.y);
      return true;
    }
    Logger.Debug("Downloading tile {0:000}_{1:000} from {url}", pos.x, pos.y, url);
    using (var client = new HttpClient()) {
      var response = await client.GetAsync(url);
      Logger.Debug("Response: {status}", response.StatusCode);
      if (response.IsSuccessStatusCode) {
        var data = await response.Content.ReadAsByteArrayAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(tilepath));
        File.WriteAllBytes(tilepath, data);
        Logger.Debug("Downloaded tile {0:000}_{1:000}", pos.x, pos.y);
        return true;
      } else {
        Logger.Error("Failed to download tile {0:000}_{1:000}: {status}", pos.x, pos.y, response.StatusCode);
        return false;
      }
    }
  }

  internal void LoadMaps(MapStore store)
  {
    tilepaths.Clear();
    var mapName = MapManager.Instance.directoryName;
    Logger.Information($"Loading modded map tiles for {mapName}");
    var desc = AccessTools.Field(typeof(MapStore), "_descriptors").GetValue(store) as Dictionary<Vector2Int, TileDescriptor>;
    foreach (var dir in Directory.GetDirectories("Mods")) {
      var modName = Path.GetFileName(dir);
      var path = Path.Combine(dir, "Maps", mapName);
      if (Directory.Exists(path)) {
        var cnt = 0;
        foreach (var file in Directory.GetFiles(path, "*.data")) {
          var parts = Path.GetFileNameWithoutExtension(file).Split('_');
          if (parts.Length != 3) continue;
          var x = int.Parse(parts[1]);
          var y = int.Parse(parts[2]);
          var position = new Vector2Int(x, y);
          desc[position] = new TileDescriptor(new Vector2Int(x, y), TileDescriptorStatus.Real);
          tilepaths[position] = file;
          cnt++;
        }
        Logger.Information("Loaded {cnt} tiles from {mod}", cnt, modName);
      }
    }
  }

  internal string GetMapTile(Vector2Int position)
  {
    if (tilepaths.ContainsKey(position)) {
      return tilepaths[position];
    }

    return "";
  }
}
