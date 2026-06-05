using System;

namespace AlinasRailTools.Shared.Heightmap;

/// <summary>
/// Geographic coordinates (latitude, longitude) in degrees.
/// </summary>
public readonly struct LatLng
{
    public double Latitude { get; }
    public double Longitude { get; }

    public LatLng(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";
}

/// <summary>
/// MapBox tile coordinates in Web Mercator projection.
/// Supports fractional coordinates for precise positioning.
/// </summary>
public readonly struct MapBoxTileCoord
{
    public double X { get; }
    public double Y { get; }
    public byte Zoom { get; }

    public MapBoxTileCoord(double x, double y, byte zoom)
    {
        X = x;
        Y = y;
        Zoom = zoom;
    }

    /// <summary>
    /// Get the integer tile coordinates (for tile fetching).
    /// </summary>
    public (uint x, uint y) GetTileIndices() => ((uint)Math.Floor(X), (uint)Math.Floor(Y));

    /// <summary>
    /// Get the fractional part within the tile (0.0 to 1.0).
    /// </summary>
    public (double x, double y) GetFractional() => (X - Math.Floor(X), Y - Math.Floor(Y));

    public override string ToString() => $"MapBox({X:F6}, {Y:F6}, z{Zoom})";
}

/// <summary>
/// Game tile coordinates (can be negative, centered on origin).
/// </summary>
public readonly struct GameTileCoord
{
    public int X { get; }
    public int Y { get; }

    public GameTileCoord(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"GameTile({X}, {Y})";
}

/// <summary>
/// Coordinate conversion utilities using Web Mercator projection.
/// Matches the game's Map.Runtime.GIS.Utilities implementation exactly.
/// </summary>
public static class CoordinateConverter
{
    /// <summary>
    /// Meters per degree of latitude (constant).
    /// Matches game's 111111f constant.
    /// </summary>
    public const double METERS_PER_DEGREE_LAT = 111_111.0;

    /// <summary>
    /// Degrees to radians constant from game (0.017453292519943295).
    /// </summary>
    private const double DEG2RAD = 0.017453292519943295;

    /// <summary>
    /// PI constant used by game (3.141592653589793).
    /// </summary>
    private const double PI = 3.141592653589793;

    /// <summary>
    /// Convert latitude/longitude to MapBox tile coordinates.
    /// Exactly matches the game's Map.Runtime.GIS.Utilities.LatLongToTile.
    /// </summary>
    public static MapBoxTileCoord LatLngToMapBoxTile(LatLng coord, byte zoom)
    {
        // Game's LatLongToMercat - modifies x and y in place
        double x = coord.Longitude;
        double y = coord.Latitude;

        double sinLat = Math.Sin(y * DEG2RAD);
        x = (x + 180.0) / 360.0;
        y = 0.5 - Math.Log((1.0 + sinLat) / (1.0 - sinLat)) / 12.566370614359172;

        // Game's LatLongToTile
        uint n = 256U << zoom;
        double tx = Clamp(x * n + 0.5, 0.0, n - 1U) / 256.0;
        double ty = Clamp(y * n + 0.5, 0.0, n - 1U) / 256.0;

        return new MapBoxTileCoord(tx, ty, zoom);
    }

    /// <summary>
    /// Convert MapBox tile coordinates to latitude/longitude.
    /// Exactly matches the game's Map.Runtime.GIS.Utilities.TileToLatLong.
    /// </summary>
    public static LatLng MapBoxTileToLatLng(MapBoxTileCoord coord)
    {
        double n = (double)(256 << coord.Zoom);
        double lng = 360.0 * (Repeat(coord.X * 256.0, 0.0, n - 1.0) / n - 0.5);
        double lat = 90.0 - 360.0 * Math.Atan(Math.Exp(-(0.5 - Clamp(coord.Y * 256.0, 0.0, n - 1.0) / n) * 2.0 * PI)) / PI;

        return new LatLng(lat, lng);
    }

    /// <summary>
    /// Clamp value between min and max.
    /// Matches game's Utilities.Clamp.
    /// </summary>
    private static double Clamp(double n, double minValue, double maxValue)
    {
        if (n < minValue) return minValue;
        if (n > maxValue) return maxValue;
        return n;
    }

    /// <summary>
    /// Wrap value to stay within range.
    /// Matches game's Utilities.Repeat.
    /// </summary>
    private static double Repeat(double n, double minValue, double maxValue)
    {
        if (double.IsInfinity(n) || double.IsInfinity(minValue) || double.IsInfinity(maxValue) ||
            double.IsNaN(n) || double.IsNaN(minValue) || double.IsNaN(maxValue))
        {
            return n;
        }

        double range = maxValue - minValue;
        while (n < minValue || n > maxValue)
        {
            if (n < minValue)
                n += range;
            else if (n > maxValue)
                n -= range;
        }
        return n;
    }

    /// <summary>
    /// Get meters per degree of longitude at a specific latitude.
    /// </summary>
    public static double MetersPerDegreeLon(double latitude)
    {
        double latRad = latitude * DEG2RAD;
        return METERS_PER_DEGREE_LAT * Math.Cos(latRad);
    }

    /// <summary>
    /// Convert meters to degrees at a specific latitude/longitude.
    /// </summary>
    public static (double deltaLat, double deltaLng) MetersToDegrees(double meters, double latitude)
    {
        double deltaLat = meters / METERS_PER_DEGREE_LAT;
        double deltaLng = meters / MetersPerDegreeLon(latitude);
        return (deltaLat, deltaLng);
    }

    /// <summary>
    /// Convert lat/lng to game tile coordinates.
    /// </summary>
    public static GameTileCoord LatLngToGameTile(double lat, double lng, double originLat, double originLng, double tileDimension)
    {
        // Calculate meters from origin
        double northMeters = (lat - originLat) * METERS_PER_DEGREE_LAT;

        // Use the target latitude for longitude calculation (approximate)
        double eastMeters = (lng - originLng) * MetersPerDegreeLon(lat);

        // Convert to tile indices
        int tileX = (int)Math.Floor(eastMeters / tileDimension);
        int tileY = (int)Math.Floor(northMeters / tileDimension);

        return new GameTileCoord(tileX, tileY);
    }

    /// <summary>
    /// Convert game tile coordinates to geographic bounds.
    /// </summary>
    public static (double minLat, double minLng, double maxLat, double maxLng) GameTileToBounds(
        int tileX, int tileY, double originLat, double originLng, double tileDimension)
    {
        // min = SW corner
        double minOffsetY = tileY * tileDimension;
        double minOffsetX = tileX * tileDimension;

        double minLat = originLat + (minOffsetY / METERS_PER_DEGREE_LAT);

        // max = NE corner
        double maxOffsetY = (tileY + 1) * tileDimension;
        double maxOffsetX = (tileX + 1) * tileDimension;

        double maxLat = originLat + (maxOffsetY / METERS_PER_DEGREE_LAT);

        // Use center latitude for consistent longitude calculation across all tiles
        // This ensures adjacent tiles align perfectly in the east-west direction
        double centerLat = (minLat + maxLat) / 2.0;
        double minLng = originLng + (minOffsetX / MetersPerDegreeLon(centerLat));
        double maxLng = originLng + (maxOffsetX / MetersPerDegreeLon(centerLat));

        return (minLat, minLng, maxLat, maxLng);
    }
}
