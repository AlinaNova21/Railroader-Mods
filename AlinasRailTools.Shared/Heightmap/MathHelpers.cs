using System;

namespace AlinasRailTools.Shared.Heightmap;

/// <summary>
/// Math helpers for compatibility across .NET versions.
/// </summary>
internal static class MathHelpers
{
#if NET5_0_OR_GREATER
    // For .NET 5+ use the built-in Math.Clamp
    public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
    public static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);
    public static ushort Clamp(ushort value, ushort min, ushort max) => (ushort)Math.Clamp((int)value, (int)min, (int)max);
#else
    /// <summary>
    /// Clamp a value between min and max.
    /// Backport of Math.Clamp for older .NET versions.
    /// </summary>
    public static int Clamp(int value, int min, int max)
    {
        if (min > max)
            throw new ArgumentException("min must be less than or equal to max");

        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    /// <summary>
    /// Clamp a value between min and max.
    /// Backport of Math.Clamp for older .NET versions.
    /// </summary>
    public static double Clamp(double value, double min, double max)
    {
        if (min > max)
            throw new ArgumentException("min must be less than or equal to max");

        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    /// <summary>
    /// Clamp a value between min and max.
    /// Backport of Math.Clamp for older .NET versions.
    /// </summary>
    public static ushort Clamp(ushort value, ushort min, ushort max)
    {
        if (min > max)
            throw new ArgumentException("min must be less than or equal to max");

        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
#endif
}
