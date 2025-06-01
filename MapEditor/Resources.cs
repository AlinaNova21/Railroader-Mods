using System.Reflection;
using Serilog;
using UnityEngine;

namespace MapEditor
{
  public static class Resources
  {

    public static class Icons
    {

      public static readonly Texture2D ConstructionIcon = LoadTexture2D("construction-icon.png", 24, 24);
      public static readonly Texture2D RotateAxisX = LoadTexture2D("rotate-axis-x.png", 256, 256);
      public static readonly Texture2D RotateAxisY = LoadTexture2D("rotate-axis-y.png", 256, 256);
      public static readonly Texture2D RotateAxisZ = LoadTexture2D("rotate-axis-z.png", 256, 256);
      public static readonly Texture2D ArrowUp = LoadTexture2D("arrow-up.png", 256, 256);

    }

    private static byte[] GetBytes(string path)
    {
      var assembly = Assembly.GetExecutingAssembly();
      using var stream = assembly.GetManifestResourceStream(path)!;
      using var ms = new System.IO.MemoryStream();
      stream.CopyTo(ms);
      return ms.ToArray();
    }

    private static Texture2D LoadTexture2D(string path, int width, int height)
    {
      try {
        var bytes = GetBytes($"MapEditor.Resources.{path}");
        var texture = new Texture2D(width, height);
        texture.LoadImage(bytes);
        return texture;
      } catch (System.Exception e) {
        Log.Error(e, "Failed to load texture {0}", path);
      }

      return null!;
    }

  }
}
