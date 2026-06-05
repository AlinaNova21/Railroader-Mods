using System.Numerics;
using AlinasRailTools.Shared.Data;
using AlinasRailTools.Shared.Definitions;
using AlinasRailTools.Shared.Resources;
using AlinasRailTools.Shared.Serialization;
using AlinasRailTools.Shared.ModLoading;

namespace AlinasRailTools.Editor.Data;

/// <summary>
/// Asynchronously loads track graph data from base game + mods into WorkingStateDB
/// Uses ModLoader to discover mods and their game-graph mixintos
/// </summary>
public class AsyncGraphLoader
{
    private readonly WorkingStateDB _db;
    private readonly string _gameDirectory;
    private Task? _loadingTask;
    private int _nodesLoaded;
    private int _segmentsLoaded;
    private int _modsLoaded;
    private bool _isLoading;
    private bool _isComplete;
    private ModLoadOrderResult? _loadOrderResult;

    public bool IsLoading => _isLoading;
    public bool IsComplete => _isComplete;
    public int NodesLoaded => _nodesLoaded;
    public int SegmentsLoaded => _segmentsLoaded;
    public int ModsLoaded => _modsLoaded;
    public ModLoadOrderResult? LoadOrderResult => _loadOrderResult;

    public AsyncGraphLoader(WorkingStateDB db, string gameDirectory)
    {
        _db = db;
        _gameDirectory = gameDirectory;
    }

    /// <summary>
    /// Start loading in the background
    /// </summary>
    public void StartLoading()
    {
        if (_isLoading || _isComplete)
            return;

        _isLoading = true;
        _loadingTask = Task.Run(LoadGraphAsync);
        Console.WriteLine($"[AsyncGraphLoader] Started loading base game + mods from {_gameDirectory}");
    }

