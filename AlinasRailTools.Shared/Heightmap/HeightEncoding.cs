using System;

namespace AlinasRailTools.Shared.Heightmap;

/// <summary>
/// Height encoding and decoding utilities using the per-tile blue channel format.
///
/// Format:
/// - Blue channel: Signed base offset in 100m increments (-128 to +127, stored as 0 to 255)
///   ALL pixels in a tile share the same blue channel value (determined from tile average)
/// - R+G channels: Height offset within the 1000m band (0 to 65535 encoding 0-1000m)
///   Each pixel's specific height relative to the tile's base offset
///
/// This allows representing heights from -12,300m to +14,200m with ~1.5cm precision.
/// Uses double precision throughout to avoid float precision loss.
/// </summary>
public static class HeightEncoding
{
    /// <summary>
    /// Base height offset in meters (game default).
    /// </summary>
    public const double BASE_HEIGHT = 500.0;

    /// <summary>
    /// Height range per band in meters.
    /// </summary>
    public const double BAND_HEIGHT = 1000.0;

    /// <summary>
    /// Base offset increment in meters (blue channel represents multiples of this).
    /// </summary>
    public const double BASE_OFFSET_INCREMENT = 100.0;

    /// <summary>
    /// Encoding multiplier for R+G channels (1000m / 65535 steps).
    /// </summary>
    public const double ENCODING_MULTIPLIER = 65.535;

    /// <summary>
    /// Minimum blue channel base offset (-128 * 100m = -12,800m from base).
    /// </summary>
    public const int MIN_BASE_OFFSET = -128;

    /// <summary>
    /// Maximum blue channel base offset (+127 * 100m = +12,700m from base).
    /// </summary>
    public const int MAX_BASE_OFFSET = 127;

    /// <summary>
    /// Calculate the tile's blue channel value from the minimum height of all pixels.
    /// Uses Floor to ensure ALL pixel heights fall within [base, base+1000] range.
    /// Blue channel is treated as a signed byte with special handling for 0.
    /// </summary>
    /// <param name="minHeight">Minimum height of all pixels in the tile</param>
    /// <returns>Blue channel value (0-255)</returns>
    public static byte CalculateTileBlueChannel(double minHeight)
    {
        // Calculate base offset from minimum height, rounded DOWN to nearest 100m
        // This ensures all pixel heights are >= base offset (no clamping)
        int signedOffset = (int)Math.Floor((minHeight - BASE_HEIGHT) / BASE_OFFSET_INCREMENT);
        signedOffset = MathHelpers.Clamp(signedOffset, MIN_BASE_OFFSET, MAX_BASE_OFFSET);

        // Convert signed offset to byte using signed byte semantics
        // Game treats blue as signed byte: blue==0 special case, else (blue-128) is offset
        // For offset==0, use blue=0 (special case) to match base game tiles
        if (signedOffset == 0)
            return 0;

        return (byte)(signedOffset + 128);
    }

    /// <summary>
    /// Encode a height value to R, G bytes relative to a tile's base offset (blue channel).
    /// </summary>
    /// <param name="heightMeters">Height in meters</param>
    /// <param name="tileBlueChannel">The blue channel value for this tile (same for all pixels)</param>
    /// <returns>Tuple of (R, G) bytes</returns>
    public static (byte r, byte g) EncodeHeightRelativeToTile(double heightMeters, byte tileBlueChannel)
    {
        // Get the tile's base height from its blue channel
        double tileBaseHeight = GetBaseHeight(tileBlueChannel);

        // Calculate height offset within the tile's 1000m band
        double offsetHeight = heightMeters - tileBaseHeight;

        // Encode offset in R+G channels (0 to 65535 representing 0-1000m)
        // Matching Patches.cs line 210: FloatToUshort using base_offset
        ushort encoded = (ushort)MathHelpers.Clamp(offsetHeight * ENCODING_MULTIPLIER, 0.0, 65535.0);

        byte r = (byte)(encoded >> 8);
        byte g = (byte)(encoded & 0xFF);

        return (r, g);
    }

    /// <summary>
    /// Decode height from R, G, Blue bytes.
    /// Uses double precision to minimize rounding errors.
    /// Matches Patches.cs line 131: 500f + (num2 * 100f) + ((float)num / 65.535f)
    /// </summary>
    /// <param name="r">Red channel</param>
    /// <param name="g">Green channel</param>
    /// <param name="blue">Blue channel (tile's base offset)</param>
    /// <returns>Height in meters</returns>
    public static double DecodeHeight(byte r, byte g, byte blue)
    {
        // Decode base offset from blue channel (Patches.cs line 128)
        int baseOffset = blue == 0 ? 0 : blue - 128;

        // Decode height offset from R+G channels (Patches.cs line 127)
        ushort encoded = (ushort)((r << 8) | g);
        double offsetHeight = encoded / ENCODING_MULTIPLIER;

        // Calculate absolute height (Patches.cs line 131)
        double baseHeight = BASE_HEIGHT + (baseOffset * BASE_OFFSET_INCREMENT);
        return baseHeight + offsetHeight;
    }

    /// <summary>
    /// Get the base height for a given blue channel value.
    /// Useful for terrain positioning.
    /// </summary>
    /// <param name="blue">Blue channel value</param>
    /// <returns>Base height in meters</returns>
    public static double GetBaseHeight(byte blue)
    {
        int baseOffset = blue == 0 ? 0 : blue - 128;
        return BASE_HEIGHT + (baseOffset * BASE_OFFSET_INCREMENT);
    }

    /// <summary>
    /// Calculate the minimum height that can be represented with a given blue channel value.
    /// </summary>
    public static double GetMinHeight(byte blue)
    {
        return GetBaseHeight(blue);
    }

    /// <summary>
    /// Calculate the maximum height that can be represented with a given blue channel value.
    /// </summary>
    public static double GetMaxHeight(byte blue)
    {
        return GetBaseHeight(blue) + BAND_HEIGHT;
    }

    /// <summary>
    /// Validate that a height round-trips correctly through encoding/decoding for a given tile blue channel.
    /// Returns the precision error in meters.
    /// </summary>
    public static double GetRoundTripError(double heightMeters, byte tileBlueChannel)
    {
        var (r, g) = EncodeHeightRelativeToTile(heightMeters, tileBlueChannel);
        double decoded = DecodeHeight(r, g, tileBlueChannel);
        return Math.Abs(decoded - heightMeters);
    }
}
