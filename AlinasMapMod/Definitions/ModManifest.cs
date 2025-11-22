using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AlinasMapMod.Definitions;

public sealed class ModManifest : IDisposable
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ModDirectory { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public List<string> Authors { get; set; } = [];
    public List<string> Patches { get; set; } = [];
    private FileSystemWatcher fileSystemWatcher { get; set; }
    public ModManifest() { }

    public static ModManifest Load(string path)
    {
        var doc = XDocument.Load(path);
        var root = doc.Element("ModManifest");
        if (root == null) {
            throw new InvalidDataException($"Invalid manifest file: {path}");
        }
        var manifest = new ModManifest();
        manifest.ModDirectory = Path.GetDirectoryName(path) ?? "";
        manifest.Name = root.Element("Name")?.Value ?? "Unknown";
        manifest.Version = root.Element("Version")?.Value ?? "0.0.0";
        manifest.Description = root.Element("Description")?.Value ?? "No description available.";
        manifest.Authors = root.Element("Authors")?.Elements("Author").Select(e => e.Value).ToList() ?? [];
        manifest.Patches = root.Element("Patches")?.Elements("GameData").Select(e => e.Value).ToList() ?? [];
        manifest.RegisterWatchers();
        return manifest;
    }

    public void Save(string path)
    {
        var doc = new XDocument(
            new XElement("ModManifest",
                new XElement("Name", Name),
                new XElement("Version", Version),
                new XElement("Description", Description),
                new XElement("Authors", Authors.Select(a => new XElement("Author", a))),
                new XElement("Patches", Patches.Select(f => new XElement("File", f)))
            )
        );
        doc.Save(path);
    }

    public void RegisterWatchers()
    {
        var watcher = new FileSystemWatcher
        {
            Path = ModDirectory,
            NotifyFilter = NotifyFilters.LastWrite,
            IncludeSubdirectories = true,
        };
        watcher.Changed += OnChanged;
        watcher.EnableRaisingEvents = true;
        fileSystemWatcher = watcher;
    }

    public event EventHandler PatchChanged;

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (Patches.Select(p => Path.GetFullPath(Path.Combine(ModDirectory, p))).Any(p => p == e.FullPath)) {
            PatchChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        fileSystemWatcher.EnableRaisingEvents = false;
        fileSystemWatcher.Dispose();
    }
}