    /// <summary>
    /// Load graph data asynchronously from base game + mods using ModLoader
    /// </summary>
    private async Task LoadGraphAsync()
    {
        try
        {
            var serializer = new GameGraphSerializer();
            var modLoader = new ModLoader(_gameDirectory);

            // Register virtual mods to satisfy dependencies
            modLoader.RegisterStandardVirtualMods();
            Console.WriteLine($"[AsyncGraphLoader] Registered virtual mods for railloader, StrangeCustoms, and AlinasMapMod");

            // 1. Load base game graph
            string baseGameGraphPath = Path.Combine(_gameDirectory, "game-graph-dump.json");
            if (File.Exists(baseGameGraphPath))
            {
                Console.WriteLine($"[AsyncGraphLoader] Loading base game graph: {baseGameGraphPath}");
                var baseGameGraph = await Task.Run(() => serializer.LoadGameGraph(baseGameGraphPath));
                await LoadGameGraphIntoDatabase(baseGameGraph, "base-game");
                _modsLoaded++;
            }

            // 2. Discover mods in load order (with topological sort)
            Console.WriteLine($"[AsyncGraphLoader] Discovering mods...");
            _loadOrderResult = await Task.Run(() => modLoader.DiscoverModsInLoadOrder());
            Console.WriteLine($"[AsyncGraphLoader] Mod discovery complete");

            if (_loadOrderResult.Errors.Count > 0)
            {
                Console.WriteLine($"[AsyncGraphLoader] Mod load order had {_loadOrderResult.Errors.Count} errors:");
                foreach (var error in _loadOrderResult.Errors.Take(5))
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            Console.WriteLine($"[AsyncGraphLoader] Discovered {_loadOrderResult.SortedMods.Count} mods in dependency order");

            // 3. Load each mod's game-graph mixintos in dependency order
            foreach (var modInfo in _loadOrderResult.SortedMods)
            {
                try
                {
                    var gameGraphMixintos = modInfo.Definition.Mixintos.GetGameGraphs().ToList();

                    if (gameGraphMixintos.Count == 0)
                        continue;

                    Console.WriteLine($"[AsyncGraphLoader] Loading mod '{modInfo.Definition.Id}' ({gameGraphMixintos.Count} game-graphs)");

                    foreach (var mixinto in gameGraphMixintos)
                    {
                        string graphPath = Path.Combine(modInfo.Directory, mixinto.File);

                        if (!File.Exists(graphPath))
                        {
                            Console.WriteLine($"[AsyncGraphLoader] WARNING: Mixinto file not found: {graphPath}");
                            continue;
                        }

                        var modGraph = await Task.Run(() => serializer.LoadGameGraph(graphPath));
                        await LoadGameGraphIntoDatabase(modGraph, modInfo.Definition.Id);
                    }

                    _modsLoaded++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AsyncGraphLoader] ERROR loading mod '{modInfo.Definition.Id}': {ex.Message}");
                    // Continue with next mod
                }
            }

            Console.WriteLine($"[AsyncGraphLoader] Completed loading: {_modsLoaded} mods, {_nodesLoaded} nodes, {_segmentsLoaded} segments");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AsyncGraphLoader] Error loading graphs: {ex.Message}");
            Console.WriteLine($"[AsyncGraphLoader] Stack trace: {ex.StackTrace}");
        }
        finally
        {
            _isLoading = false;
            _isComplete = true;
        }
    }

    /// <summary>
    /// Load a single game graph into the database using batch inserts
    /// </summary>
    private async Task LoadGameGraphIntoDatabase(SerializedGameGraph gameGraph, string source)
    {
        if (gameGraph?.Tracks == null)
        {
            Console.WriteLine($"[AsyncGraphLoader] No tracks data in graph from {source}");
            return;
        }

        // Load nodes into database in batches
        if (gameGraph.Tracks.Nodes != null)
        {
            const int batchSize = 200;
            var nodeBatch = new List<(ResId<TrackNodeData>, TrackNodeData)>(batchSize);

            foreach (var nodeKvp in gameGraph.Tracks.Nodes)
            {
                var node = nodeKvp.Value;
                if (node == null || node.Position == null || node.Rotation == null)
                    continue;

                var nodeData = new TrackNodeData
                {
                    Id = nodeKvp.Key,
                    Position = new Vector3(node.Position.X, node.Position.Y, node.Position.Z),
                    Rotation = new Vector3(node.Rotation.X, node.Rotation.Y, node.Rotation.Z)
                };

                ResId<TrackNodeData> resId = new ResId<TrackNodeData>(nodeKvp.Key);
                nodeBatch.Add((resId, nodeData));

                // Insert batch when full
                if (nodeBatch.Count >= batchSize)
                {
                    _db.ReplaceBatch(nodeBatch);
                    _nodesLoaded += nodeBatch.Count;
                    nodeBatch.Clear();
                    await Task.Yield(); // Yield after each batch
                }
            }

            // Insert remaining nodes
            if (nodeBatch.Count > 0)
            {
                _db.ReplaceBatch(nodeBatch);
                _nodesLoaded += nodeBatch.Count;
            }
        }

        // Load segments into database in batches
        if (gameGraph.Tracks.Segments != null)
        {
            const int batchSize = 200;
            var segmentBatch = new List<(ResId<TrackSegmentData>, TrackSegmentData)>(batchSize);

            foreach (var segmentKvp in gameGraph.Tracks.Segments)
            {
                var segment = segmentKvp.Value;
                if (segment == null)
                    continue;

                var segmentData = new TrackSegmentData
                {
                    Id = segmentKvp.Key,
                    StartNodeId = segment.StartId,
                    EndNodeId = segment.EndId
                };

                ResId<TrackSegmentData> resId = new ResId<TrackSegmentData>(segmentKvp.Key);
                segmentBatch.Add((resId, segmentData));

                // Insert batch when full
                if (segmentBatch.Count >= batchSize)
                {
                    _db.ReplaceBatch(segmentBatch);
                    _segmentsLoaded += segmentBatch.Count;
                    segmentBatch.Clear();
                    await Task.Yield(); // Yield after each batch
                }
            }

            // Insert remaining segments
            if (segmentBatch.Count > 0)
            {
                _db.ReplaceBatch(segmentBatch);
                _segmentsLoaded += segmentBatch.Count;
            }
        }
    }

    /// <summary>
    /// Wait for loading to complete
    /// </summary>
    public async Task WaitForCompletionAsync()
    {
        if (_loadingTask != null)
        {
            await _loadingTask;
        }
    }
}
