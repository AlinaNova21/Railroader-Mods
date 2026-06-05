using System;
using System.Collections.Generic;
using System.Linq;
using KeyValue.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AlinasRailTools.Runtime;

/// <summary>
/// Virtual filesystem that stores files in a KeyValueObject for network-synced storage
/// </summary>
public class VirtualFileSystem
{
    private readonly KeyValueObject _kvo;
    private const string VFS_PREFIX = "vfs/";

    /// <summary>
    /// Create a new VirtualFileSystem backed by a KeyValueObject
    /// </summary>
    public VirtualFileSystem(KeyValueObject keyValueObject)
    {
        _kvo = keyValueObject ?? throw new ArgumentNullException(nameof(keyValueObject));
    }

    public bool HasKey(string key) => _kvo.Dictionary.ContainsKey(key);

    /// <summary>
    /// Store a raw key-value pair
    /// </summary>
    public void Set(string key, string value) => _kvo.Set(key, Value.String(value));

  /// <summary>
  /// Retrieve a raw value by key
  /// </summary>
  public string Get(string key)
    {
        try
        {
            var value = _kvo[key];
            return value.StringValue;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get the full VFS key for a file path
    /// </summary>
    private string GetKey(string path)
    {
        // Normalize path separators, remove leading slash, and convert to lowercase for case-insensitive storage
        path = path.Replace('\\', '/').TrimStart('/').ToLowerInvariant();
        return VFS_PREFIX + path;
    }

  /// <summary>
  /// Write a file with string content
  /// </summary>
  public void WriteFile(string path, string content) => Set(GetKey(path), content);

  /// <summary>
  /// Read a file as string
  /// </summary>
  public string ReadFile(string path)
    {
        var content = Get(GetKey(path));
        if (content == null)
        {
            throw new FileNotFoundException($"File not found: {path}");
        }
        return content;
    }

    /// <summary>
    /// Check if a file exists
    /// </summary>
    public bool FileExists(string path)
    {
        return HasKey(GetKey(path));
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    public void DeleteFile(string path)
    {
        // Set to null to remove the file
        Set(GetKey(path), null);
    }

    /// <summary>
    /// List all files with optional prefix filter
    /// </summary>
    public IEnumerable<string> ListFiles(string prefix = "")
    {
        // Normalize prefix
        prefix = prefix.Replace('\\', '/').TrimStart('/');
        var fullPrefix = VFS_PREFIX + prefix;

        // Get all keys that start with the VFS prefix
        var allKeys = _kvo.Keys.Where(k => k.StartsWith(fullPrefix));

        // Strip the VFS_PREFIX from the keys to return clean paths
        return allKeys.Select(k => k.Substring(VFS_PREFIX.Length));
    }

    /// <summary>
    /// Read a file and parse as JObject
    /// </summary>
    public JObject ReadJSON(string path)
    {
        var content = ReadFile(path);
        return JObject.Parse(content);
    }

    /// <summary>
    /// Read a file and deserialize as type T
    /// </summary>
    public T ReadJSON<T>(string path)
    {
        var content = ReadFile(path);
        return JsonConvert.DeserializeObject<T>(content);
    }

    /// <summary>
    /// Serialize a JObject and write to file
    /// </summary>
    public void WriteJSON(string path, JObject obj)
    {
        var content = obj.ToString(Formatting.Indented);
        WriteFile(path, content);
    }

    /// <summary>
    /// Serialize an object and write to file
    /// </summary>
    public void WriteJSON<T>(string path, T obj)
    {
        var content = JsonConvert.SerializeObject(obj, Formatting.Indented);
        WriteFile(path, content);
    }
}

/// <summary>
/// Exception thrown when a file is not found in the VFS
/// </summary>
public class FileNotFoundException : Exception
{
    public FileNotFoundException(string message) : base(message) { }
}
